namespace WebApplication1.DTOs
{
    public class NewDto
    {
        public class Dionica
        {
            public int VoznjaId { get; set; }
            public string Polaziste { get; set; } = "";
            public string Odrediste { get; set; } = "";
            public DateTime VremeOd { get; set; }
            public DateTime VremeDo { get; set; }
            public string? StaniceSve { get; set; }
            public List<string> UsputneStanice =>
                string.IsNullOrEmpty(StaniceSve)
                    ? new List<string>()
                    : StaniceSve.Split(',').Select(s => s.Trim()).ToList();
        }

        public class RutaSaPresedanjem
        {
            public List<Dionica> Segmenti { get; set; } = new();
            public int BrojPresedanja => Segmenti.Count - 1;
            public DateTime Polazak => Segmenti.First().VremeOd;
            public DateTime Dolazak => Segmenti.Last().VremeDo;
            public int UkupnoMinuta => (int)(Dolazak - Polazak).TotalMinutes;
            public int CekanjeMinuta =>
                Segmenti.Count < 2 ? 0 :
                Segmenti.Zip(Segmenti.Skip(1), (a, b) =>
                    (int)(b.VremeOd - a.VremeDo).TotalMinutes).Sum();

            public double Score { get; set; }
        }
    }
}
