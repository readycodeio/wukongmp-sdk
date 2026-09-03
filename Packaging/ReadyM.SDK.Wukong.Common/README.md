# ReadyM.SDK.Wukong.Common

Shared types for a [WukongMP](https://readym.io) mod: the ECS components and RPC contracts
that the two halves of a mod have to agree on, plus the ReadyM source generator that turns
them into registrations.

Reference this from your mod's shared project. The client and server packages both depend on
it, so you do not need to add it again alongside them.

## Which package do I need

A mod is three projects, and there is one package per project:

| your project | package | framework |
|---|---|---|
| shared code | `ReadyM.SDK.Wukong.Common` | `netstandard2.0`, `net10.0` |
| the mod that runs in the game | `ReadyM.SDK.Wukong.Client` | `netstandard2.0` |
| the mod that runs on the relay server | `ReadyM.SDK.Wukong.Server` | `net10.0` |

Client and Server both depend on Common, and neither depends on the other. That is
deliberate: it means shared code cannot reach into client-only API and then fail when the
server loads it, and neither half can reference the other's SDK by accident.

```bash
dotnet add package ReadyM.SDK.Wukong.Common
```

Add `--prerelease` while the version you want is a preview.

## What is in it

The ReadyM core API and multiplayer layer, the Wukong component types, the Yooni native
container and serialization assemblies, and our fork of
[Friflo.Engine.ECS](https://github.com/friflo/Friflo.Engine.ECS). The source generator ships as
an analyzer, so it runs in every project that references this package.

[LiteNetLib](https://github.com/RevenantX/LiteNetLib) comes in as a package dependency rather
than a bundled assembly.

Multi-targeted on purpose, so a `net10.0` server mod gets the `net10.0` builds rather than
falling back to `netstandard2.0`.

## See also

- [readym-core-sdk](https://github.com/readycodeio/readym-core-sdk), the game-agnostic core
  this builds on
- [wukong-game-refs](https://github.com/readycodeio/wukong-game-refs), reference-only
  assemblies for the game itself
