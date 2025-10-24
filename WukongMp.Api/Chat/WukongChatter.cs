using System;
using System.Collections.Generic;
using System.Linq;
using b1;
using B1UI;
using BtlShare;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Client;
using ReadyM.Relay.Client.State;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.DTO;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.Resources;
using WukongMp.Api.State;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Chat;

public class WukongChatter : IDisposable
{
    private readonly WukongConnectionManager _connection;
    private readonly ClientState _state;
    private readonly WukongAreaState _areaState;
    private readonly WukongPlayerState _playerState;
    private readonly WukongRpcCallbacks _rpc;
    private readonly IClientEcsUpdateLoop _ecsLoop;

    private string NickName => _playerState.LocalPlayerEntity?.GetState().NickName ?? "";
    private const char Separator = ' ';
    private readonly Dictionary<string, WukongChatterCommand> _commands = new();

    public WukongChatter(
        WukongConnectionManager connection,
        ClientState state,
        WukongAreaState areaState,
        WukongPlayerState playerState,
        WukongRpcCallbacks rpc,
        IClientEcsUpdateLoop ecsLoop
    )
    {
        Logging.LogDebug("Initializing WukongChatter");

        _connection = connection;
        _state = state;
        _areaState = areaState;
        _playerState = playerState;
        _rpc = rpc;
        _ecsLoop = ecsLoop;

        _state.OnJoinedArea += OnJoinedAreaHandler;
        _state.OnOtherPlayerOutsideArea += OnOtherPlayerOutsideAreaHandler;

        SetupCommands();
    }

    public void Dispose()
    {
        Logging.LogDebug("Disposing WukongChatter");

        _state.OnJoinedArea -= OnJoinedAreaHandler;
        _state.OnOtherPlayerOutsideArea -= OnOtherPlayerOutsideAreaHandler;
    }

