using CarPooling.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WebApplication1.Constants;
using static WebApplication1.DTOs.NewDto;

namespace CarPooling.Services;

public class PresedanjeService1
{
    private readonly string _connectionString;
    private const int MinBufferMin = 10; // Minimum za presedanje
    private const int MaxCekanjeMin = 240; // Maksimum čekanja (4h)
    private readonly AppDbContext contextDb;

    public PresedanjeService1(IConfiguration configuration, AppDbContext db)
    {
        contextDb = db;
        _connectionString = configuration.GetConnectionString("Default")
            ?? throw new ArgumentNullException("Konekcija 'Default' nije nađena!");


        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }

    public async Task<List<string>> GetAllCities()
    {
        return await contextDb.Voznje
            .Select(v => v.PocetniGrad)
            .Union(contextDb.Voznje.Select(v => v.KrajnjiGrad))
            .Union(contextDb.UsputneStanice.Select(s => s.Stanica))
            .OrderBy(g => g)
            .ToListAsync();
    }

    public async Task<List<RutaSaPresedanjem>> FindRoutes(
        string odGrada,
        string doGrada,
        string vreme,
        int maxPresedanja = 1)
    {
        // Koristimo Parse bez ToUniversalTime() da zadržimo lokalno vreme (09:00 ostaje 09:00)
        var datumVreme = DateTime.Parse(vreme);

        // Učitavamo dionice koje kreću od unetog vremena pa nadalje (za taj dan)
        var sveDionice = await UcitajSveDionice(datumVreme);

        var poPolazistuDictionary = sveDionice
         //   .Where(d=> d.VremeOd >= datumVreme)
            .GroupBy(d => d.Polaziste)
            .ToDictionary(g => g.Key, g => g.ToList());

        var rezultati = new List<RutaSaPresedanjem>();

        Pretrazi(
            trenutniGrad: odGrada,
            cilj: doGrada,
            najranijeMoguceVreme: datumVreme,
            preostaloSkokova: maxPresedanja + 1,
            poPolazistu: poPolazistuDictionary,
            tekuciLanac: new List<Dionica>(),
            korisceniIds: new HashSet<int>(),
            rezultati: rezultati
        );

        return rezultati
            .OrderBy(r => r.Polazak)
            .ThenBy(r => r.UkupnoMinuta)
            .ToList();
    }

    private void Pretrazi(
    string trenutniGrad,
    string cilj,
    DateTime najranijeMoguceVreme, // Ovo je vreme od kog putnik MOŽE da krene
    int preostaloSkokova,
    Dictionary<string, List<Dionica>> poPolazistu,
    List<Dionica> tekuciLanac,
    HashSet<int> korisceniIds,
    List<RutaSaPresedanjem> rezultati)
    {
        // 1. Bazni uslovi za prekid
        if (preostaloSkokova <= 0) return;
        if (!poPolazistu.TryGetValue(trenutniGrad, out var kandidati)) return;

        foreach (var dionica in kandidati)
        {
            // --- FILTER 1: VREME POLASKA 
            // Ako bus kreće u 08:00, a putnik je rekao da može tek od 09:00 -> PRESKOČI
            if (dionica.VremeOd < najranijeMoguceVreme)
            {
                continue;
            }

            // --- FILTER 2: ISTI BUS ---
            // Ne možeš presedati iz BusA u BusA
            if (korisceniIds.Contains(dionica.VoznjaId))
            {
                continue;
            } 

            // --- FILTER 3: MAKSIMALNO ČEKANJE ---
            // Ako je ovo presedanje (tekuciLanac nije prazan), proveri da se ne čeka predugo
            if (tekuciLanac.Count > 0)
            {
                var cekanje = (dionica.VremeOd - tekuciLanac.Last().VremeDo).TotalMinutes;
                if (cekanje > MaxCekanjeMin) continue;
                // Opciono: if (cekanje < MinBufferMin) continue; // Ako želiš i minimalno vreme za transfer
            }

            // --- AKCIJA: DODAVANJE U LANAC ---
            var noviKorisceni = new HashSet<int>(korisceniIds) { dionica.VoznjaId };
            tekuciLanac.Add(dionica);

            if (dionica.Odrediste == cilj)
            {
                // Našli smo validnu rutu!
                rezultati.Add(new RutaSaPresedanjem
                {
                    Segmenti = new List<Dionica>(tekuciLanac)
                });
            }
            else
            {
                // Nastavljamo pretragu iz grada gde je ovaj segment stao
                Pretrazi(
                    trenutniGrad: dionica.Odrediste,
                    cilj: cilj,
                    // Sledeći bus mora kretati nakon što ovaj stigne (plus buffer za presedanje)
                    najranijeMoguceVreme: dionica.VremeDo.AddMinutes(MinBufferMin),
                    preostaloSkokova: preostaloSkokova - 1,
                    poPolazistu: poPolazistu,
                    tekuciLanac: tekuciLanac,
                    korisceniIds: noviKorisceni,
                    rezultati: rezultati
                );
            }

            // --- BACKTRACK ---
            tekuciLanac.RemoveAt(tekuciLanac.Count - 1);
        }
    }

    private async Task<List<Dionica>> UcitajSveDionice(DateTime odVremena)
    {
        const string sql = SqlQueries.sql;

        using var connection = new NpgsqlConnection(_connectionString);
        var result = await connection.QueryAsync<Dionica>(sql, new { odVremena });
        return result.ToList();
    }
}