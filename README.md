# Timberborn Mods

Source and local release workspaces for custom mods targeting Timberborn 1.1.

## Mods

| Mod | Description |
| --- | --- |
| [Idle Worker Alert](IdleWorkerAlert/) | Shows sustained idle-worker counts for Builder's Huts, District Centers, and Hauling Posts, with a native HUD alert for affected workplaces. |
| [Natural Removal Button](NaturalRemovalButton/) | Exposes Timberborn's normal **Mark for demolition** action for natural resources, including supported plants added by other mods. |

Each project contains its source code, build script, mod staging directory, and more detailed documentation.

## Compatibility

These mods target Timberborn 1.1. The exact minimum supported game version is defined in each mod's manifest, and each release should be rebuilt and tested against the current stable game build.

Timberborn updates can change internal assemblies and APIs. Rebuild and retest the mods before using them with a newer major game version.

## Building

Both projects use PowerShell build scripts that compile against the assemblies in an installed Windows copy of Timberborn. By default, they expect the game at:

```text
C:\Program Files (x86)\Steam\steamapps\common\Timberborn
```

From the repository root, build a project with:

```powershell
& .\IdleWorkerAlert\build.ps1
& .\NaturalRemovalButton\Build.ps1
```

Pass `-GameDirectory` when Timberborn is installed elsewhere.

Idle Worker Alert also compiles against the Mod Settings mod. Its build script uses the default Steam Workshop location, or accepts a different path through `-ModSettingsDirectory`.

## Installing local builds

After building, copy the desired directory from the project's `mod` folder into:

```text
Documents\Timberborn\Mods
```

The resulting paths should look like:

```text
Documents\Timberborn\Mods\IdleWorkerAlert\manifest.json
Documents\Timberborn\Mods\NaturalRemovalButton\manifest.json
```

Start or restart Timberborn, enable the mod in the Mod Manager, and restart again if prompted.

## Releases and Workshop

GitHub release downloads and Steam Workshop links will be added after the first public releases are prepared and validated. After a Workshop item is created, preserve its generated `workshop_data.json` file so later uploads update the existing item instead of creating a duplicate.

## License

The source code and original project files in this repository are available under the [MIT License](LICENSE).

Timberborn is developed by Mechanistry. This repository is an unofficial fan project and is not affiliated with or endorsed by Mechanistry.
