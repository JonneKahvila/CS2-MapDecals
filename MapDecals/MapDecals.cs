using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using MapDecals.Config;
using MapDecals.Database;
using MapDecals.Database.Models;
using MapDecals.Commands;
using MapDecals.Events;
using MapDecals.Functions;
using MapDecals.Menus;

namespace MapDecals;

public class MapDecals : BasePlugin, IPluginConfig<MapDecalsConfig>
{
    public override string ModuleName => "Map Decals";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "JonneKahvila";
    public override string ModuleDescription => "Allows server owners to place decals on maps at predefined locations";

    public MapDecalsConfig Config { get; set; } = new();
    public DatabaseService DatabaseService { get; private set; } = null!;
    public DecalFunctions DecalFunctions { get; private set; } = null!;
    public MenuManager? MenuManager { get; private set; }
    public new CommandHandlers CommandHandlers { get; private set; } = null!;
    public EventHandlers EventHandlers { get; private set; } = null!;

    public List<MapDecal> ActiveMapDecals { get; set; } = new();
    public Dictionary<string, bool> PlayerPreferences { get; set; } = new();
    public Dictionary<string, string> PlacementMode { get; set; } = new();
    public Dictionary<string, long> RepositionMode { get; set; } = new();

    public void OnConfigParsed(MapDecalsConfig config)
    {
        Config = config;
        
        // Validate configuration
        if (string.IsNullOrEmpty(config.DatabaseConnection))
        {
            Logger.LogError("Database connection string is not configured!");
            return;
        }

        if (config.Props.Count == 0)
        {
            Logger.LogWarning("No decals configured in the configuration file!");
        }
    }

    public override void Load(bool hotReload)
    {
        try
        {
            Logger.LogInformation("Loading Map Decals plugin...");

            // Initialize database service
            DatabaseService = new DatabaseService(Config.DatabaseConnection, Config.DatabaseType);

            // Initialize other services
            DecalFunctions = new DecalFunctions(this);
            MenuManager = new MenuManager(this);
            CommandHandlers = new CommandHandlers(this);
            EventHandlers = new EventHandlers(this);

            // Initialize database
            Task.Run(async () =>
            {
                try
                {
                    await DatabaseService.InitializeDatabaseAsync();
                    Logger.LogInformation("Database initialized successfully");

                    // Load decals for current map if not on map change
                    if (hotReload)
                    {
                        await LoadMapDecalsAsync();
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to initialize database: {ex.Message}");
                }
            });

            // Register commands
            CommandHandlers.RegisterCommands();

            // Register events
            EventHandlers.RegisterEvents();

            Logger.LogInformation("Map Decals plugin loaded successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error loading plugin: {ex.Message}");
            throw;
        }
    }

    public override void Unload(bool hotReload)
    {
        try
        {
            Logger.LogInformation("Unloading Map Decals plugin...");

            // Despawn all decals
            DecalFunctions.DespawnAllDecals();

            // Clear collections
            ActiveMapDecals.Clear();
            PlayerPreferences.Clear();
            PlacementMode.Clear();
            RepositionMode.Clear();

            Logger.LogInformation("Map Decals plugin unloaded successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error unloading plugin: {ex.Message}");
        }
    }

    [GameEventHandler]
    public HookResult OnMapStart(EventMapTransition @event, GameEventInfo info)
    {
        // Load decals for the new map
        Server.NextFrame(async () =>
        {
            await LoadMapDecalsAsync();
        });

        return HookResult.Continue;
    }

    private async Task LoadMapDecalsAsync()
    {
        try
        {
            var mapName = Server.MapName;
            Logger.LogInformation($"Loading decals for map: {mapName}");

            // Clear existing decals
            DecalFunctions.DespawnAllDecals();
            ActiveMapDecals.Clear();

            // Load from database
            var decals = await DatabaseService.GetMapDecalsAsync(mapName);
            ActiveMapDecals.AddRange(decals);

            Logger.LogInformation($"Loaded {decals.Count} decals for map {mapName}");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error loading map decals: {ex.Message}");
        }
    }
}
