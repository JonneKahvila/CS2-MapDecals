# CS2-MapDecals CounterStrikeSharp Implementation Notes

## Implementation Summary

This document provides technical details about the CounterStrikeSharp (CS#) port of the MapDecals plugin.

## Architecture

### Project Structure
```
MapDecals/
├── MapDecals.cs                 # Main plugin class
├── Config/
│   └── MapDecalsConfig.cs      # Configuration models
├── Database/
│   ├── DatabaseService.cs      # Database operations
│   └── Models/
│       └── MapDecal.cs         # Decal data model
├── Commands/
│   └── CommandHandlers.cs      # Command implementations
├── Events/
│   └── EventHandlers.cs        # Game event handlers
├── Functions/
│   └── DecalFunctions.cs       # Decal entity management
├── Menus/
│   └── MenuManager.cs          # Chat menu system
└── MapDecals.csproj            # Project file
```

### Dependencies
- **CounterStrikeSharp.API** (v1.0.256): Core CS# framework
- **Dapper** (v2.1.35): Lightweight ORM for database operations
- **MySqlConnector** (v2.3.5): MySQL database driver
- **Npgsql** (v8.0.5): PostgreSQL database driver
- **Microsoft.Data.Sqlite** (v8.0.1): SQLite database driver

## Key Implementation Details

### Database Support
The plugin supports three database types:
- **MySQL**: Production-ready, recommended for large servers
- **PostgreSQL**: Advanced features, good performance
- **SQLite**: File-based, good for testing or small servers

Database tables:
- `cc_mapdecals`: Stores decal configurations and positions
- `cc_mapdecals_preferences`: Stores player visibility preferences

### Configuration System
Uses JSON-based configuration with:
- Decal definitions (material paths, permissions)
- Command configuration (names, aliases, permissions)
- Database connection settings

### Command System
Two main commands:
1. **Place Decal** (default: `!mapdecal`)
   - Permission-based access control
   - Opens interactive menu system
   - Requires player to be alive

2. **Toggle Decal** (default: `!decal`)
   - Toggles personal decal visibility
   - Preference persisted in database
   - VIP permission required

### Menu System
Interactive chat-based menus using CS# MenuManager:
- Main menu (place/edit options)
- Decal selection menu
- Edit menu with multiple options
- Dimension adjustment submenus

### Event System
Hooks into game events:
- **OnRoundStart**: Spawns all active decals
- **OnPlayerConnectFull**: Loads player preferences
- **OnPlayerDisconnect**: Cleanup
- **OnPlayerPing**: Handles decal placement/repositioning

### Decal Placement Logic
1. Player pings a location (right-click ping)
2. Plugin calculates eye angles and position
3. Decal is placed 2 units backward from ping point
4. Floor detection: If looking down steeply (angle < -0.90), place on floor
5. Wall placement: Apply 90° pitch rotation for walls
6. Automatically saved to database

## API Adaptations from SwiftlyS2

### Major Changes
1. **Plugin Base Class**: `SwiftlyS2.Plugins.BasePlugin` → `CounterStrikeSharp.API.Core.BasePlugin`
2. **Configuration**: SwiftlyS2 built-in → CS# `IPluginConfig<T>` interface
3. **Database**: SwiftlyS2 built-in → Manual Dapper implementation
4. **Commands**: SwiftlyS2 decorators → CS# `AddCommand()` and attributes
5. **Events**: SwiftlyS2 methods → CS# `RegisterEventHandler<T>()`
6. **Menus**: SwiftlyS2 menu system → CS# `MenuManager` API
7. **Permissions**: SwiftlyS2 built-in → CS# `AdminManager`
8. **Logging**: SwiftlyS2 logger → Microsoft.Extensions.Logging
9. **Entities**: `Core.EntitySystem.CreateEntityByDesignerName` → `Utilities.CreateEntityByName<T>()`

### API Limitations
Due to CS# entity API constraints, some features are simplified:

1. **Entity Properties**: CEnvDecal properties (width, height, depth, material) may not be fully exposed
2. **Transmit Control**: Per-player visibility is simplified
3. **Material Assignment**: Cannot directly set decal material through entity API

These limitations are documented and may be resolved in future CS# updates.

## Security Considerations

1. **SQL Injection**: Protected by Dapper parameterized queries
2. **Permission Checks**: All commands validate permissions before execution
3. **Input Validation**: Player inputs are validated before database operations
4. **Async Safety**: Database operations use proper async/await patterns

## Performance Considerations

1. **Database Connections**: Created and disposed per operation (connection pooling handled by drivers)
2. **Entity Management**: Entities stored in dictionary for quick lookups
3. **Event Handlers**: Use `Server.NextFrame()` for async operations to avoid blocking
4. **Memory Management**: Proper cleanup in Unload method

## Configuration Example

```json
{
  "DatabaseConnection": "Server=localhost;Database=cs2;User=root;Password=yourpassword;",
  "DatabaseType": "mysql",
  "Props": [
    {
      "UniqId": "exampleTexture",
      "Name": "Example Decal",
      "Material": "materials/Example/exampleTexture.vmat",
      "ShowPermission": ""
    }
  ],
  "PlaceDecalCommands": {
    "Command": "mapdecal",
    "Aliases": ["paintmapdecal", "placedecals", "placedecal"],
    "Permission": "cc-mapdecals.admin"
  },
  "AdToggleCommands": {
    "Command": "decal",
    "Aliases": ["decals"],
    "Permission": "cc-mapdecals.vip"
  }
}
```

## Installation

1. Install CounterStrikeSharp on your CS2 server
2. Copy `MapDecals.dll` to `game/csgo/addons/counterstrikesharp/plugins/MapDecals/`
3. Create configuration file at `game/csgo/addons/counterstrikesharp/configs/plugins/MapDecals/MapDecals.json`
4. Configure database connection and decal properties
5. Restart server or use `css_plugins load MapDecals`

## Testing Checklist

- [ ] Plugin loads without errors
- [ ] Database tables created successfully
- [ ] Commands registered and accessible
- [ ] Permissions enforced correctly
- [ ] Menu navigation works
- [ ] Decal placement via ping works
- [ ] Decal properties saved to database
- [ ] Decal entities spawn correctly
- [ ] Player preferences persist across reconnects
- [ ] Decal editing functions work
- [ ] Decal deletion works
- [ ] Multiple database types tested

## Future Enhancements

1. **Enhanced Entity Control**: When CS# API exposes more entity properties
2. **Advanced Permissions**: Per-decal permission system
3. **Decal Templates**: Preset configurations for common use cases
4. **Admin Panel**: Web-based management interface
5. **Decal Previews**: Preview decals before placing
6. **Batch Operations**: Bulk import/export of decals
7. **Map Transitions**: Preserve decals across map changes
8. **Performance Optimizations**: Caching, lazy loading

## Troubleshooting

### Plugin doesn't load
- Check CS# version compatibility
- Verify all dependencies are present
- Check server console for error messages

### Database connection fails
- Verify connection string is correct
- Check database server is running
- Verify database user has proper permissions

### Decals don't appear
- Check material paths are correct
- Verify decals are marked as active
- Check player permissions
- Verify player hasn't toggled decals off

### Commands don't work
- Check command names in configuration
- Verify player has required permissions
- Check player is alive (for placement command)

## Support

For issues, questions, or contributions:
- GitHub: https://github.com/JonneKahvila/CS2-MapDecals
- Original SwiftlyS2 version: https://github.com/JonneKahvila/CS2-MapDecals-SwiftlyS2

## License

This project is provided as-is for use with Counter-Strike 2 servers.
