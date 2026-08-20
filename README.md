# WukongMP SDK

The SDK for writing [WukongMP](https://readym.io) mods: multiplayer for Black Myth: Wukong.
This repository is the source of the three NuGet packages a mod references, and of the
`WukongMp.Api` layer that talks to the game itself.

Writing a mod? You want the packages, not this repository. Building or changing the SDK? Read on.

## Packages

A mod is three projects, and there is one package per project, so a project only sees the
assemblies that exist in the process it runs in:

| package | tfm | for |
|---|---|---|
| `ReadyM.SDK.Wukong.Common` | `netstandard2.0`, `net10.0` | shared components, RPC contracts, the source generator |
| `ReadyM.SDK.Wukong.Client` | `netstandard2.0` | the mod the game loads, plus mod loader assemblies |
| `ReadyM.SDK.Wukong.Server` | `net10.0` | the mod the relay server loads |

Client and Server both depend on Common and neither depends on the other. Shared code
therefore cannot reference client-only API and then fail when the server loads it.

## Layout

| project | tfm | |
|---|---|---|
| `WukongMp.Api` | `netstandard2.0` | the game-facing layer: Harmony patches, game state, chat, UI |
| `WukongMp.Sdk` | `netstandard2.0` | the client-side surface a mod derives from |
| `WukongMp.Sdk.Serverside` | `net10.0` | the server-side surface a mod derives from |
| `ReadyM.Wukong.Common` | `netstandard2.0`, `net10.0` | Wukong component types both sides agree on |
| `Packaging/` | | packaging-only projects, one per NuGet package |

`netstandard2.0` is not a stylistic choice: anything the game process loads has to target what
that runtime accepts.

## Build

```bash
git clone --recursive https://github.com/readycodeio/wukongmp-sdk.git
dotnet build WukongMP.SDK.sln
```

`--recursive` matters: [readym-core-sdk](https://github.com/readycodeio/readym-core-sdk) is a
submodule and gets built from source.

The game's own assemblies come from the
[`ReadyM.Wukong.GameRefs`](https://github.com/readycodeio/wukong-game-refs) package. They are
reference-only, so the SDK compiles against the game without redistributing any of it.

## Packing

```powershell
./BuildSdkPackage.ps1                              # all three, version from Directory.Build.props
./BuildSdkPackage.ps1 -PackageVersion 0.3.2-rc.1   # override, applied to all three
./BuildSdkPackage.ps1 -CheckDependencies           # non-zero exit if a dependency list drifted
./BuildSdkPackage.ps1 -SyncDependencies            # regenerate them
```

Each package lists the assemblies it owns and packs nothing else, even though the build has
every transitive assembly on hand. A listed name that resolves to nothing fails the build,
rather than quietly dropping out of the package.

Third-party assemblies are declared as dependencies, not bundled. That list is generated from
the projects whose assemblies each package ships, which is what `-SyncDependencies` does, so
do not hand-edit the region between the `GENERATED DEPENDENCIES` markers.

## Building the SDK alongside a mod

A mod referencing the packages cannot be stepped into. To build the SDK from source with the
mods instead, set `UseSdkProjectReferences=true` and point `WukongSdkRoot` and `CoreSdkRoot`
at your checkouts. This repository's `Directory.Build.props` imports whatever sits above it,
so an outer file wins and a standalone clone is unaffected.

Releasing the runtime SDK to the game servers: [docs/releasing.md](docs/releasing.md).
