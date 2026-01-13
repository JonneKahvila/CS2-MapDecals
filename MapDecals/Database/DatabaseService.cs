using System.Data;
using Dapper;
using MapDecals.Database.Models;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;

namespace MapDecals.Database;

public class DatabaseService
{
    private readonly string _connectionString;
    private readonly string _databaseType;

    public DatabaseService(string connectionString, string databaseType)
    {
        _connectionString = connectionString;
        _databaseType = databaseType.ToLower();
    }

    private IDbConnection CreateConnection()
    {
        return _databaseType switch
        {
            "mysql" => new MySqlConnection(_connectionString),
            "postgresql" or "postgres" => new NpgsqlConnection(_connectionString),
            "sqlite" => new SqliteConnection(_connectionString),
            _ => throw new ArgumentException($"Unsupported database type: {_databaseType}")
        };
    }

    public async Task InitializeDatabaseAsync()
    {
        using var connection = CreateConnection();
        await connection.OpenAsync();

        string createTableQuery = _databaseType switch
        {
            "mysql" => @"
                CREATE TABLE IF NOT EXISTS cc_mapdecals (
                    id BIGINT PRIMARY KEY AUTO_INCREMENT,
                    map VARCHAR(64) NOT NULL,
                    decal_id VARCHAR(64) NOT NULL,
                    decal_name VARCHAR(64) NOT NULL,
                    position VARCHAR(64) NOT NULL,
                    angles VARCHAR(64) NOT NULL,
                    depth INT NOT NULL DEFAULT 12,
                    width FLOAT NOT NULL DEFAULT 128,
                    height FLOAT NOT NULL DEFAULT 128,
                    force_on_vip BOOLEAN NOT NULL DEFAULT FALSE,
                    is_active BOOLEAN NOT NULL DEFAULT TRUE,
                    INDEX idx_map (map)
                );",
            "postgresql" or "postgres" => @"
                CREATE TABLE IF NOT EXISTS cc_mapdecals (
                    id BIGSERIAL PRIMARY KEY,
                    map VARCHAR(64) NOT NULL,
                    decal_id VARCHAR(64) NOT NULL,
                    decal_name VARCHAR(64) NOT NULL,
                    position VARCHAR(64) NOT NULL,
                    angles VARCHAR(64) NOT NULL,
                    depth INT NOT NULL DEFAULT 12,
                    width FLOAT NOT NULL DEFAULT 128,
                    height FLOAT NOT NULL DEFAULT 128,
                    force_on_vip BOOLEAN NOT NULL DEFAULT FALSE,
                    is_active BOOLEAN NOT NULL DEFAULT TRUE
                );
                CREATE INDEX IF NOT EXISTS idx_map ON cc_mapdecals(map);",
            "sqlite" => @"
                CREATE TABLE IF NOT EXISTS cc_mapdecals (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    map TEXT NOT NULL,
                    decal_id TEXT NOT NULL,
                    decal_name TEXT NOT NULL,
                    position TEXT NOT NULL,
                    angles TEXT NOT NULL,
                    depth INTEGER NOT NULL DEFAULT 12,
                    width REAL NOT NULL DEFAULT 128,
                    height REAL NOT NULL DEFAULT 128,
                    force_on_vip INTEGER NOT NULL DEFAULT 0,
                    is_active INTEGER NOT NULL DEFAULT 1
                );
                CREATE INDEX IF NOT EXISTS idx_map ON cc_mapdecals(map);",
            _ => throw new ArgumentException($"Unsupported database type: {_databaseType}")
        };

        await connection.ExecuteAsync(createTableQuery);

        // Create player preferences table
        string createPreferencesQuery = _databaseType switch
        {
            "mysql" => @"
                CREATE TABLE IF NOT EXISTS cc_mapdecals_preferences (
                    steam_id VARCHAR(64) PRIMARY KEY,
                    decals_enabled BOOLEAN NOT NULL DEFAULT TRUE
                );",
            "postgresql" or "postgres" => @"
                CREATE TABLE IF NOT EXISTS cc_mapdecals_preferences (
                    steam_id VARCHAR(64) PRIMARY KEY,
                    decals_enabled BOOLEAN NOT NULL DEFAULT TRUE
                );",
            "sqlite" => @"
                CREATE TABLE IF NOT EXISTS cc_mapdecals_preferences (
                    steam_id TEXT PRIMARY KEY,
                    decals_enabled INTEGER NOT NULL DEFAULT 1
                );",
            _ => throw new ArgumentException($"Unsupported database type: {_databaseType}")
        };

        await connection.ExecuteAsync(createPreferencesQuery);
    }

