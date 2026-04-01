//using CarPooling.Data;
//using Microsoft.EntityFrameworkCore;
////using CarPooling.Data;
////using CarPooling.DTOs;
////using CarPooling.Models;
////using QuickGraph;
////using System;
////using Itinero;
//using Npgsql;
////using Dapper;
////using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

//namespace CarPooling.Services;

//public class VoznjaService
//{
//    private readonly AppDbContext _db;

//    // Parametri za presedanja
//    private const int MinBufferPresedanjeMin = 15;
//    private const int MaxCekanjePresedanjeMin = 180;
//    private const double PenalPoPresedanju = 35.0; // Povećan penal da bi direktne rute dominirale

//    private readonly string _connectionString;

//    public VoznjaService(AppDbContext db, IConfiguration configuration)
//    {
//        _db = db;
//        _connectionString = configuration.GetConnectionString("Default")
//                        ?? throw new ArgumentNullException("Konekcija pod imenom 'Default' nije nađena!");
//    }

//    //    // Pomoćna klasa za graf
//    //    private class VoznjaGrana : IEdge<string>
//    //    {
//    //        public string Source { get; set; } = "";
//    //        public string Target { get; set; } = "";
//    //        public RezultatPretrage Match { get; set; } = null!;
//    //        public DateTime VremePolaska { get; set; }
//    //        public DateTime VremeDolaska { get; set; }
//    //        public int VoznjaId { get; set; }
//    //    }

//    // --- VRACENA METODA ---
//    public async Task<List<string>> GetSviGradovi()
//    {
//        return await _db.Voznje
//            .Select(v => v.PocetniGrad)
//            .Union(_db.Voznje.Select(v => v.KrajnjiGrad))
//            .Union(_db.UsputneStanice.Select(s => s.Stanica))
//            .OrderBy(g => g)
//            .ToListAsync();
//    }
//}

////    public async Task<(List<UniverzalniRezultat> Items, int Total)> PronadjiSveVoznje(
////        string polazna, string odredisna, string datumIVreme, int page = 1, int pageSize = 20)
////    {
////        // 1. Pronađi direktne vožnje
////        var (direktne, _) = await PronadjiNajoptimalnijuVoznju(polazna, odredisna, datumIVreme, 1, 999);

////        // 2. Pronađi presedanja (QuickGraph Engine)
////        var (presedanja, _) = await PronadjiVoznjeGraf(polazna, odredisna, datumIVreme);

////        // 3. STROGO FILTRIRANJE
////        // Filtriramo presedanja: 
////        // - ID vožnji moraju biti unikatni (ne smeš presedati u ista kola)
////        // - Ako putanja već postoji kao direktna, ne nudi je kao presedanje
////        var filtriranaPresedanja = presedanja
////            .Where(p => p.Deonice.Select(d => d.VoznjaId).Distinct().Count() == p.Deonice.Count)
////            .Where(p => !direktne.Any(d => p.Deonice.Any(deonica => deonica.VoznjaId == d.VoznjaId)))
////            .ToList();

////        var sve = direktne.Select(d => new UniverzalniRezultat
////        {
////            JePresedanje = false,
////            Direktna = d,
////            Score = Math.Max(0, d.Score + 20)
////        })
////        .Concat(filtriranaPresedanja.Select(p => new UniverzalniRezultat
////        {
////            JePresedanje = true,
////            Presedanje = p,
////            Score = Math.Max(0, p.Score)
////        }))
////        .OrderByDescending(r => r.Score)
////        .ToList();

////        var total = sve.Count;
////        var items = sve.Skip((page - 1) * pageSize).Take(pageSize).ToList();
////        return (items, total);
////    }

////    public async Task<(List<RezultatPretrage>, int Total)> PronadjiNajoptimalnijuVoznju(
////        string polazna, string odredisna, string datumIVreme, int page = 1, int pageSize = 5)
////    {
////        var zeljeno = DateTime.Parse(datumIVreme);
////        var voznje = await _db.Voznje.Include(v => v.UsputneStanice)
////            .Where(v => v.VremePolaska.Date == zeljeno.Date).ToListAsync();

