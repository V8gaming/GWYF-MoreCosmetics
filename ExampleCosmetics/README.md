# Example Cosmetics

Example cosmetic pack for [More Cosmetics](https://thunderstore.io/c/gamble-with-your-friends/p/MrMeeseeks/MoreCosmetics/). Demonstrates all three model types: custom OBJ with texture, vanilla mesh cloning, and tint-only cosmetics.

## Included Cosmetics

| Name | Type | Rarity | Model |
|---|---|---|---|
| Custom Suit | Clothing | Legendary | OBJ + texture |
| Blue Tinted Shirt | Clothing | Common | Vanilla `Shirt 1` clone + blue tint |
| Red Beanie | Hat | Common | Vanilla `Beanie 1` clone + red tint |
| Gold Bowtie | Neckwear | Common | Vanilla `Bowtie 2` clone + gold tint |

## How It Works

This pack is just a folder with a `cosmetics.json` and assets. The More Cosmetics library auto-discovers it — no special code needed.

## Creating Your Own Pack

1. Create a folder under `BepInEx/plugins/`
2. Add a `cosmetics.json` following the [More Cosmetics format](https://thunderstore.io/c/gamble-with-your-friends/p/MrMeeseeks/MoreCosmetics/)
3. Drop `.obj` files in `models/` and `.png` textures in `textures/`
4. Restart the game — cosmetics appear in the wardrobe

`cosmetics.json` is all you need, but the ExampleCosmetics.dll, is needed to enforces the bepinex Dependencies and is needed for the Thunderstore upload.

## Dependencies

- [BepInEx Pack](https://thunderstore.io/c/gamble-with-your-friends/p/BepInEx/BepInExPack/)
- [More Cosmetics](https://thunderstore.io/c/gamble-with-your-friends/p/MrMeeseeks/MoreCosmetics/)
