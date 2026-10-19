# Natural Removal Button

Timberborn 1.1 mod that exposes the game's built-in **Mark for demolition** button when any natural resource is selected.

The button uses Timberborn's normal demolition workflow. It creates a job for a beaver, supports demolition priority and progress, and can be cancelled from the same entity panel. It does not instantly delete the selected object.

The mod uses two layers. Bundled blueprint overrides guarantee the button for the 22 vanilla plants. A native Timberborn template decorator is intended to discover other natural resources at runtime, including plants supplied by other mods when they use Timberborn's normal `NaturalResourceSpec` and `DemolishableSpec` components. A custom object without the game's demolition job component is left unchanged.

The mod also logs each runtime initialization stage to `Player.log` with a Natural Removal Button prefix. These messages make it possible to identify a failed mod loader, configurator, template provider, or entity-component stage without adding Harmony or replacing Timberborn's UI.

## Build

Run `Build.ps1` before installing from a source checkout. It compiles against the assemblies in the installed Timberborn directory and writes the DLL into `mod/NaturalRemovalButton/Scripts`.

```powershell
& .\Build.ps1
```

The script expects Timberborn in the default Steam installation directory. Pass `-GameDirectory` when the game is installed elsewhere.

## Install

After building, copy `mod/NaturalRemovalButton` into the Timberborn user mods directory, then enable **Natural Removal Button** in the game's Mods screen and restart when prompted.

## Compatibility

Built for Timberborn 1.1 and checked against the current stable 1.1 build.

## TODO / issue tracker

- [ ] **Workshop release blocker — verify and repair custom-plant support.** The 22 bundled vanilla blueprint overrides work independently, but recent `Player.log` files show the mod starter running without any subsequent configurator, template-provider, or entity-component diagnostics. That indicates the runtime decorator intended to support plants added by other mods is not currently being installed. The best next step is to install a Timberborn 1.1 mod that adds a custom plant, reproduce the behavior with a minimal mod list, check whether the demolition button appears on that plant, and compare the result with the Natural Removal Button log stages. The likely first fix to test is registering `NaturalRemovalButtonConfigurator` for the game context, then rebuilding and repeating the custom-plant test. Before Workshop publication, either make this runtime path work reliably or change the manifest and documentation to promise vanilla plants only.
