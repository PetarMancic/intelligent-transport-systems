namespace CarPooling.DTOs;

// Zahtev putnika 
public class PretragaRequest
{
    public string OdGrada { get; set; } = string.Empty;
    public string DoGrada { get; set; } = string.Empty;
    public string Vreme { get; set; } = string.Empty;   // format: "HH:mm"
}

// Jedan rezultat pretrage 
public class RezultatPretrage
{
    public int VoznjaId { get; set; }
    public string PocetniGrad { get; set; } = string.Empty;
    public string KrajnjiGrad { get; set; } = string.Empty;
    public string VremePolaska { get; set; }  // vreme kad vozač kreće, vezano za svoju voznju 
    public string VremeDolaska { get; set; }  // vreme kad vozač stiže, vezano za svoju voznju 
    public string VremeUlaska { get; set; } = string.Empty;   // "HH:mm"
    public string VremeIzlaska { get; set; } = string.Empty;   // "HH:mm"
    public int StaniceIzmedju { get; set; }
    public int TrajanjePutnikoveDeoniceMin { get; set; }
    public int RazlikaVremeMin { get; set; }
    public double ScoreVremeKasnjenja { get; set; }
    public double ScoreStanice { get; set; }
    public double Score { get; set; }
    public List<string> UsputneStanice { get; set; } = new();
}