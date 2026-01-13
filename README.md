# CS2-MapDecals

A CounterStrikeSharp plugin that allows CS2 server owners to place decals on maps at predefined locations. Players with admin permissions can place decals using ping locations, edit their properties (width, height, depth, position), and toggle visibility.

## Features

- **Decal Placement**: Place custom decals on maps using the ping system (right-click ping)
- **Decal Management**: Edit existing decals (reposition, resize, enable/disable)
- **Permission System**: Control who can place and see decals using CS# permissions
- **Player Preferences**: Players can toggle decal visibility with a command
- **Database Support**: Supports MySQL, PostgreSQL, and SQLite
- **Multi-Map Support**: Decals are saved per-map
- **Force on VIP**: Option to force certain decals to always show for VIP players

## Installation

1. Install [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) on your CS2 server
2. Download the latest release of MapDecals
3. Extract the plugin files to `game/csgo/addons/counterstrikesharp/plugins/MapDecals/`
4. Configure the plugin (see Configuration section)
5. Restart the server or use `css_plugins load MapDecals`

## Configuration

The plugin configuration file is located at:
`game/csgo/addons/counterstrikesharp/configs/plugins/MapDecals/MapDecals.json`

### Example Configuration

```json
{
  "DatabaseConnection": "Server=localhost;Database=cs2;User=root;Password=;",
  "DatabaseType": "mysql",
  "Props": [
    {
      "UniqId": "exampleTexture",
      "Name": "Example Name",
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

### Configuration Options

#### Database Settings
- `DatabaseConnection`: Connection string for your database
- `DatabaseType`: Type of database (`mysql`, `postgresql`, or `sqlite`)

#### Decals Configuration
- `Props`: Array of available decals
  - `UniqId`: Unique identifier for the decal
  - `Name`: Display name shown in menus
  - `Material`: Path to the decal material (VMAT file)
  - `ShowPermission`: Optional permission required to see the decal

#### Command Configuration
- `PlaceDecalCommands`: Configuration for the placement command
  - `Command`: Main command name
  - `Aliases`: Alternative command names
  - `Permission`: Permission required to use the command
  
- `AdToggleCommands`: Configuration for the toggle command
  - `Command`: Main command name
  - `Aliases`: Alternative command names
  - `Permission`: Permission required to use the command

### Database Connection Strings

**MySQL:**
```
Server=localhost;Database=cs2;User=root;Password=yourpassword;
```

**PostgreSQL:**
```
Host=localhost;Database=cs2;Username=postgres;Password=yourpassword;
```

**SQLite:**
```
Data Source=/path/to/database.db;
```

## Commands

### Place/Manage Decals
- `!mapdecal` (or configured command)
- Opens the main menu for placing and managing decals
- Requires admin permission (default: `cc-mapdecals.admin`)
- Must be alive to use

### Toggle Decal Visibility
- `!decal` (or configured command)
- Toggles visibility of non-forced decals
- Requires VIP permission (default: `cc-mapdecals.vip`)
- Preference is saved to database

## Usage

### Placing a Decal

1. Use `!mapdecal` command to open the menu
2. Select "Place New Decal"
3. Choose a decal from the list
4. Right-click ping (Radar ping) where you want to place the decal
5. The decal will be placed and the edit menu will open automatically

### Editing a Decal

1. Use `!mapdecal` command to open the menu
2. Select "Edit Existing Decals"
3. Choose the decal you want to edit
4. Select an edit option:
   - **Reposition**: Ping a new location for the decal
   - **Adjust Width**: Change the decal width (64, 128, 256, 512)
   - **Adjust Height**: Change the decal height (64, 128, 256, 512)
   - **Adjust Depth**: Change the decal depth (4, 8, 12, 16, 24)
   - **Force on VIP**: Toggle whether the decal is always visible to VIPs
   - **Enable/Disable**: Toggle the decal on/off
   - **Delete Decal**: Remove the decal permanently

### Toggling Decals

Players with VIP permission can toggle decal visibility:
- Use `!decal` command to toggle decals on/off
- This only affects non-forced decals
- Preference is saved across reconnects

## Permissions

Configure permissions in CounterStrikeSharp's admin system:

```json
{
  "Groups": [
    {
      "Name": "Admin",
      "Permissions": [
        "cc-mapdecals.admin"
      ]
    },
    {
      "Name": "VIP",
      "Permissions": [
        "cc-mapdecals.vip"
      ]
    }
  ]
}
```

## Database Schema

The plugin automatically creates the following tables:

### cc_mapdecals
Stores decal information:
- `id`: Unique decal ID
- `map`: Map name
- `decal_id`: Reference to config UniqId
- `decal_name`: Display name
- `position`: 3D position (X Y Z)
- `angles`: 3D rotation (X Y Z)
- `depth`: Decal depth
- `width`: Decal width
- `height`: Decal height
- `force_on_vip`: Whether to force show for VIPs
- `is_active`: Whether the decal is active

### cc_mapdecals_preferences
Stores player preferences:
- `steam_id`: Player's SteamID64
- `decals_enabled`: Whether decals are enabled (1/0)

## Building from Source

### Requirements
- .NET 8.0 SDK
- CounterStrikeSharp.API

### Build Instructions

1. Clone the repository:
```bash
git clone https://github.com/JonneKahvila/CS2-MapDecals.git
cd CS2-MapDecals
```

2. Build the project:
```bash
cd MapDecals
dotnet build -c Release
```

3. The compiled plugin will be in `bin/Release/net8.0/`

## Troubleshooting

### Decals not appearing
- Check that the material path is correct in the configuration
- Verify the VMAT file exists on the server
- Ensure players have the required permission to see the decal
- Check if players have toggled decals off

### Database connection errors
- Verify your connection string is correct
- Ensure the database server is running
- Check that the database user has proper permissions

### Commands not working
- Verify the command names in the configuration
- Check player permissions in the admin configuration
- Ensure the plugin is loaded (`css_plugins list`)

## Known Limitations

Due to CounterStrikeSharp API limitations, some features have been simplified:

- Decal entity properties (width, height, depth, material) may not be fully customizable through the entity API
- Per-player transmit control is simplified and may show/hide decals for all players
- Entity property configuration is stored in the database but may not affect the visual appearance

These limitations are due to the CS# entity API not exposing all env_decal properties. Future updates to CounterStrikeSharp may allow full property control.

## Credits

- Original SwiftlyS2 version: [CS2-MapDecals-SwiftlyS2](https://github.com/JonneKahvila/CS2-MapDecals-SwiftlyS2)
- Ported to CounterStrikeSharp by JonneKahvila

## License

This project is provided as-is for use with Counter-Strike 2 servers.
