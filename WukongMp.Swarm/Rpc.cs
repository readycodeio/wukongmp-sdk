using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Generators;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Api.Multiplayer.RPC;
using ReadyM.Api.Multiplayer.Serialization;
using UnrealEngine.Runtime;
using WukongMp.Api;
using WukongMp.Sdk.Api;

namespace WukongMp.Swarm;

public partial class Rpc(IRpcClient client, IRelaySerializer serializer) : RpcClassBase(client, serializer)
{
    [RpcEvent(RelayMode.AreaOfInterestAll)]
    private void OnSwarmStarted(PlayerId __sender)
    {
        if (!WukongApi.Sync.TryGetPlayerInfoById(__sender, out var playerName, out _))
        {
            Logging.LogError("Failed to get player info for sender {Sender}", __sender);
        }

        WukongApi.Local.ShowInfoMessage("Get ready!", 3);
        WukongApi.Chat.ShowLocalMessage($"Swarm mode enabled by {playerName}!", FLinearColor.NavajoWhite);
        WukongApi.Chat.ShowLocalMessage("Enemies will spawn around you every 10 seconds, with increasing numbers. Survive as long as you can!", FLinearColor.NavajoWhite);
    }

    [RpcEvent(RelayMode.AreaOfInterestAll)]
    private void OnSwarmEnded(int enemiesSpawned)
    {
        WukongApi.Chat.ShowLocalMessage($"Swarm mode ended, survived {enemiesSpawned} enemies", FLinearColor.OrangeRed);
    }
    
    [RpcEvent(RelayMode.AreaOfInterestAll)]
    private void OnRemainingPlayers(int remaining)
    {
        WukongApi.Chat.ShowLocalMessage($"Remaining players: {remaining}", FLinearColor.Yellow);
    }
}