    public void ProcessMessage(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            message = message.Trim();
            if (!TryHandleCommand(message))
            {
                SendChatMessage(NickName, message);
            }
        }
    }

    private void SetupCommands()
    {
        _commands.Add("/reconnect", new WukongChatterCommand(RequestReconnect));
        _commands.Add("/giveup", new WukongChatterCommand(RequestGiveUp));
        _commands.Add("/rebirth", new WukongChatterCommand(RequestRebirth));
        _commands.Add("/rebirth_shrine", new WukongChatterCommand(RequestPointRebirth));
        if (Constants.IsPvP)
        {
            _commands.Add("/spawn", new WukongChatterCommand(RequestSpawn)); // TODO: Enable in PvP
        }
#if DEBUG
        _commands.Add("/play", new WukongChatterCommand(PlayCutscene));
        _commands.Add("/disconnect", new WukongChatterCommand(RequestDisconnect));
        _commands.Add("/spectator", new WukongChatterCommand(SetSpectatorStatus));
        _commands.Add("/teleport", new WukongChatterCommand(Teleport));
        _commands.Add("/openlevel", new WukongChatterCommand(OpenLevel));
#endif
    }

    private void RequestSpawn(ReadOnlyMemory<string> args)
    {
        var unitName = args.Span[0];
        if (!UnitPathsConfig.IsValidUnitName(unitName))
        {
            ChatWidget.Instance.AddMessage(true, "Command", $"{Texts.InvalidUnitName}: \"{args.Span[0]}\"");
            return;
        }

        var playerEntity = _playerState.LocalPlayerEntity;
        if (playerEntity == null)
            return;

        var characterEntity = _playerState.LocalMainCharacter;
        if (characterEntity == null)
            return;

        var teamId = PvPUtils.GetOppositeTeam(playerEntity.Value.GetState().TeamId);
        var playerPawn = characterEntity.Value.GetLocalState().Pawn;
        if (playerPawn == null)
            return;

        var location = SpawningUtils.CalculateSpawnLocation(playerPawn.GetActorLocation(), playerPawn.GetActorForwardVector());
        var count = 0;
        var shouldSpawn = false;

        switch (args.Length)
        {
            case 1:
                count = 1;
                shouldSpawn = true;
                break;
            case 2:
            {
                if (int.TryParse(args.Span[1], out count))
                {
                    shouldSpawn = true;
                }
                else
                {
                    ChatWidget.Instance.AddMessage(true, "Command", $"{Texts.InvalidUnitName}: \"{args.Span[1]}\"");
                }

                break;
            }
        }

        if (shouldSpawn)
        {
            _rpc.SendRequestSpawnUnits(new UnitSpawnRequestData(unitName, count, teamId, location));
            SendServerMessage("PlayerSpawned", characterEntity.Value.GetState().CharacterNickName, count.ToString(), args.Span[0]);
        }
    }

    private void RequestRebirth(ReadOnlyMemory<string> _)
    {
        var playerId = _connection.PlayerId;
        if (playerId == null)
            return;

        _rpc.SendRebirthPlayer(playerId.Value);
        SendServerMessage("PlayerRequestedRebirth", NickName);
    }

    private void RequestPointRebirth(ReadOnlyMemory<string> _)
    {
        if (_playerState.LocalMainCharacter is not { } mainEntity)
            return;

        var playerId = mainEntity.GetState().PlayerId;
        PlayerUtils.TeleportLocalPlayerToRebirthPoint(mainEntity);
        _rpc.SendRebirthPlayer(playerId);
        SendServerMessage("PlayerRequestedRebirth", NickName);
    }

    private void PlayCutscene(ReadOnlyMemory<string> args)
    {
        if (args.Length == 1 && int.TryParse(args.Span[0], out var seqId))
        {
            GSG.GMSvc.GMTeleportToTargetSequence(seqId);
        }
    }

    private void RequestGiveUp(ReadOnlyMemory<string> _)
    {
        SendServerMessage("PlayerGaveUp", NickName);

        // no need to send an RPC event since in co-op all players are authoritative over their HP
        _ecsLoop.Scheduler.Schedule(static (_, self) =>
        {
            if (self._playerState.LocalMainCharacter is not { } mainEntity)
                return;

            ref var localMainComp = ref mainEntity.GetLocalState();
            var events = BUS_EventCollectionCS.Get(localMainComp.Pawn);
            events?.Evt_IncreaseAttrFloat.Invoke(EBGUAttrFloat.Hp, -2000f);
            events?.Evt_UnitDead.Invoke(localMainComp.Pawn, EDeadReason.Suicide);
        }, this);
    }

    private void RequestReconnect(ReadOnlyMemory<string> _)
    {
        _connection.Reconnect();
    }

    private void RequestDisconnect(ReadOnlyMemory<string> _)
    {
        if (_connection.AreaState.InRoom)
        {
            SendServerMessage("PlayerLeft", NickName);
            _connection.Disconnect();
        }
    }

    private void SetSpectatorStatus(ReadOnlyMemory<string> args)
    {
        if (args.Length == 1)
        {
            var isSpectator = args.Span[1].Equals("true", StringComparison.OrdinalIgnoreCase);

            var playerEntity = _playerState.LocalMainCharacter;
            if (playerEntity == null)
                return;
            playerEntity.Value.GetPvP().IsSpectator = isSpectator;
        }
    }

    private void Teleport(ReadOnlyMemory<string> args)
    {
        if (args.Length == 1 && int.TryParse(args.Span[0], out var birthpointId))
        {
            BPS_EventCollectionCS.Get(GameUtils.GetControlledPawn()?.PlayerState).Evt_BPS_TeleportTo.Invoke(ETeleportTypeV2.RebirthPointTeleportOnly, new TeleportParam_RebirthPoint
            {
                RebirthPointId = birthpointId
            }, EPlayerTeleportReason.RebirthPoint);
        }
    }

    private void OpenLevel(ReadOnlyMemory<string> args)
    {
        if (args.Length == 1)
        { 
            UGameplayStatics.OpenLevel(GameUtils.GetWorld(), new FName(args.Span[0]));
        }
    }


    private bool TryHandleCommand(string message)
    {
        var commandParts = message.Split(Separator);
        if (commandParts.Length > 0)
        {
            if (_commands.ContainsKey(commandParts[0]))
            {
                var cmd = _commands[commandParts[0]];
                var rest = commandParts.Skip(1).ToArray();
                cmd.Handler(rest);
                return true;
            }
        }

        return false;
    }

    private void SendChatMessage(string nickname, string message)
    {
        Logging.LogDebug("Sending message {Message}", message);
        _rpc.SendChatMessage(ChatMessage.CreateClientMessage(nickname, message));
    }

    public void SendServerMessage(string message, params string[] args)
    {
        Logging.LogDebug("Sending server message {Message}", message);
        _rpc.SendChatMessage(ChatMessage.CreateServerMessage(message, args));
    }

    public static void OnGetMessage(ChatMessage message)
    {
        var senderNickname = message.IsServer ? "Server" : message.Nickname!;
        var translatedMessage = message.Message;
        if (message.IsServer)
        {
            translatedMessage = string.Format(Texts.ResourceManager.GetString(message.Message, Texts.Culture)!, [.. message.Placeholders]);
        }

        Logging.LogDebug("Message \"{Message}\" received from \"{Sender}\"", message, senderNickname);
        ChatWidget.Instance.AddMessage(message.IsServer, senderNickname, translatedMessage);
    }

    private static void SendLocalMessage(string message, string[] placeholders)
    {
        var translatedMessage = string.Format(Texts.ResourceManager.GetString(message, Texts.Culture)!, [.. placeholders]);
        ChatWidget.Instance.AddMessage(true, "Server", translatedMessage);
    }

    private void OnJoinedAreaHandler(AreaId areaId, Entity entity)
    {
        var playerEntity = _playerState.LocalPlayerEntity;
        if (playerEntity == null)
            return;
        ref var player = ref playerEntity.Value.GetState();
        Logging.LogDebug("Player {PlayerName} joined the room", player.NickName);
        SendServerMessage("PlayerJoined", player.NickName);
    }

    private void OnOtherPlayerOutsideAreaHandler(PlayerId arg1, AreaId arg2, ReadyM.Api.Multiplayer.Common.OtherPlayerOutsideAreaReason arg3)
    {
        var playerEntity = _playerState.GetPlayerById(arg1);
        if (playerEntity == null)
            return;
        ref var player = ref playerEntity.Value.GetState();
        var nickname = player.NickName;
        SendLocalMessage("PlayerLeft", [nickname]);
    }
}