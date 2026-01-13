using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using Microsoft.Extensions.Logging;
using MapDecals.Database.Models;

namespace MapDecals.Menus;

public class MenuManager
{
    private readonly MapDecals _plugin;

    public MenuManager(MapDecals plugin)
    {
        _plugin = plugin;
    }

    public void OpenMainMenu(CCSPlayerController player)
    {
        var menu = new ChatMenu("Map Decals Menu");

        menu.AddMenuOption("Place New Decal", (player, option) =>
        {
            OpenPlaceDecalMenu(player);
        });

        menu.AddMenuOption("Edit Existing Decals", (player, option) =>
        {
            OpenEditDecalsListMenu(player);
        });

        CounterStrikeSharp.API.Modules.Menu.MenuManager.OpenChatMenu(player, menu);
    }

    public void OpenPlaceDecalMenu(CCSPlayerController player)
    {
        var menu = new ChatMenu("Select Decal to Place");

        foreach (var decalConfig in _plugin.Config.Props)
        {
            // Check permission
            if (!string.IsNullOrEmpty(decalConfig.ShowPermission))
            {
                if (!_plugin.DecalFunctions.PlayerHasPermission(player, decalConfig.ShowPermission))
                    continue;
            }

            menu.AddMenuOption(decalConfig.Name, (player, option) =>
            {
                var steamId = player.SteamID.ToString();
                _plugin.PlacementMode[steamId] = decalConfig.UniqId;
                player.PrintToChat(" [MapDecals] Ping where you want to place the decal.");
                
            });
        }

        menu.AddMenuOption("Back", (player, option) =>
        {
            OpenMainMenu(player);
        });

        CounterStrikeSharp.API.Modules.Menu.MenuManager.OpenChatMenu(player, menu);
    }

    public void OpenEditDecalsListMenu(CCSPlayerController player)
    {
        var menu = new ChatMenu("Edit Decals");

        var mapDecals = _plugin.ActiveMapDecals.ToList();
        if (mapDecals.Count == 0)
        {
            player.PrintToChat(" [MapDecals] No decals found on this map.");
            return;
        }

        foreach (var decal in mapDecals)
        {
            var status = decal.IsActive ? "[Active]" : "[Disabled]";
            menu.AddMenuOption($"{decal.DecalName} {status}", (player, option) =>
            {
                OpenEditDecalMenu(player, decal);
            });
        }

        menu.AddMenuOption("Back", (player, option) =>
        {
            OpenMainMenu(player);
        });

        CounterStrikeSharp.API.Modules.Menu.MenuManager.OpenChatMenu(player, menu);
    }

    public void OpenEditDecalMenu(CCSPlayerController player, MapDecal decal)
    {
        var menu = new ChatMenu($"Edit: {decal.DecalName}");

        menu.AddMenuOption("Reposition", (player, option) =>
        {
            var steamId = player.SteamID.ToString();
            _plugin.RepositionMode[steamId] = decal.Id;
            player.PrintToChat(" [MapDecals] Ping the new location for the decal.");
            
        });

        menu.AddMenuOption("Adjust Width", (player, option) =>
        {
            OpenWidthMenu(player, decal);
        });

        menu.AddMenuOption("Adjust Height", (player, option) =>
        {
            OpenHeightMenu(player, decal);
        });

        menu.AddMenuOption("Adjust Depth", (player, option) =>
        {
            OpenDepthMenu(player, decal);
        });

        var forceText = decal.ForceOnVip ? "Force on VIP: ON" : "Force on VIP: OFF";
        menu.AddMenuOption(forceText, (player, option) =>
        {
            ToggleForceOnVip(player, decal);
        });

        var activeText = decal.IsActive ? "Disable Decal" : "Enable Decal";
        menu.AddMenuOption(activeText, (player, option) =>
        {
            ToggleDecalActive(player, decal);
        });

        menu.AddMenuOption("Delete Decal", (player, option) =>
        {
            DeleteDecal(player, decal);
        });

        menu.AddMenuOption("Back", (player, option) =>
        {
            OpenEditDecalsListMenu(player);
        });

        CounterStrikeSharp.API.Modules.Menu.MenuManager.OpenChatMenu(player, menu);
    }

    private void OpenWidthMenu(CCSPlayerController player, MapDecal decal)
    {
        var menu = new ChatMenu("Adjust Width");

        float[] presets = { 64f, 128f, 256f, 512f };
        foreach (var preset in presets)
        {
            menu.AddMenuOption($"{preset}", (player, option) =>
            {
                UpdateDecalDimension(player, decal, "width", preset);
            });
        }

        menu.AddMenuOption("Back", (player, option) =>
        {
            OpenEditDecalMenu(player, decal);
        });

        CounterStrikeSharp.API.Modules.Menu.MenuManager.OpenChatMenu(player, menu);
    }

    private void OpenHeightMenu(CCSPlayerController player, MapDecal decal)
    {
        var menu = new ChatMenu("Adjust Height");

        float[] presets = { 64f, 128f, 256f, 512f };
        foreach (var preset in presets)
        {
            menu.AddMenuOption($"{preset}", (player, option) =>
            {
                UpdateDecalDimension(player, decal, "height", preset);
            });
        }

        menu.AddMenuOption("Back", (player, option) =>
        {
            OpenEditDecalMenu(player, decal);
        });

        CounterStrikeSharp.API.Modules.Menu.MenuManager.OpenChatMenu(player, menu);
    }

