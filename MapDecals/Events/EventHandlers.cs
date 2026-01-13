using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace MapDecals.Events;

public class EventHandlers
{
    private readonly MapDecals _plugin;

    public EventHandlers(MapDecals plugin)
    {
        _plugin = plugin;
    }

    public void RegisterEvents()
    {
        _plugin.RegisterEventHandler<EventRoundStart>(OnRoundStart);
        _plugin.RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        _plugin.RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        _plugin.RegisterEventHandler<EventPlayerPing>(OnPlayerPing);
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        // Spawn all active decals
        Server.NextFrame(() =>
        {
            foreach (var decal in _plugin.ActiveMapDecals)
            {
                if (decal.IsActive)
                {
                    _plugin.DecalFunctions.SpawnDecal(decal);
                }
            }
        });

        return HookResult.Continue;
    }

    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || player.IsBot)
            return HookResult.Continue;

        var steamId = player.SteamID.ToString();

        // Load player preference
        Server.NextFrame(async () =>
        {
            try
            {
                var preference = await _plugin.DatabaseService.GetPlayerDecalPreferenceAsync(steamId);
                _plugin.PlayerPreferences[steamId] = preference;
            }
            catch (Exception ex)
            {
                _plugin.Logger.LogError($"Error loading player decal preference: {ex.Message}");
            }
        });

        return HookResult.Continue;
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || player.IsBot)
            return HookResult.Continue;

        var steamId = player.SteamID.ToString();

        // Save player preference (already saved on toggle, but this is a safety measure)
        if (_plugin.PlayerPreferences.ContainsKey(steamId))
        {
            _plugin.PlayerPreferences.Remove(steamId);
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerPing(EventPlayerPing @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        var steamId = player.SteamID.ToString();

        // Check if player is in placement or reposition mode
        if (_plugin.PlacementMode.TryGetValue(steamId, out var decalId))
        {
            HandleDecalPlacement(player, @event.X, @event.Y, @event.Z, decalId);
            _plugin.PlacementMode.Remove(steamId);
        }
        else if (_plugin.RepositionMode.TryGetValue(steamId, out var repositionDecalId))
        {
            HandleDecalReposition(player, @event.X, @event.Y, @event.Z, repositionDecalId);
            _plugin.RepositionMode.Remove(steamId);
        }

        return HookResult.Continue;
    }

    private void HandleDecalPlacement(CCSPlayerController player, float x, float y, float z, string decalId)
    {
        try
        {
            var decalConfig = _plugin.Config.Props.FirstOrDefault(p => p.UniqId == decalId);
            if (decalConfig == null)
            {
                player.PrintToChat(" [MapDecals] Invalid decal configuration.");
                return;
            }

            var pingPosition = new Vector(x, y, z);
            var eyeAngles = player.PlayerPawn?.Value?.EyeAngles ?? new QAngle(0, 0, 0);

            // Calculate decal placement
            var (position, angles) = _plugin.DecalFunctions.CalculateDecalPlacement(pingPosition, eyeAngles);

            // Create database entry
            var mapName = Server.MapName;
            var decal = new Database.Models.MapDecal
            {
                Map = mapName,
                DecalId = decalId,
                DecalName = decalConfig.Name,
                Position = $"{position.X} {position.Y} {position.Z}",
                Angles = $"{angles.X} {angles.Y} {angles.Z}",
                Depth = 12,
                Width = 128f,
                Height = 128f,
                ForceOnVip = false,
                IsActive = true
            };

            // Save to database
            Server.NextFrame(async () =>
            {
                try
                {
                    var newId = await _plugin.DatabaseService.InsertDecalAsync(decal);
                    decal.Id = newId;

                    // Add to active decals
                    _plugin.ActiveMapDecals.Add(decal);

                    // Spawn the entity
                    _plugin.DecalFunctions.SpawnDecal(decal);

                    player.PrintToChat(" [MapDecals] Decal placed successfully!");

                    // Open edit menu
                    _plugin.MenuManager?.OpenEditDecalMenu(player, decal);
                }
                catch (Exception ex)
                {
                    _plugin.Logger.LogError($"Error saving decal: {ex.Message}");
                    player.PrintToChat(" [MapDecals] Error placing decal. Please try again.");
                }
            });
        }
        catch (Exception ex)
        {
            _plugin.Logger.LogError($"Error handling decal placement: {ex.Message}");
            player.PrintToChat(" [MapDecals] Error placing decal. Please try again.");
        }
    }

    private void HandleDecalReposition(CCSPlayerController player, float x, float y, float z, long decalId)
    {
        try
        {
            var decal = _plugin.ActiveMapDecals.FirstOrDefault(d => d.Id == decalId);
            if (decal == null)
            {
                player.PrintToChat(" [MapDecals] Decal not found.");
                return;
            }

            var pingPosition = new Vector(x, y, z);
            var eyeAngles = player.PlayerPawn?.Value?.EyeAngles ?? new QAngle(0, 0, 0);

            // Calculate new decal placement
            var (position, angles) = _plugin.DecalFunctions.CalculateDecalPlacement(pingPosition, eyeAngles);

            // Update decal
            decal.Position = $"{position.X} {position.Y} {position.Z}";
            decal.Angles = $"{angles.X} {angles.Y} {angles.Z}";

            // Save to database
            Server.NextFrame(async () =>
            {
                try
                {
                    await _plugin.DatabaseService.UpdateDecalAsync(decal);

                    // Despawn and respawn the entity
                    _plugin.DecalFunctions.DespawnDecal(decalId);
                    _plugin.DecalFunctions.SpawnDecal(decal);

                    player.PrintToChat(" [MapDecals] Decal repositioned successfully!");

                    // Reopen edit menu
                    _plugin.MenuManager?.OpenEditDecalMenu(player, decal);
                }
                catch (Exception ex)
                {
                    _plugin.Logger.LogError($"Error repositioning decal: {ex.Message}");
                    player.PrintToChat(" [MapDecals] Error repositioning decal. Please try again.");
                }
            });
        }
        catch (Exception ex)
        {
            _plugin.Logger.LogError($"Error handling decal reposition: {ex.Message}");
            player.PrintToChat(" [MapDecals] Error repositioning decal. Please try again.");
        }
    }
}
