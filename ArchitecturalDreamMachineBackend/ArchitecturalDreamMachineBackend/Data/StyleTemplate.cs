namespace ArchitecturalDreamMachineBackend.Data;

public class StyleTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RoofType { get; set; } = string.Empty;
    public string WindowStyle { get; set; } = string.Empty;
    public int RoomCount { get; set; }
    public string Color { get; set; } = string.Empty;
    public string Texture { get; set; } = string.Empty;
}