    private void OpenDepthMenu(CCSPlayerController player, MapDecal decal)
    {
        var menu = new ChatMenu("Adjust Depth");

        int[] presets = { 4, 8, 12, 16, 24 };
        foreach (var preset in presets)
        {
            menu.AddMenuOption($"{preset}", (player, option) =>
            {
                UpdateDecalDimension(player, decal, "depth", preset);
            });
        }

        menu.AddMenuOption("Back", (player, option) =>
        {
            OpenEditDecalMenu(player, decal);
        });

        CounterStrikeSharp.API.Modules.Menu.MenuManager.OpenChatMenu(player, menu);
    }

    private void UpdateDecalDimension(CCSPlayerController player, MapDecal decal, string dimension, float value)
    {
        try
        {
            switch (dimension.ToLower())
            {
                case "width":
                    decal.Width = value;
                    break;
                case "height":
                    decal.Height = value;
                    break;
                case "depth":
                    decal.Depth = (int)value;
                    break;
            }

            Server.NextFrame(async () =>
            {
                try
                {
                    await _plugin.DatabaseService.UpdateDecalAsync(decal);
                    _plugin.DecalFunctions.DespawnDecal(decal.Id);
                    _plugin.DecalFunctions.SpawnDecal(decal);
                    player.PrintToChat($" [MapDecals] Decal {dimension} updated to {value}!");
                    OpenEditDecalMenu(player, decal);
                }
                catch (Exception ex)
                {
                    _plugin.Logger.LogError($"Error updating decal dimension: {ex.Message}");
                    player.PrintToChat(" [MapDecals] Error updating decal.");
                }
            });
        }
        catch (Exception ex)
        {
            _plugin.Logger.LogError($"Error in UpdateDecalDimension: {ex.Message}");
            player.PrintToChat(" [MapDecals] Error updating decal.");
        }
    }

    private void ToggleForceOnVip(CCSPlayerController player, MapDecal decal)
    {
        try
        {
            decal.ForceOnVip = !decal.ForceOnVip;

            Server.NextFrame(async () =>
            {
                try
                {
                    await _plugin.DatabaseService.UpdateDecalAsync(decal);
                    var status = decal.ForceOnVip ? "ON" : "OFF";
                    player.PrintToChat($" [MapDecals] Force on VIP set to {status}!");
                    OpenEditDecalMenu(player, decal);
                }
                catch (Exception ex)
                {
                    _plugin.Logger.LogError($"Error toggling force on VIP: {ex.Message}");
                    player.PrintToChat(" [MapDecals] Error updating decal.");
                }
            });
        }
        catch (Exception ex)
        {
            _plugin.Logger.LogError($"Error in ToggleForceOnVip: {ex.Message}");
            player.PrintToChat(" [MapDecals] Error updating decal.");
        }
    }

    private void ToggleDecalActive(CCSPlayerController player, MapDecal decal)
    {
        try
        {
            decal.IsActive = !decal.IsActive;

            Server.NextFrame(async () =>
            {
                try
                {
                    await _plugin.DatabaseService.UpdateDecalAsync(decal);
                    
                    if (decal.IsActive)
                    {
                        _plugin.DecalFunctions.SpawnDecal(decal);
                        player.PrintToChat(" [MapDecals] Decal enabled!");
                    }
                    else
                    {
                        _plugin.DecalFunctions.DespawnDecal(decal.Id);
                        player.PrintToChat(" [MapDecals] Decal disabled!");
                    }
                    
                    OpenEditDecalMenu(player, decal);
                }
                catch (Exception ex)
                {
                    _plugin.Logger.LogError($"Error toggling decal active: {ex.Message}");
                    player.PrintToChat(" [MapDecals] Error updating decal.");
                }
            });
        }
        catch (Exception ex)
        {
            _plugin.Logger.LogError($"Error in ToggleDecalActive: {ex.Message}");
            player.PrintToChat(" [MapDecals] Error updating decal.");
        }
    }

    private void DeleteDecal(CCSPlayerController player, MapDecal decal)
    {
        try
        {
            Server.NextFrame(async () =>
            {
                try
                {
                    await _plugin.DatabaseService.DeleteDecalAsync(decal.Id);
                    _plugin.DecalFunctions.DespawnDecal(decal.Id);
                    _plugin.ActiveMapDecals.Remove(decal);
                    player.PrintToChat(" [MapDecals] Decal deleted!");
                    OpenEditDecalsListMenu(player);
                }
                catch (Exception ex)
                {
                    _plugin.Logger.LogError($"Error deleting decal: {ex.Message}");
                    player.PrintToChat(" [MapDecals] Error deleting decal.");
                }
            });
        }
        catch (Exception ex)
        {
            _plugin.Logger.LogError($"Error in DeleteDecal: {ex.Message}");
            player.PrintToChat(" [MapDecals] Error deleting decal.");
        }
    }
}
