using CarPooling.Models;

public class UsputnaStanica
{
    public int Id { get; set; }
    public int VoznjaId { get; set; }
    public Voznja Voznja { get; set; } = null!;
    public string Stanica { get; set; } = string.Empty;
    public DateTime VremeDolaska { get; set; }
}