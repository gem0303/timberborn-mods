# Idle Worker Alert

Timberborn 1.1 quality-of-life mod for Builder's Huts, District Centers, and Hauling Posts.

## Behavior

- Detects workers continuously using Timberborn's built-in `WaitInsideIdlyWorkplaceBehavior`.
- Applies a configurable grace period independently to each assigned worker (1 in-game hour by default).
- Lets the player change the grace period from 0 to 12 hours through Mod Settings, including decimal values.
- Shows `Idle workers: X of Y` in the selected workplace panel only while one or more workers are idle.
- Adds a normal bottom-left `Idle workers` alert for affected buildings.
- Uses Timberborn's alert selection behavior, including cycling through multiple affected buildings.
- Does not pause buildings, change staffing, or show a floating world-space status icon.

The idle grace period restarts after loading a save. No new data is written to save files.
The configured grace period is global across saves and takes effect immediately.

## Dependency

Requires [Mod Settings](https://steamcommunity.com/sharedfiles/filedetails/?id=3283831040) version 1.1.1.0 or newer.

## Build

Run `build.ps1`. It compiles against the assemblies in the installed Timberborn directory and the Mod Settings installation, then writes the DLL into `mod/IdleWorkerAlert/Scripts`.

```powershell
& .\build.ps1
```

The script uses the default Steam locations. Pass `-GameDirectory` or `-ModSettingsDirectory` when either dependency is installed elsewhere.

## Validate

Run `test.ps1` after building and pass `-MonoCecilPath` pointing to a local Mono.Cecil 0.11.6 assembly. Mono.Cecil is a test-only dependency and is not bundled in this repository.

## TODO / issue tracker

- [ ] **Workshop release blocker — expand workplace coverage.** The mod currently reports idle workers only for Builder's Huts, District Centers, and Hauling Posts. This narrow scope was intentional because another mod in the author's normal load order automatically shuts down many buildings when they have no available jobs, but it does not cover every building. Before publishing to the Workshop, inventory the remaining vanilla workplaces that can leave assigned workers waiting idly, decide which ones should generate alerts, and add them. After vanilla coverage is reliable, investigate capability-based detection or another extensible mechanism for workplaces added by other mods.
- [ ] **Non-blocking — narrow component attachment.** `IdleWorkerMonitor` is currently attached to every `Workplace` template and disables itself at runtime when the workplace is unsupported. This is straightforward and should have little impact, but it creates unnecessary components. Investigate whether the template decorators can target only supported workplace types without introducing duplicate components or making future custom-building support harder. This does not block a Workshop release.
