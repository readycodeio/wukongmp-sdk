using b1;
using Friflo.Engine.ECS;
using HarmonyLib;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.ECS.Jobs;
using WukongMp.Api.Monitors;

namespace WukongMp.Api.Patches;

[HarmonyPatch(typeof(BGWGameInstanceCS), "ReceiveTick_Implementation")]
[HarmonyPatchCategory(Constants.GlobalPatches)]
public static class ReceiveTickPatch
{
    public static void Prefix(ref int TickGroup)
    {
        var mask = CustomTickGroupToTickGroupMask(TickGroup);

        if (mask == BGW_TickGroupMask.TG_None)
        {
            TickGroup = 3;
        }
    }

    public static void Postfix(float DeltaSeconds, int TickGroup)
    {
        var mask = CustomTickGroupToTickGroupMask(TickGroup);

        if (mask == BGW_TickGroupMask.TG_OnTick)
        {
            RunMontageSync();
            DI.Instance.EcsLoop.Tick(DeltaSeconds);
#if DEBUG
            DI.Instance.TestsRunner.Update(DeltaSeconds);
            ComponentMonitorManager.Instance.Update();
#endif
        }
    }

    private static void RunMontageSync()
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        var mainEntity = DI.Instance.PlayerState.LocalMainCharacter;
        if (mainEntity == null)
            return;

        SyncPlayerMontage(mainEntity.Value);

        var playerId = DI.Instance.State.LocalPlayerId;
        if (playerId == null)
            return;

        DI.Instance.World.Query<LocalTamerComponent, MetadataComponent>().Each(new SyncMontageJob(DI.Instance.Rpc, playerId.Value));
    }

    private static void SyncPlayerMontage(MainCharacterEntity mainEntity)
    {
        ref var localMainComp = ref mainEntity.GetLocalState();

        if (localMainComp.Pawn == null || localMainComp.Pawn.Mesh == null)
            return;

        var montageState = localMainComp.MontageState;
        if (montageState.LocalAnimationInstance == null)
        {
            montageState.LocalAnimationInstance = localMainComp.Pawn.Mesh.GetAnimInstance();
            if (montageState.LocalAnimationInstance == null)
                return;
        }

        var currentMontage = localMainComp.Pawn.GetCurrentMontage();

        if (currentMontage != null)
        {
            bool isNewMontage = montageState.LocalMontage != currentMontage;
            float currentPosition = montageState.LocalAnimationInstance.Montage_GetPosition(currentMontage);

            bool hasMontageRewound = currentPosition < montageState.LocalMontagePosition && !isNewMontage;
            bool hasSkippedFrames = currentPosition - montageState.LocalMontagePosition > 0.5f && !isNewMontage;

            if (isNewMontage || hasMontageRewound || hasSkippedFrames)
            {
                var netId = mainEntity.GetMeta().NetId;
                DI.Instance.Rpc.SendMontageCallback(netId, currentMontage, currentPosition, hasMontageRewound);
            }

            montageState.LocalMontagePosition = currentPosition;
        }
        else if (montageState.LocalMontage != null)
        {
            var netId = mainEntity.GetMeta().NetId;
            DI.Instance.Rpc.SendMontageCancel(netId);
        }

        montageState.LocalMontage = currentMontage;
        localMainComp.MontageState = montageState;
    }

    private static BGW_TickGroupMask CustomTickGroupToTickGroupMask(int tickGroup)
    {
        switch (tickGroup)
        {
            case 0:
                return BGW_TickGroupMask.TG_OnTick;
            case 1:
                return BGW_TickGroupMask.TG_None;
            case 2:
                return BGW_TickGroupMask.TG_AfterAnim;
            case 3:
                return BGW_TickGroupMask.TG_None;
            case 4:
                return BGW_TickGroupMask.TG_PostPhysics;
            case 5:
                return BGW_TickGroupMask.TG_PostUpdateWork;
            case 101:
                return BGW_TickGroupMask.TG_PreAnim;
            case 111:
                return BGW_TickGroupMask.TG_BeforeStartPhsic;
            case 141:
                return BGW_TickGroupMask.TG_BeforePostPhsic;
            case 151:
                return BGW_TickGroupMask.TG_BeforePostUpdateWork;
            default:
                Logging.LogError("CustomTickGroup_To_BGWTickGroupMask : unknown tickgroup");
                return BGW_TickGroupMask.TG_None;
        }
    }
}