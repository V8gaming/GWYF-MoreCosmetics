# More Cosmetics

BepInEx **library** that adds custom clothing/cosmetics to [Gamble With Your Friends](https://store.steampowered.com/app/2327790/Gamble_With_Your_Friends/). Supports custom OBJ models, PNG textures, and vanilla model tinting - all driven by a simple JSON file. Cosmetic packs auto-discovered from plugin folders.

## How It Works

1. Install **More Cosmetics** (the library DLL) and at least one cosmetic pack
2. Cosmetic packs are just folders with a `cosmetics.json` - auto-discovered by the library
3. Cosmetics are injected into the game's wardrobe at runtime
4. All cosmetics are auto-unlocked by default

## Creating a Cosmetic Pack

Drop a folder in `BepInEx/plugins/` with:

```
MyCosmetics/
├── cosmetics.json        ← required - defines your cosmetics
├── models/              ← optional - .obj files from Blender
└── textures/            ← optional - .png texture files
```

### `cosmetics.json` Format

```json
[
  {
    "name": "Blue Floral Shirt",
    "type": "Clothing",
    "rarity": "Rare",
    "description": "A lovely floral shirt.",
    "model": { "vanilla": "Shirt 1" },
    "tint": [0.2, 0.5, 1.0]
  },
  {
    "name": "My Custom Hat",
    "type": "Hat",
    "model": { "obj": "my_hat.obj" },
    "texture": "my_hat.png"
  }
]
```

| Field | Default | Description |
|---|---|---|
| `name` | (required) | Display name in the wardrobe |
| `type` | (required) | `Hat`, `Hair`, `Mustache`, `Beard`, `Neckwear`, `Clothing`, `Facewear` |
| `model` | (required) | `{ "vanilla": "Name" }` to clone a vanilla mesh, `{ "obj": "file.obj" }` for a custom OBJ, or `{ "bundle": "file.bundle", "asset": "Name" }` for an AssetBundle |
| `rarity` | `Common` | `Common`, `Uncommon`, `Rare`, `Epic`, `Legendary` |
| `description` | `""` | Tooltip text |
| `texture` | `""` | PNG filename in `textures/` |
| `tint` | `[1,1,1]` | RGB color multiplier (used if no texture) |
| `shader` | `""` | Custom shader name (blank = game default) |

## For Mod Creators: Blender Workflow

See [`tools/README.md`](tools/README.md) for the full workflow: Unity FBX export → Blender UV remap & paint → OBJ/PNG export → JSON entry.

## Dependencies

- [BepInEx Pack](https://thunderstore.io/c/gamble-with-your-friends/p/BepInEx/BepInExPack/)

## Config

`BepInEx/config/com.morecosmetics.injector.cfg`:

| Setting | Default | Description |
|---|---|---|
| `Enabled` | `true` | Master toggle |
| `AutoDiscover` | `true` | Scan all plugin folders for `cosmetics.json` |
| `AutoUnlockModCosmetics` | `true` | Auto-unlock all loaded cosmetics |
| `CosmeticIdStart` | `10000` | Starting ID range |

## Build

```powershell
# Build library
dotnet build -c Release -p:GameDir="D:\Steam\steamapps\common\Gamble With Your Friends"

# Build example pack
dotnet build -c Release ExampleCosmetics/GWYF-ExampleCosmetics.csproj -p:GameDir="..."
```

## License

MIT
