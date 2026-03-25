using Microsoft.EntityFrameworkCore;
using CarPooling.Data;
using CarPooling.DTOs;
using CarPooling.Models;
using System;
using System.Diagnostics;

namespace CarPooling.Services;

public class VoznjaService
{
    private readonly AppDbContext _db;

    private const double UticajVremenaPolaska = 0.40;
    private const double UticajBrojaStanica = 0.15;
    private const double UticajTrajanjaPutovanja = 0.45;
    private const int MaksimalnaDozvoljenRazlikaMinuta = 90;

    public VoznjaService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Vraca listu svih gradova koji se pojavljuju u bazi —
    /// kao pocetni grad, krajnji grad ili usputna stanica.
    /// Rezultat je sortiran azbucno i bez duplikata.
    /// Koristi se za popunjavanje dropdown-a na frontendu.
    /// </summary>
    public async Task<List<string>> GetSviGradovi()
    {
        var sw2 = Stopwatch.StartNew();
        var novoResenje = await _db.Voznje
            .Select(v => v.PocetniGrad)
            .Union(_db.Voznje.Select(v => v.KrajnjiGrad))
            .Union(_db.UsputneStanice.Select(s => s.Stanica))
            .OrderBy(g => g)
            .ToListAsync();

        return novoResenje;
    }

    /// <summary>
    /// Glavna metoda pretrage — pronalazi sve voznje koje odgovaraju
    /// putnikovim zahtevima i rangira ih po skoru podudarnosti.
    /// 
    /// Logika:
    /// 1. Iz baze izvlaci voznje za zadati datum gde vozac prolazi kroz
    ///    polazni grad putnika u zadato vreme ili kasnije.
    /// 2. Svaka voznja prolazi kroz PokusajPoklapanje koji proverava
    ///    redosled stanica i hard limit od 90 minuta razlike.
    /// 3. Preostale voznje se normalizuju — skor svakog parametra
    ///    (kasnjenje, broj stanica, trajanje) racuna se relativno
    ///    unutar skupa rezultata, a ne prema fiksnoj skali.
    /// 4. Rezultati se sortiraju po ukupnom skoru .
    /// </summary>
    /// <param name="polaznaDestinacija">Grad ukrcavanja putnika</param>
    /// <param name="odredisnaDestinacija">Grad iskrcavanja putnika</param>
    /// <param name="datumIVreme">Zeljeno vreme polaska u formatu ISO 8601 (npr. 2026-03-22T08:00:00)</param>
    /// <param name="page">Broj stranice (pocinje od 1)</param>
    /// <param name="pageSize">Broj rezultata po stranici</param>
    /// <returns>Lista rangiranih voznji za trenutnu stranicu i ukupan broj rezultata</returns>
    public async Task<(List<RezultatPretrage>, int Total)> PronadjiNajoptimalnijuVoznju(
    string polaznaDestinacija,
    string odredisnaDestinacija,
    string datumIVreme,
    int page = 1,
    int pageSize = 5)
    {
        var zeljenoVreme = DateTime.Parse(datumIVreme);

        var datumSamo = zeljenoVreme.Date; // 2026-03-22

        var voznje = await _db.Voznje
            .Include(v => v.UsputneStanice)
            .Where(v => (
                (v.PocetniGrad == polaznaDestinacija
                    && v.VremePolaska.Date == datumSamo
                    && v.VremePolaska >= zeljenoVreme)
                ||
                v.UsputneStanice.Any(s =>
                    s.Stanica == polaznaDestinacija
                    && s.VremeDolaska.Date == datumSamo
                    && s.VremeDolaska >= zeljenoVreme)
            ) && (
                v.KrajnjiGrad == odredisnaDestinacija ||
                v.UsputneStanice.Any(s => s.Stanica == odredisnaDestinacija)
            ))
            .ToListAsync();

        var kandidati = new List<RezultatPretrage>();
        foreach (var voznja in voznje)
        {
            var match = PokusajPoklapanje(voznja, polaznaDestinacija, odredisnaDestinacija, zeljenoVreme);
            if (match is not null)
                kandidati.Add(match);
        }

        // Normalizuj scoreTrajanjaVoznje relativno unutar skupa rezultata
        if (kandidati.Count > 0)
        {
            // Normalizacija kašnjenja
            double minKasnjenje = kandidati.Min(r => r.RazlikaVremeMin);
            double maxKasnjenje = kandidati.Max(r => r.RazlikaVremeMin);


            double minTrajanje = kandidati.Min(r => r.TrajanjePutnikoveDeoniceMin);
            double maxTrajanje = kandidati.Max(r => r.TrajanjePutnikoveDeoniceMin);


            // Normalizacija stanica
            double minBrojUsputnihStanica = kandidati.Min(r => r.StaniceIzmedju);
            double maxBrojUsputnihStanice = kandidati.Max(r => r.StaniceIzmedju);

            foreach (var k in kandidati)
            {
                double scoreTrajanje = (maxTrajanje == minTrajanje)
                    ? 100
                    : 100 - ((k.TrajanjePutnikoveDeoniceMin - minTrajanje) / (maxTrajanje - minTrajanje) * 100);

                double scoreKasnjenje = (maxKasnjenje == minKasnjenje)
                    ? 100
                    : 100 - ((k.RazlikaVremeMin - minKasnjenje) / (maxKasnjenje - minKasnjenje) * 100);

                double scoreStanice = (maxBrojUsputnihStanice == minBrojUsputnihStanica)
                    ? 100
                    : 100 - ((k.StaniceIzmedju - minBrojUsputnihStanica) / (maxBrojUsputnihStanice - minBrojUsputnihStanica) * 100);

                k.ScoreVremeKasnjenja = Math.Round(scoreKasnjenje, 2);
                k.ScoreStanice = Math.Round(scoreStanice, 2);

                k.Score = Math.Round(
                    (scoreKasnjenje * UticajVremenaPolaska) +
                    (scoreStanice * UticajBrojaStanica) +
                    (scoreTrajanje * UticajTrajanjaPutovanja), 1);
            }
        }

        var sortirani = kandidati.OrderByDescending(r => r.Score).ToList();
        var total = sortirani.Count;
        var items = sortirani
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (items, total);
    }

