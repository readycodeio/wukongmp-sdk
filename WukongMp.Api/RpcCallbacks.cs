using b1;
using b1.BGW;
using LiteNetLib;
using ReadyM.Api.Multiplayer;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Protocol;
using ReadyM.Relay.Common.Protocol.Enums;
using UnrealEngine.Engine;
using WukongMp.Api.Old;
using WukongMp.Api.Old.Api;
using WukongMp.Api.Old.DTO;

namespace WukongMp.Api;

public partial class WukongMpMod
{
    // TODO: Generate this
    protected override void OnCustomEvent(CustomEventHeader header, NetPacketReader reader)
    {
        switch (header.EventCode)
        {
            case 0:
            {
                var payload = RelayClient.DeserializeObject<MontageCallbackData>(reader);
                OnMontageCallback(payload);
                break;
            }
            case 1:
            {
                var payload = RelayClient.DeserializeObject<UnitDeadPacket>(reader);
                OnUnitDead(payload);
                break;
            }
        }
    }

    [RpcEvent(RelayMode.Others)]
    public void OnMontageCallback(MontageCallbackData data)
    {
        var id = data.NetId;
        var pawn = GetPawnByNetworkId(id);
        if (pawn == null)
        {
            LogNullCharacter(id);
            return;
        }

        if (string.IsNullOrEmpty(data.MontagePath))
        {
            Logging.LogDebug("Stopping montage playback for character {CharacterId}", id);
            pawn.StopAnimMontage(null);
            return;
        }

        var fullMontagePath = data.Compressed ? MontageHelpers.DecompressMontageName(data.MontagePath) : data.MontagePath;
        Logging.LogDebug("Received montage: {Montage}, position: {Position}, reset: {Reset}", fullMontagePath, data.Position, data.Reset);

        var animInstance = pawn.Mesh.GetAnimInstance();
        if (animInstance == null)
        {
            Logging.LogError("AnimInstance is null");
            return;
        }

        var currentMontage = animInstance.GetCurrentActiveMontage();
        Logging.LogDebug("Current montage: {Montage}", currentMontage?.PathName);

        // if the same montage is currently playing an no reset flag is given, do not play new montage
        if (currentMontage != null && currentMontage.PathName == fullMontagePath && !data.Reset)
        {
            Logging.LogDebug("Skipping montage playback: {Montage}, is reset: {Reset}", fullMontagePath, data.Reset);
            return;
        }

        var montage = BGW_PreloadAssetMgr.Get(GameUtils.GetWorld()).TryGetCachedResourceObj<UAnimMontage>(fullMontagePath, ELoadResourceType.SyncLoadAndCache);

        if (montage == null)
        {
            Logging.LogWarning("Montage not found: {Montage}", fullMontagePath);
            return;
        }

        var events = BUS_EventCollectionCS.Get(pawn);

        if (events == null)
        {
            Logging.LogError("events are null");
            return;
        }

        Logging.LogDebug("Applying montage callback for character {CharacterId} with montage {Montage} @ {Position}", id, fullMontagePath, data.Position);
        animInstance.Montage_Play(montage, 1f, EMontagePlayReturnType.MontageLength, data.Position);
        events.Evt_PlayMontageCallback.Invoke(EMontageBindReason.Default, montage, EMontageCallbackState.OnStarted);
    }

    [RpcEvent(RelayMode.Others)]
    public void OnUnitDead(UnitDeadPacket data)
    {
        var pawn = GetPawnByNetworkId(data.NetworkId);
        if (pawn == null)
        {
            LogNullCharacter(data.NetworkId);
            return;
        }

        var events = BUS_EventCollectionCS.Get(pawn);
        if (events == null)
        {
            Logging.LogError("Failed to get event collection for unit {Unit}", pawn.GetName());
            return;
        }

        events.Evt_UnitDead.Invoke(null, data.DeadReason, data.DmgId, data.StiffLevel, null, default, data.IsDotDmg, data.AbnormalType);
    }

    private void LogNullCharacter(NetworkIdComponent characterId)
    {
        if (characterId.Id != uint.MaxValue)
            Logging.LogWarning("Monster not found: {Id}", characterId); // monster not found
        else
            Logging.LogError("Player not found: {Id}", characterId); // player not found
    }
}