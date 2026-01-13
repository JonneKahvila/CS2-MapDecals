namespace MapDecals.Database.Models;

public class MapDecal
{
    public long Id { get; set; }
    public string Map { get; set; } = string.Empty;
    public string DecalId { get; set; } = string.Empty;
    public string DecalName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Angles { get; set; } = string.Empty;
    public int Depth { get; set; } = 12;
    public float Width { get; set; } = 128f;
    public float Height { get; set; } = 128f;
    public bool ForceOnVip { get; set; } = false;
    public bool IsActive { get; set; } = true;
}
