using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API;
using Microsoft.Extensions.Logging;

namespace MapDecals.Commands;

public class CommandHandlers
{
    private readonly MapDecals _plugin;

    public CommandHandlers(MapDecals plugin)
    {
        _plugin = plugin;
    }

    public void RegisterCommands()
    {
        // Register place decal command and aliases
        var placeCmd = _plugin.Config.PlaceDecalCommands;
        _plugin.AddCommand($"css_{placeCmd.Command}", "Open decal placement menu", OnPlaceDecalCommand);
        
        foreach (var alias in placeCmd.Aliases)
        {
            _plugin.AddCommand($"css_{alias}", "Open decal placement menu", OnPlaceDecalCommand);
        }

        // Register toggle decal command and aliases
        var toggleCmd = _plugin.Config.AdToggleCommands;
        _plugin.AddCommand($"css_{toggleCmd.Command}", "Toggle decal visibility", OnToggleDecalCommand);
        
        foreach (var alias in toggleCmd.Aliases)
        {
            _plugin.AddCommand($"css_{alias}", "Toggle decal visibility", OnToggleDecalCommand);
        }
    }

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnPlaceDecalCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null || !player.IsValid)
            return;

        // Check permission
        var permission = _plugin.Config.PlaceDecalCommands.Permission;
        if (!string.IsNullOrEmpty(permission) && !AdminManager.PlayerHasPermissions(player, permission))
        {
            player.PrintToChat(" [MapDecals] You don't have permission to use this command.");
            return;
        }

        // Check if player is alive
        if (player.PlayerPawn?.Value == null || !player.PlayerPawn.Value.IsValid || player.PlayerPawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE)
        {
            player.PrintToChat(" [MapDecals] You must be alive to place decals.");
            return;
        }

        // Open main menu
        _plugin.MenuManager?.OpenMainMenu(player);
    }

    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnToggleDecalCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player == null || !player.IsValid)
            return;

        // Check permission
        var permission = _plugin.Config.AdToggleCommands.Permission;
        if (!string.IsNullOrEmpty(permission) && !AdminManager.PlayerHasPermissions(player, permission))
        {
            player.PrintToChat(" [MapDecals] You don't have permission to use this command.");
            return;
        }

        var steamId = player.SteamID.ToString();
        
        // Toggle preference
        Server.NextFrame(async () =>
        {
            try
            {
                var currentPref = await _plugin.DatabaseService.GetPlayerDecalPreferenceAsync(steamId);
                var newPref = !currentPref;
                await _plugin.DatabaseService.SetPlayerDecalPreferenceAsync(steamId, newPref);
                
                // Update in memory
                _plugin.PlayerPreferences[steamId] = newPref;

                // Notify player
                var status = newPref ? "enabled" : "disabled";
                player.PrintToChat($" [MapDecals] Decals are now {status}.");
            }
            catch (Exception ex)
            {
                _plugin.Logger.LogError($"Error toggling decal preference: {ex.Message}");
                player.PrintToChat(" [MapDecals] Error toggling decals. Please try again.");
            }
        });
    }
}
