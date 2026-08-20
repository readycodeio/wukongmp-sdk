# ReadyM.SDK.Wukong.Client

The in-game half of a [WukongMP](https://readym.io) mod: the client SDK, the Wukong API
surface, the relay client, and the mod loader assemblies your mod patches through and derives
from.

Reference this from the mod project that the game loads. It depends on
`ReadyM.SDK.Wukong.Common`, so the shared types come with it.

## Which package do I need

A mod is three projects, and there is one package per project:

| your project | package | framework |
|---|---|---|
| shared code | `ReadyM.SDK.Wukong.Common` | `netstandard2.0`, `net10.0` |
| the mod that runs in the game | `ReadyM.SDK.Wukong.Client` | `netstandard2.0` |
| the mod that runs on the relay server | `ReadyM.SDK.Wukong.Server` | `net10.0` |

Client and Server both depend on Common, and neither depends on the other. The server SDK is
deliberately absent here, so a client mod cannot reference server-only API by accident.

```bash
dotnet add package ReadyM.SDK.Wukong.Client
```

Add `--prerelease` while the version you want is a preview.

`netstandard2.0` is not a choice: the game's runtime is what loads your mod, so the client
half targets what that runtime accepts.

## What is in it

`WukongMp.Sdk` and `WukongMp.Api`, the relay client, and the
[mod loader](https://github.com/readycodeio/wukong-modloader) assemblies, Harmony included,
since mods patch through them and derive from their base types.

The game's own assemblies arrive through
[`ReadyM.Wukong.GameRefs`](https://github.com/readycodeio/wukong-game-refs), which comes in as
a dependency. Those are reference-only: they exist so your mod compiles, and the real
assemblies are already loaded in the game process at runtime.

## See also

- [readym-core-sdk](https://github.com/readycodeio/readym-core-sdk), the game-agnostic core
  this builds on