////        var kandidati = new List<RezultatPretrage>();
////        foreach (var v in voznje)
////        {
////            var m = PokusajPoklapanje(v, polazna, odredisna, zeljeno);
////            if (m != null) kandidati.Add(m);
////        }

////        if (kandidati.Any()) SkorujDirektne(kandidati);

////        return (kandidati, kandidati.Count);
////    }

////    private async Task<(List<RezultatPresedanja>, int Total)> PronadjiVoznjeGraf(string polazna, string odredisna, string datum)
////    {
////        var zeljeno = DateTime.Parse(datum);
////        var sveVoznje = await _db.Voznje.Include(v => v.UsputneStanice)
////            .Where(v => v.VremePolaska.Date == zeljeno.Date).ToListAsync();

////        var graf = new AdjacencyGraph<string, VoznjaGrana>(true);
////        foreach (var v in sveVoznje)
////        {
////            var tacke = IzgradiTackeRute(v);
////            for (int i = 0; i < tacke.Count - 1; i++)
////            {
////                for (int j = i + 1; j < tacke.Count; j++)
////                {
////                    var m = PokusajPoklapanje(v, tacke[i].Grad, tacke[j].Grad, tacke[i].Vreme);
////                    if (m != null) graf.AddVerticesAndEdge(new VoznjaGrana
////                    {
////                        Source = tacke[i].Grad,
////                        Target = tacke[j].Grad,
////                        VoznjaId = v.Id,
////                        VremePolaska = tacke[i].Vreme,
////                        VremeDolaska = tacke[j].Vreme,
////                        Match = m
////                    });
////                }
////            }
////        }

////        var rezultati = new List<RezultatPresedanja>();
////        if (graf.ContainsVertex(polazna) && graf.ContainsVertex(odredisna))
////            NadjiSvePutanje(graf, polazna, odredisna, zeljeno, new List<VoznjaGrana>(), new HashSet<string> { polazna }, rezultati, 3);

////        SkorujPresedanja(rezultati);
////        return (rezultati, rezultati.Count);
////    }

////    private void NadjiSvePutanje(AdjacencyGraph<string, VoznjaGrana> graf, string trenutni, string cilj,
////        DateTime vremeStizanja, List<VoznjaGrana> putanja, HashSet<string> poseceni, List<RezultatPresedanja> rezultati, int maxD)
////    {
////        if (putanja.Count >= maxD || !graf.TryGetOutEdges(trenutni, out var grane)) return;

////        foreach (var g in grane)
////        {
////            // ZABRANA ISTE VOZNJE I CIKLUSA
////            if (putanja.Any(p => p.VoznjaId == g.VoznjaId) || poseceni.Contains(g.Target)) continue;

////            // PROVERA VREMENA + BUFFER
////            var minP = putanja.Count == 0 ? vremeStizanja : vremeStizanja.AddMinutes(MinBufferPresedanjeMin);
////            if (g.VremePolaska < minP || (g.VremePolaska - vremeStizanja).TotalMinutes > MaxCekanjePresedanjeMin) continue;

////            var novaP = new List<VoznjaGrana>(putanja) { g };
////            if (g.Target == cilj)
////            {
////                rezultati.Add(new RezultatPresedanja
////                {
////                    Deonice = novaP.Select(x => x.Match).ToList(),
////                    UkupnoTrajanjeSaWait = (int)(g.VremeDolaska - novaP[0].VremePolaska).TotalMinutes
////                });
////            }
////            else
////            {
////                var noviPos = new HashSet<string>(poseceni) { g.Target };
////                NadjiSvePutanje(graf, g.Target, cilj, g.VremeDolaska, novaP, noviPos, rezultati, maxD);
////            }
////        }
////    }

////    private RezultatPretrage? PokusajPoklapanje(Voznja v, string odG, string doG, DateTime zeljeno)
////    {
////        var t = IzgradiTackeRute(v);
////        int i1 = t.FindIndex(x => x.Grad == odG), i2 = t.FindIndex(x => x.Grad == doG);
////        if (i1 == -1 || i2 == -1 || i1 >= i2) return null;

