using CounterStrikeSharp.API;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using MapDecals.Database.Models;

namespace MapDecals.Functions;

public class DecalFunctions
{
    private readonly MapDecals _plugin;
    private readonly Dictionary<long, CEnvDecal> _spawnedDecals = new();

    public DecalFunctions(MapDecals plugin)
    {
        _plugin = plugin;
    }

    public void SpawnDecal(MapDecal decal)
    {
        try
        {
            // Find the decal config
            var decalConfig = _plugin.Config.Props.FirstOrDefault(p => p.UniqId == decal.DecalId);
            if (decalConfig == null)
            {
                _plugin.Logger?.LogWarning($"Decal config not found for {decal.DecalId}");
                return;
            }

            // Parse position and angles
            var positionParts = decal.Position.Split(' ');
            var anglesParts = decal.Angles.Split(' ');

            if (positionParts.Length != 3 || anglesParts.Length != 3)
            {
                _plugin.Logger?.LogError($"Invalid position or angles for decal {decal.Id}");
                return;
            }

            var position = new Vector(
                float.Parse(positionParts[0]),
                float.Parse(positionParts[1]),
                float.Parse(positionParts[2])
            );

            var angles = new QAngle(
                float.Parse(anglesParts[0]),
                float.Parse(anglesParts[1]),
                float.Parse(anglesParts[2])
            );

            // Create the decal entity
            var entity = Utilities.CreateEntityByName<CEnvDecal>("env_decal");
            if (entity == null)
            {
                _plugin.Logger?.LogError($"Failed to create env_decal entity for decal {decal.Id}");
                return;
            }

            // Set entity properties
            entity.DecalName = decalConfig.Material;
            entity.Width = decal.Width;
            entity.Height = decal.Height;
            entity.Depth = decal.Depth;
            entity.RenderMode = RenderMode_t.kRenderNormal;
            entity.ProjectOnWorld = true;

            // Set position and angles
            entity.Teleport(position, angles, new Vector(0, 0, 0));

            // Spawn the entity
            entity.DispatchSpawn();

            // Store reference
            _spawnedDecals[decal.Id] = entity;

            _plugin.Logger?.LogInformation($"Spawned decal {decal.Id} at {decal.Position}");
        }
        catch (Exception ex)
        {
            _plugin.Logger?.LogError($"Error spawning decal {decal.Id}: {ex.Message}");
        }
    }

    public void DespawnDecal(long decalId)
    {
        if (_spawnedDecals.TryGetValue(decalId, out var entity))
        {
            entity.Remove();
            _spawnedDecals.Remove(decalId);
            _plugin.Logger?.LogInformation($"Despawned decal {decalId}");
        }
    }

    public void DespawnAllDecals()
    {
        foreach (var entity in _spawnedDecals.Values)
        {
            entity.Remove();
        }
        _spawnedDecals.Clear();
        _plugin.Logger?.LogInformation("Despawned all decals");
    }

    public void UpdateDecalTransmit(CEnvDecal entity, MapDecal decal)
    {
        if (!decal.IsActive)
        {
            // Hide from everyone if not active
            entity.AcceptInput("Disable");
            return;
        }

        entity.AcceptInput("Enable");
        
        // Note: Per-player transmit control would require hooks into the transmit system
        // This is a simplified version that enables/disables for everyone
        // For full per-player control, we would need to use SetTransmit hooks
    }

    public (Vector position, QAngle angles) CalculateDecalPlacement(Vector pingPosition, QAngle eyeAngles)
    {
        // Calculate decal position 2 units backward from ping
        var forward = new Vector(
            (float)Math.Cos(eyeAngles.Y * Math.PI / 180) * (float)Math.Cos(eyeAngles.X * Math.PI / 180),
            (float)Math.Sin(eyeAngles.Y * Math.PI / 180) * (float)Math.Cos(eyeAngles.X * Math.PI / 180),
            -(float)Math.Sin(eyeAngles.X * Math.PI / 180)
        );

        var decalPosition = new Vector(
            pingPosition.X - forward.X * 2,
            pingPosition.Y - forward.Y * 2,
            pingPosition.Z - forward.Z * 2
        );

        QAngle decalAngles;
        
        // If looking down steeply (eyeZ < -0.90), place on floor
        if (forward.Z < -0.90f)
        {
            decalAngles = new QAngle(0, eyeAngles.Y, 0);
        }
        else
        {
            // Place on wall with 90° pitch rotation
            decalAngles = new QAngle(eyeAngles.X + 90, eyeAngles.Y, 0);
        }

        return (decalPosition, decalAngles);
    }

    public bool PlayerHasPermission(CCSPlayerController player, string permission)
    {
        if (string.IsNullOrEmpty(permission))
            return true;

        // Check using CS# admin system
        return AdminManager.PlayerHasPermissions(player, permission);
    }

    public bool PlayerCanSeeDecal(CCSPlayerController player, MapDecal decal, bool playerPreference)
    {
        // Find decal config
        var decalConfig = _plugin.Config.Props.FirstOrDefault(p => p.UniqId == decal.DecalId);
        if (decalConfig == null)
            return false;

        // Check permission if required
        if (!string.IsNullOrEmpty(decalConfig.ShowPermission))
        {
            if (!PlayerHasPermission(player, decalConfig.ShowPermission))
                return false;
        }

        // If forced on VIP, always show (if player has permission)
        if (decal.ForceOnVip)
            return true;

        // Otherwise respect player preference
        return playerPreference;
    }

    public CEnvDecal? GetSpawnedDecal(long decalId)
    {
        return _spawnedDecals.GetValueOrDefault(decalId);
    }
}