    public async Task<List<MapDecal>> GetMapDecalsAsync(string mapName)
    {
        using var connection = CreateConnection();
        var decals = await connection.QueryAsync<MapDecal>(
            "SELECT * FROM cc_mapdecals WHERE map = @Map",
            new { Map = mapName });
        return decals.ToList();
    }

    public async Task<MapDecal?> GetDecalByIdAsync(long id)
    {
        using var connection = CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<MapDecal>(
            "SELECT * FROM cc_mapdecals WHERE id = @Id",
            new { Id = id });
    }

    public async Task<long> InsertDecalAsync(MapDecal decal)
    {
        using var connection = CreateConnection();
        
        string insertQuery = _databaseType switch
        {
            "mysql" => @"
                INSERT INTO cc_mapdecals (map, decal_id, decal_name, position, angles, depth, width, height, force_on_vip, is_active)
                VALUES (@Map, @DecalId, @DecalName, @Position, @Angles, @Depth, @Width, @Height, @ForceOnVip, @IsActive);
                SELECT LAST_INSERT_ID();",
            "postgresql" or "postgres" => @"
                INSERT INTO cc_mapdecals (map, decal_id, decal_name, position, angles, depth, width, height, force_on_vip, is_active)
                VALUES (@Map, @DecalId, @DecalName, @Position, @Angles, @Depth, @Width, @Height, @ForceOnVip, @IsActive)
                RETURNING id;",
            "sqlite" => @"
                INSERT INTO cc_mapdecals (map, decal_id, decal_name, position, angles, depth, width, height, force_on_vip, is_active)
                VALUES (@Map, @DecalId, @DecalName, @Position, @Angles, @Depth, @Width, @Height, @ForceOnVip, @IsActive);
                SELECT last_insert_rowid();",
            _ => throw new ArgumentException($"Unsupported database type: {_databaseType}")
        };

        return await connection.ExecuteScalarAsync<long>(insertQuery, decal);
    }

    public async Task UpdateDecalAsync(MapDecal decal)
    {
        using var connection = CreateConnection();
        await connection.ExecuteAsync(@"
            UPDATE cc_mapdecals 
            SET position = @Position, angles = @Angles, depth = @Depth, 
                width = @Width, height = @Height, force_on_vip = @ForceOnVip, 
                is_active = @IsActive
            WHERE id = @Id",
            decal);
    }

    public async Task DeleteDecalAsync(long id)
    {
        using var connection = CreateConnection();
        await connection.ExecuteAsync("DELETE FROM cc_mapdecals WHERE id = @Id", new { Id = id });
    }

    public async Task<bool> GetPlayerDecalPreferenceAsync(string steamId)
    {
        using var connection = CreateConnection();
        var result = await connection.QueryFirstOrDefaultAsync<int?>(
            "SELECT decals_enabled FROM cc_mapdecals_preferences WHERE steam_id = @SteamId",
            new { SteamId = steamId });
        return result == null || result == 1;
    }

    public async Task SetPlayerDecalPreferenceAsync(string steamId, bool enabled)
    {
        using var connection = CreateConnection();
        
        string upsertQuery = _databaseType switch
        {
            "mysql" => @"
                INSERT INTO cc_mapdecals_preferences (steam_id, decals_enabled)
                VALUES (@SteamId, @Enabled)
                ON DUPLICATE KEY UPDATE decals_enabled = @Enabled;",
            "postgresql" or "postgres" => @"
                INSERT INTO cc_mapdecals_preferences (steam_id, decals_enabled)
                VALUES (@SteamId, @Enabled)
                ON CONFLICT (steam_id) DO UPDATE SET decals_enabled = @Enabled;",
            "sqlite" => @"
                INSERT OR REPLACE INTO cc_mapdecals_preferences (steam_id, decals_enabled)
                VALUES (@SteamId, @Enabled);",
            _ => throw new ArgumentException($"Unsupported database type: {_databaseType}")
        };

        await connection.ExecuteAsync(upsertQuery, new { SteamId = steamId, Enabled = enabled ? 1 : 0 });
    }
}