////        return new RezultatPretrage
////        {
////            VoznjaId = v.Id,
////            PocetniGrad = v.PocetniGrad,
////            KrajnjiGrad = v.KrajnjiGrad,
////            VremePolaska = v.VremePolaska.ToString("HH:mm"),
////            VremeDolaska = v.VremeDolaska.ToString("HH:mm"),
////            VremeUlaska = t[i1].Vreme.ToString("HH:mm"),
////            VremeIzlaska = t[i2].Vreme.ToString("HH:mm"),
////            TrajanjePutnikoveDeoniceMin = (int)(t[i2].Vreme - t[i1].Vreme).TotalMinutes,
////            RazlikaVremeMin = (int)Math.Abs((t[i1].Vreme - zeljeno).TotalMinutes),
////            StaniceIzmedju = i2 - i1 - 1,
////            UsputneStanice = v.UsputneStanice.OrderBy(s => s.VremeDolaska).Select(s => $"{s.Stanica} ({s.VremeDolaska:HH:mm})").ToList()
////        };
////    }

////    private List<(string Grad, DateTime Vreme)> IzgradiTackeRute(Voznja v)
////    {
////        var l = new List<(string, DateTime)> { (v.PocetniGrad, v.VremePolaska) };
////        l.AddRange(v.UsputneStanice.OrderBy(s => s.VremeDolaska).Select(s => (s.Stanica, s.VremeDolaska)));
////        l.Add((v.KrajnjiGrad, v.VremeDolaska));
////        return l;
////    }

////    private void SkorujDirektne(List<RezultatPretrage> k)
////    {
////        if (!k.Any()) return;
////        var minT = k.Min(x => x.TrajanjePutnikoveDeoniceMin);
////        var maxT = k.Max(x => x.TrajanjePutnikoveDeoniceMin);
////        var minK = k.Min(x => x.RazlikaVremeMin);
////        var maxK = k.Max(x => x.RazlikaVremeMin);

////        foreach (var r in k)
////        {
////            double sT = maxT == minT ? 100 : 100 - ((r.TrajanjePutnikoveDeoniceMin - minT) / (double)(maxT - minT) * 100);
////            double sK = maxK == minK ? 100 : 100 - ((r.RazlikaVremeMin - minK) / (double)(maxK - minK) * 100);
////            r.Score = Math.Round(sT * 0.5 + sK * 0.5, 1);
////        }
////    }

////    private void SkorujPresedanja(List<RezultatPresedanja> k)
////    {
////        if (!k.Any()) return;
////        var minT = k.Min(x => x.UkupnoTrajanjeSaWait);
////        var maxT = k.Max(x => x.UkupnoTrajanjeSaWait);
////        foreach (var r in k)
////        {
////            double sT = maxT == minT ? 100 : 100 - ((r.UkupnoTrajanjeSaWait - minT) / (double)(maxT - minT) * 100);
////            r.Score = Math.Round(sT - (r.Deonice.Count - 1) * PenalPoPresedanju, 1);
////        }
////    }
////    public async Task<List<VoznjaSaPresedanjemDto>> PronadjiSveMoguceOpcije(string odGrada, string doGrada, string vreme)
////    {
////        DateTime datumVreme = DateTime.Parse(vreme);

////        // Koristimo @odrediste umesto @do da izbegnemo Postgres error
////        string sql = @"
////WITH SveDionice AS (
////    -- 1. DIREKTNE LINIJE (A-G) + SVE STANICE
////    SELECT 
////        v.""Id"" AS ""VoznjaId"", 
////        v.""PocetniGrad"" AS ""Polaziste"", 
////        v.""KrajnjiGrad"" AS ""Odrediste"", 
////        v.""VremePolaska"" AS ""VremeOd"", 
////        v.""VremeDolaska"" AS ""VremeDo"",
////        (SELECT STRING_AGG(us.""Stanica"", ',' ORDER BY us.""VremeDolaska"") 
////         FROM usputne_stanice us WHERE us.""VoznjaId"" = v.""Id"") AS ""StaniceSve""
////    FROM voznje v

