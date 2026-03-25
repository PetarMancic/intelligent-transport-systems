namespace CarPooling.Models;

public class Voznja
{
    public int Id { get; set; }
    public string PocetniGrad { get; set; } = string.Empty;
    public string KrajnjiGrad { get; set; } = string.Empty;
    public DateTime VremePolaska { get; set; }
    public DateTime VremeDolaska { get; set; }

    public ICollection<UsputnaStanica> UsputneStanice { get; set; } = new List<UsputnaStanica>();
}