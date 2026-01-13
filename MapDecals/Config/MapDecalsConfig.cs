using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace MapDecals.Config;

public class MapDecalsConfig : IBasePluginConfig
{
    [JsonPropertyName("DatabaseConnection")]
    public string DatabaseConnection { get; set; } = "Server=localhost;Database=cs2;User=root;Password=;";

    [JsonPropertyName("DatabaseType")]
    public string DatabaseType { get; set; } = "mysql";

    [JsonPropertyName("Props")]
    public List<DecalConfig> Props { get; set; } = new();

    [JsonPropertyName("PlaceDecalCommands")]
    public CommandConfig PlaceDecalCommands { get; set; } = new();

    [JsonPropertyName("AdToggleCommands")]
    public CommandConfig AdToggleCommands { get; set; } = new();

    public int Version { get; set; } = 1;
}

public class DecalConfig
{
    [JsonPropertyName("UniqId")]
    public string UniqId { get; set; } = string.Empty;

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Material")]
    public string Material { get; set; } = string.Empty;

    [JsonPropertyName("ShowPermission")]
    public string ShowPermission { get; set; } = string.Empty;
}

public class CommandConfig
{
    [JsonPropertyName("Command")]
    public string Command { get; set; } = string.Empty;

    [JsonPropertyName("Aliases")]
    public List<string> Aliases { get; set; } = new();

    [JsonPropertyName("Permission")]
    public string Permission { get; set; } = string.Empty;
}