////    UNION ALL

////    -- 2. OD POČETKA DO USPUTNE (A-E) + STANICE IZMEĐU
////    SELECT 
////        v.""Id"", v.""PocetniGrad"", us.""Stanica"", v.""VremePolaska"", us.""VremeDolaska"",
////        (SELECT STRING_AGG(us2.""Stanica"", ',' ORDER BY us2.""VremeDolaska"") 
////         FROM usputne_stanice us2 
////         WHERE us2.""VoznjaId"" = v.""Id"" AND us2.""VremeDolaska"" < us.""VremeDolaska"")
////    FROM voznje v
////    JOIN usputne_stanice us ON v.""Id"" = us.""VoznjaId""

////    UNION ALL

////    -- 3. OD USPUTNE DO KRAJA (E-G) + STANICE IZMEĐU
////    SELECT 
////        v.""Id"", us.""Stanica"", v.""KrajnjiGrad"", us.""VremeDolaska"", v.""VremeDolaska"",
////        (SELECT STRING_AGG(us2.""Stanica"", ',' ORDER BY us2.""VremeDolaska"") 
////         FROM usputne_stanice us2 
////         WHERE us2.""VoznjaId"" = v.""Id"" AND us2.""VremeDolaska"" > us.""VremeDolaska"")
////    FROM voznje v
////    JOIN usputne_stanice us ON v.""Id"" = us.""VoznjaId""
////),
////SveOpcije AS (
////    -- SCENARIO: DIREKTNA VOZNJA (npr. ID 3)
////    SELECT 
////        ""VoznjaId"" AS ""Bus1Id"", 
////        NULL AS ""Bus2Id"", 
////        ""Polaziste"" AS ""Start"", 
////        NULL AS ""PresedanjeU"", 
////        ""Odrediste"" AS ""Cilj"", 
////        ""VremeOd"" AS ""PolazakPirot"", 
////        ""VremeDo"" AS ""DolazakBeograd"",
////        ""VremeOd"" AS ""PolazakDalje"", -- Za direktnu je isto
////        ""VremeDo"" AS ""DolazakPresedanje"", -- Za direktnu je isto
////        ""StaniceSve"" AS ""StaniceSegment1"",
////        NULL AS ""StaniceSegment2""
////    FROM SveDionice 
////    WHERE ""Polaziste"" = @polaziste AND ""Odrediste"" = @odrediste AND ""VremeOd"" >= @vremePocetka

////    UNION ALL

////    -- SCENARIO: PRESEDANJA (1+2, 1+3)
////    SELECT 
////        v1.""VoznjaId"", 
////        v2.""VoznjaId"", 
////        v1.""Polaziste"", 
////        v1.""Odrediste"" AS ""PresedanjeU"", 
////        v2.""Odrediste"", 
////        v1.""VremeOd"", 
////        v2.""VremeDo"",
////        v2.""VremeOd"" AS ""PolazakDalje"",
////        v1.""VremeDo"" AS ""DolazakPresedanje"",
////        v1.""StaniceSve"" AS ""StaniceSegment1"",
////        v2.""StaniceSve"" AS ""StaniceSegment2""
////    FROM SveDionice v1
////    JOIN SveDionice v2 ON v1.""Odrediste"" = v2.""Polaziste""
////    WHERE v1.""Polaziste"" = @polaziste 
////      AND v2.""Odrediste"" = @odrediste 
////      AND v2.""VremeOd"" > v1.""VremeDo"" 
////      AND v1.""VoznjaId"" <> v2.""VoznjaId""
////)
////SELECT * FROM SveOpcije ORDER BY ""PolazakPirot"" ASC;";

////        using (var connection = new NpgsqlConnection(_connectionString))
////        {
////            var result = await connection.QueryAsync<VoznjaSaPresedanjemDto>(sql, new
////            {
////                polaziste = odGrada,
////                odrediste = doGrada,
////                vremePocetka = datumVreme
////            });

////            return result.ToList();
////        }
////    }
////}