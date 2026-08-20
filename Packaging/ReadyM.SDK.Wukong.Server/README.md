# ReadyM.SDK.Wukong.Server

The server half of a [WukongMP](https://readym.io) mod, hosted by the ReadyM relay server.

Reference this from the mod project the relay server loads. It depends on
`ReadyM.SDK.Wukong.Common`, so the shared types come with it.

## Which package do I need

A mod is three projects, and there is one package per project:

| your project | package | framework |
|---|---|---|
| shared code | `ReadyM.SDK.Wukong.Common` | `netstandard2.0`, `net10.0` |
| the mod that runs in the game | `ReadyM.SDK.Wukong.Client` | `netstandard2.0` |
| the mod that runs on the relay server | `ReadyM.SDK.Wukong.Server` | `net10.0` |

Client and Server both depend on Common, and neither depends on the other. The client SDK and
the mod loader assemblies are deliberately absent here, so a server mod cannot reference
client-only API by accident and then fail when the server loads it.

```bash
dotnet add package ReadyM.SDK.Wukong.Server
```

Add `--prerelease` while the version you want is a preview.

## What is in it

`WukongMp.Sdk.Serverside` and the relay server SDK: the entry point you derive from, system
registration, and the RPC handler side of the contracts you declared in your shared project.

Server mods have no folder of their own on the server. Every file sits next to the SDK's own
server mods, so ship only what is yours.

## See also

- [readym-core-sdk](https://github.com/readycodeio/readym-core-sdk), the game-agnostic core
  this builds on
