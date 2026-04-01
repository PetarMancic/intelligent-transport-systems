using static WebApplication1.DTOs.NewDto;

namespace WebApplication1.Services
{
    public class ScoredResult
    {
        public RutaSaPresedanjem? NajboljaDirektna { get; set; }
        public RutaSaPresedanjem? NajboljaSaPresedanjem { get; set; }
    }

    public class ScoringService
    {

        // Težinski faktori iz tvog flow chart-a
        private const double WeightKasnjenje = 0.40;
        private const double WeightTrajanje = 0.45;
        private const double WeightStanice = 0.15;

        public List<RutaSaPresedanjem> RankAllRoutes(List<RutaSaPresedanjem> sveRute, DateTime zeljenoVreme)
        {
            if (sveRute == null || !sveRute.Any()) return new List<RutaSaPresedanjem>();

            // 1. Priprema podataka za normalizaciju (identično kao pre)
            var metaPodaci = sveRute.Select(r => new
            {
                Ruta = r,
                SirovoKasnjenje = Math.Abs((r.Polazak - zeljenoVreme).TotalMinutes),
                SirovoTrajanje = r.UkupnoMinuta,
                // Broj stanica: usputne + broj presedanja (kao dodatno stajanje)
                SiroveStanice = r.Segmenti.Sum(s => s.UsputneStanice.Count) + r.BrojPresedanja
            }).ToList();

            // Ekstremi za normalizaciju
            double minK = metaPodaci.Min(x => x.SirovoKasnjenje);
            double maxK = metaPodaci.Max(x => x.SirovoKasnjenje);
            double minT = metaPodaci.Min(x => x.SirovoTrajanje);
            double maxT = metaPodaci.Max(x => x.SirovoTrajanje);
            double minS = metaPodaci.Min(x => x.SiroveStanice);
            double maxS = metaPodaci.Max(x => x.SiroveStanice);

            // 2. Izračunaj score za svaku rutu u listi
            foreach (var m in metaPodaci)
            {
                double scoreK = Normalizuj(m.SirovoKasnjenje, minK, maxK);
                double scoreT = Normalizuj(m.SirovoTrajanje, minT, maxT);
                double scoreS = Normalizuj(m.SiroveStanice, minS, maxS);

                // Tvoji težinski faktori: 40% Kašnjenje, 45% Trajanje, 15% Stanice
                m.Ruta.Score = (scoreK * 0.40) + (scoreT * 0.45) + (scoreS * 0.15);
            }

            // 3. VRATI CELU LISTU SORTIRANU PO SCORE-U (Od najvećeg ka najmanjem)
            return sveRute.OrderByDescending(r => r.Score).ToList();
        }

        private double Normalizuj(double val, double min, double max)
        {
            if (Math.Abs(max - min) < 0.001) return 100;
            return 100 - ((val - min) / (max - min) * 100);
        }
    }
}
