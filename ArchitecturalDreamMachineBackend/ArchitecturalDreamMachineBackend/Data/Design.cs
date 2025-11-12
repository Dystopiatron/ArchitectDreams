namespace ArchitecturalDreamMachineBackend.Data;

public class Design
{
    public int Id { get; set; }
    public double LotSize { get; set; }
    public string StyleKeywords { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
