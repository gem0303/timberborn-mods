# Guideline Numbers

Timberborn 1.1 quality-of-life mod that adds distance numbers to the vanilla construction-guidelines cursor grid.

## Behavior

- Labels the tile under the cursor `1`, then counts outward independently along all four arms.
- Shows a number on every visible guideline tile, with no negative coordinates.
- Follows the vanilla controls: hold `X` for temporary guidelines or press `Shift+X` to toggle them continuously.
- Keeps the numbers flat on the terrain and rotates them in quarter turns with the camera.
- Appears only on the cursor grid, not on construction guidelines shown while placing buildings.
- Generates the digit meshes at runtime and reuses pooled renderers; it does not bundle fonts, textures, or asset bundles.

The mod has no settings and does not write data to save files.

## Dependency

Requires [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=3284904751) version 2.4.1 or newer.

## Build

Run `build.ps1`. It compiles against the assemblies in the installed Timberborn directory and the Harmony installation, then writes the DLL into `mod/GuidelineNumbers/Scripts`.

```powershell
& .\build.ps1
```

The script uses the default Steam locations. Pass `-GameDirectory` or `-HarmonyDirectory` when either dependency is installed elsewhere.

## Validate

Run `test.ps1` after building and pass `-MonoCecilPath` pointing to a local Mono.Cecil 0.11.6 assembly. Mono.Cecil is a test-only dependency and is not bundled in this repository.

## Install

After building, copy `mod/GuidelineNumbers` into the Timberborn user mods directory, then enable **Guideline Numbers** in the game's Mods screen and restart when prompted.

## Compatibility

Built for Timberborn 1.1.2.7. The mod patches private construction-guideline methods and should be rebuilt and retested after game updates.