    /// <summary>
    /// Pokusava da upari vozacevu voznju sa putnikovim zahtevom.
    /// 
    /// Proverava:
    /// - Da li je grad ukrcavanja pre grada iskrcavanja na ruti
    /// - Da li razlika izmedju vozacevog prolaska kroz polazni grad
    ///   i putnikovog zeljenog vremena nije veca od MaksimalnaDozvoljenRazlikaMinuta
    /// 
    /// Ako provera prodje, vraca popunjen RezultatPretrage sa sirim vrednostima
    /// (Score = 0, bice izracunat naknadno u PronadjiNajoptimalnijuVoznju).
    /// Ako ne prodje, vraca null.
    /// </summary>
    /// <param name="vozacevaVoznja">Voznja iz baze sa usputnim stanicama</param>
    /// <param name="odGrada">Grad ukrcavanja putnika</param>
    /// <param name="doGrada">Grad iskrcavanja putnika</param>
    /// <param name="zeljenoVremePolaskaPutnika">Zeljeno vreme polaska putnika</param>
    /// <returns>RezultatPretrage ako voznja odgovara, null ako ne odgovara</returns>
    private RezultatPretrage? PokusajPoklapanje(
    Voznja vozacevaVoznja,
    string odGrada,
    string doGrada,
    DateTime zeljenoVremePolaskaPutnika)
    {
        var staniceVozaceveVoznje = vozacevaVoznja.UsputneStanice.OrderBy(s => s.VremeDolaska).ToList();

        var tackeRuteVozaceveVoznje = new List<(string Grad, DateTime Vreme)>
        {
            (vozacevaVoznja.PocetniGrad, vozacevaVoznja.VremePolaska)
        };

        foreach (var s in staniceVozaceveVoznje)
            tackeRuteVozaceveVoznje.Add((s.Stanica, s.VremeDolaska));

        tackeRuteVozaceveVoznje.Add((vozacevaVoznja.KrajnjiGrad, vozacevaVoznja.VremeDolaska));


        int indeksGradaUkrcavanjaPutnika = tackeRuteVozaceveVoznje.FindIndex(t => t.Grad == odGrada);
        int indeksGradaIskrcavanjaPutnika = tackeRuteVozaceveVoznje.FindIndex(t => t.Grad == doGrada);

        if (indeksGradaUkrcavanjaPutnika == -1 ||  indeksGradaIskrcavanjaPutnika == -1 ||  indeksGradaUkrcavanjaPutnika >= indeksGradaIskrcavanjaPutnika)
                 return null;

        var vremeDolaskaNaPickupGrad = tackeRuteVozaceveVoznje[indeksGradaUkrcavanjaPutnika].Vreme;
        var vremeDolaskaNaDropoffGrad = tackeRuteVozaceveVoznje[indeksGradaIskrcavanjaPutnika].Vreme;

        double kasnjenjePolaskaUMinutima = Math.Abs((vremeDolaskaNaPickupGrad - zeljenoVremePolaskaPutnika).TotalMinutes);

        if (kasnjenjePolaskaUMinutima > MaksimalnaDozvoljenRazlikaMinuta)
            return null;

        int brojStanicaIzmedjuPutnikovihGradova = indeksGradaIskrcavanjaPutnika - indeksGradaUkrcavanjaPutnika - 1;
        double trajanjePutnikovogPutovanjaMin = (vremeDolaskaNaDropoffGrad - vremeDolaskaNaPickupGrad).TotalMinutes;

        return new RezultatPretrage
        {
            VoznjaId = vozacevaVoznja.Id,
            PocetniGrad = vozacevaVoznja.PocetniGrad,
            KrajnjiGrad = vozacevaVoznja.KrajnjiGrad,
            VremePolaska = vozacevaVoznja.VremePolaska.ToString("HH:mm"),
            VremeDolaska = vozacevaVoznja.VremeDolaska.ToString("HH:mm"),
            VremeUlaska = vremeDolaskaNaPickupGrad.ToString("HH:mm"),
            VremeIzlaska = vremeDolaskaNaDropoffGrad.ToString("HH:mm"),
            StaniceIzmedju = brojStanicaIzmedjuPutnikovihGradova,
            TrajanjePutnikoveDeoniceMin = (int)trajanjePutnikovogPutovanjaMin,
            RazlikaVremeMin = (int)kasnjenjePolaskaUMinutima,
            ScoreVremeKasnjenja = 0,  // popuniće se u PronadjiNajoptimalnijuVoznju
            ScoreStanice = 0,  // popuniće se u PronadjiNajoptimalnijuVoznju
            Score = 0,  // popuniće se u PronadjiNajoptimalnijuVoznju
            UsputneStanice = staniceVozaceveVoznje
                .Select(s => $"{s.Stanica} ({s.VremeDolaska:HH:mm})")
                .ToList()
        };
    }
}