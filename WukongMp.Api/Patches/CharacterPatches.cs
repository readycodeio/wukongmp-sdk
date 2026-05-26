using System.Reflection;
using b1;
using b1.ECS;
using BtlShare;
using HarmonyLib;
using PreludeLib.Attributes;
using ReadyM.Wukong.Common.ECS.Components;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.ECS.GameEvents;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Patches;

[HarmonyPatch(typeof(BUC_AttrContainer), nameof(BUC_AttrContainer.OnTick))]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchAttrs
{
    public static void Postfix(BUC_AttrContainer __instance)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        if (__instance.Owner.IsNullOrDestroyed())
        {
            Logging.LogError("Owner is null or destroyed");
            return;
        }

        // this patch is an example of an ECS -> Game data sync point
        // if the ECS state is authoritative, we need to sync it to the game here
        // authority is determined either by policy, or setting from API
        if (DI.Instance.MappingPolicyDir.IsMainCharacterMapped(__instance.Owner, out var mainEntity))
        {
            if (DI.Instance.MappedField.CanSyncToGame<HpComponent>(mainEntity.Value.Entity, out var syncHp))
            {
                syncHp.SyncToGame(HpComponent.Fields.Hp.In<BUC_AttrContainer>(), __instance);
            }

            if (DI.Instance.MappedField.CanSyncToGame<MainCharacterComponent>(mainEntity.Value.Entity, out var syncMain))
            {
                syncMain.SyncToGame(MainCharacterComponent.Fields.Attributes.In<BUC_AttrContainer>(), __instance);
            }
        }
        else if (DI.Instance.MappingPolicyDir.IsMonsterTamerMapped(__instance.Owner as BGUCharacterCS, out var tamerEntity))
        {
            if (DI.Instance.MappedField.CanSyncToGame<HpComponent>(tamerEntity.Value.Entity, out var sync))
            {
                var localTamer = tamerEntity.Value.GetLocalTamer();
                if (!localTamer.IsTamerSynced)
                {
                    Logging.LogDebug("Monster {Name} is not synced, skipping HP update", __instance.Owner.GetName());
                    return;
                }

                sync.SyncToGame(HpComponent.Fields.HpMaxBase.In<BUC_AttrContainer>(), __instance);
                sync.SyncToGame(HpComponent.Fields.Hp.In<BUC_AttrContainer>(), __instance);
            }
        }
    }
}

[HarmonyPatch(typeof(BUS_AttrComp), "InitAttrByMaxAttr")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchHpOnReset
{
    public static void Postfix(BUS_AttrComp __instance, BUC_AttrContainer ___AttrContainer)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        var owner = __instance.GetOwner() as BGUCharacterCS;

        if (owner.IsNullOrDestroyed())
        {
            Logging.LogError("Owner is null or destroyed");
            return;
        }

        if (DI.Instance.MappingPolicyDir.IsMainCharacterMapped(owner, out var mainEntity))
        {
            if (DI.Instance.MappedField.CanLoadFromGame<HpComponent>(mainEntity.Value, out var loadHp))
            {
                loadHp.LoadFromGame(HpComponent.Fields.Hp.In<BUC_AttrContainer>(), ___AttrContainer);
            }

            if (DI.Instance.MappedField.CanLoadFromGame<MainCharacterComponent>(mainEntity.Value, out var loadMain))
            {
                foreach (var attrId in new[] { EBGUAttrFloat.B1Stun, EBGUAttrFloat.SkillSuperArmor, EBGUAttrFloat.BlockCollapseArmor, EBGUAttrFloat.BloodBottomNum })
                {
                    loadMain.LoadFromGame(MainCharacterComponent.Fields.Attributes.In<(EBGUAttrFloat, BUC_AttrContainer)>(), (attrId, ___AttrContainer));
                }
            }
        }
        else if (DI.Instance.MappingPolicyDir.IsMonsterTamerMapped(owner, out var tamerEntity))
        {
            if (DI.Instance.MappedField.CanLoadFromGame<HpComponent>(tamerEntity.Value, out var loader))
            {
                loader.LoadFromGame(HpComponent.Fields.Hp.In<BUC_AttrContainer>(), ___AttrContainer);
            }
        }
    }
}

[HarmonyPatch(typeof(BUS_AttrComp), "SetFloatValue")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchHp
{
    public static bool Prefix(BUS_AttrComp __instance, EBGUAttrFloat AttrID, float NewValue, BUC_AttrContainer ___AttrContainer)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        var owner = __instance.GetOwner();
        var localPlayerState = DI.Instance.PlayerState.LocalMainCharacter?.GetLocalState();
        var isLocalPlayer = owner == DI.Instance.PlayerState.LocalMainCharacter?.Pawn;

        if (AttrID == EBGUAttrFloat.Hp)
        {
#if DEBUG
            if (DebugUtils.InvincibilityEnabled && isLocalPlayer)
                return false;
#endif
            var netId = DI.Instance.PawnState.GetNetworkIdByActor(owner);
            if (netId.HasValue)
                return DI.Instance.ClientOwnership.OwnsEntity(netId.Value);
        }

        var cheatsEnabled = DI.Instance.AreaState.CurrentArea.HasValue && DI.Instance.AreaState.CurrentArea.Value.Room.CheatsAllowed;
        if (cheatsEnabled && localPlayerState.HasValue && isLocalPlayer)
        {
            if (AttrID == EBGUAttrFloat.VigorEnergy && localPlayerState.Value.SpiritCooldownEnabled && !localPlayerState.Value.ShouldSetSpiritCooldown)
            {
                var current = ___AttrContainer.GetFloatValue(EBGUAttrFloat.VigorEnergy);
                var max = ___AttrContainer.GetFloatValue(EBGUAttrFloat.VigorEnergyMax);
                if (NewValue.Equals(max, Constants.FloatComparisonTolerance))
                {
                    return true;
                }

                if (NewValue > current)
                {
                    return false;
                }
            }

            if (AttrID == EBGUAttrFloat.FabaoEnergy && localPlayerState.Value.HasInfiniteVessel)
            {
                var current = ___AttrContainer.GetFloatValue(EBGUAttrFloat.FabaoEnergy);
                if (NewValue < current)
                {
                    return false;
                }
            }

            if (AttrID == EBGUAttrFloat.CurEnergy && localPlayerState.Value.HasInfiniteTransform)
            {
                var current = ___AttrContainer.GetFloatValue(EBGUAttrFloat.CurEnergy);
                if (NewValue < current)
                {
                    return false;
                }
            }

            if (AttrID == EBGUAttrFloat.Mp && localPlayerState.Value.HasInfiniteMana)
            {
                var current = ___AttrContainer.GetFloatValue(EBGUAttrFloat.Mp);
                if (NewValue < current)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public static void Postfix(BUS_AttrComp __instance, BUC_AttrContainer ___AttrContainer, EBGUAttrFloat AttrID)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        var owner = __instance.GetOwner() as BGUCharacterCS;

        if (owner.IsNullOrDestroyed())
        {
            Logging.LogError("Owner is null or destroyed");
            return;
        }

        if (AttrID == EBGUAttrFloat.Hp)
        {
            if (DI.Instance.MappingPolicyDir.IsMainCharacterMapped(owner, out var mainEntity))
            {
                if (DI.Instance.MappedField.CanLoadFromGame<HpComponent>(mainEntity.Value, out var loadHp))
                {
                    loadHp.LoadFromGame(HpComponent.Fields.Hp.In<BUC_AttrContainer>(), ___AttrContainer);
                }
            }
            else if (DI.Instance.MappingPolicyDir.IsMonsterTamerMapped(owner, out var tamerEntity))
            {
                if (DI.Instance.MappedField.CanLoadFromGame<HpComponent>(tamerEntity.Value, out var loader))
                {
                    var localTamer = tamerEntity.Value.GetLocalTamer();

                    if (!localTamer.IsTamerSynced)
                        return; // not synced

                    loader.LoadFromGame(HpComponent.Fields.HpMaxBase.In<BUC_AttrContainer>(), ___AttrContainer);
                    loader.LoadFromGame(HpComponent.Fields.Hp.In<BUC_AttrContainer>(), ___AttrContainer);
                }
            }
        }
        else if (DI.Instance.MappingPolicyDir.IsMainCharacterMapped(owner, out var mainEntity))
        {
            if (DI.Instance.MappedField.CanLoadFromGame<MainCharacterComponent>(mainEntity.Value, out var loadMain))
            {
                loadMain.LoadFromGame(MainCharacterComponent.Fields.Attributes.In<(EBGUAttrFloat, BUC_AttrContainer)>(), (AttrID, ___AttrContainer));
            }
        }
    }
}

// NOTE: Runs multithreaded
[HarmonyPatch(typeof(BUC_ABPCharacterData), nameof(BUC_ABPCharacterData.Update_GameThread))]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchCharacterAnimation
{
    public static void Postfix(BUC_ABPCharacterData? __instance, AActor Owner)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        if (__instance == null)
        {
            Logging.LogError("__instance is null in BUC_ABPCharacterData.Update_GameThread");
            return;
        }

        if (Owner is not BGUCharacterCS character)
            return;

        if (Owner.IsNullOrDestroyed())
        {
            Logging.LogError("Owner is null or destroyed");
            return;
        }

        if (DI.Instance.MappingPolicyDir.IsMainCharacterMapped(character, out var mainEntity))
        {
            if (mainEntity.Value.GetLocalState().IsWaitingForSequence)
            {
                // update local player location
                RestrictPlayerLocation(mainEntity.Value, __instance);
            }

            if (DI.Instance.MappedField.CanLoadFromGame<MainCharacterComponent>(mainEntity.Value, out var load))
            {
                load.SetFromGame(MainCharacterComponent.Fields.IsFlying, __instance.IsFlying);
                load.SetFromGame(MainCharacterComponent.Fields.IsFalling, __instance.IsFalling);
                load.SetFromGame(MainCharacterComponent.Fields.IsLandingMove, __instance.IsLandingMove);
                load.SetFromGame(MainCharacterComponent.Fields.Velocity, __instance.Velocity.ToVector3());
                load.SetFromGame(MainCharacterComponent.Fields.MoveAcceleration, __instance.MoveAcceleration.ToVector3());
            }
            else if (DI.Instance.MappedField.CanSyncToGame<MainCharacterComponent>(mainEntity.Value, out var sync))
            {
                sync.SyncToGame(MainCharacterComponent.Fields.IsFlying, static (x, c) => c.IsFlying = x, __instance);
                sync.SyncToGame(MainCharacterComponent.Fields.IsFalling, static (x, c) => c.IsFalling = x, __instance);
                sync.SyncToGame(MainCharacterComponent.Fields.IsLandingMove, static (x, c) => c.IsLandingMove = x, __instance);
                sync.SyncToGame(MainCharacterComponent.Fields.Velocity.In<BUC_ABPCharacterData>(), __instance);
                sync.SyncToGame(MainCharacterComponent.Fields.MoveAcceleration.In<BUC_ABPCharacterData>(), __instance);
            }

            if (DI.Instance.MappedField.CanLoadFromGame<TransformComponent>(mainEntity.Value, out var loadTransform))
            {
                loadTransform.SetFromGame(TransformComponent.Fields.Position, __instance.ActorLocation.ToVector3());
                loadTransform.SetFromGame(TransformComponent.Fields.Rotation, __instance.ActorRotation.ToVector3());
            }
            else if (DI.Instance.MappedField.CanSyncToGame<TransformComponent>(mainEntity.Value, out var syncTransform))
            {
                var events = BUS_EventCollectionCS.Get(character);
                syncTransform.SyncToGame(static (comp, pair) =>
                {
                    if (!comp.Position.ToFVector().Equals(pair.__instance.ActorLocation, Constants.FloatComparisonTolerance) ||
                        !comp.Rotation.ToFRotator().Equals(pair.__instance.ActorRotation, Constants.FloatComparisonTolerance))
                    {
                        pair.events.Evt_InterpolationMove.Invoke(comp.Position.ToFVector(), comp.Rotation.ToFRotator(), Constants.ToleratedLatencyMs / 1000f, true, false, false, true);
                    }
                }, (events, __instance));

                if (__instance.RealWorldVelocity.Equals(FVector.ZeroVector, Constants.FloatComparisonTolerance))
                {
                    __instance.Velocity = FVector.ZeroVector;
                    // mainEntity.Velocity = FVector.ZeroVector.ToVector3();
                    __instance.MoveAcceleration = FVector.ZeroVector;
                    // mainEntity.MoveAcceleration = FVector.ZeroVector.ToVector3();
                    __instance.LastVelocity = FVector.ZeroVector;
                }
            }

            TeleportUtils.CheckForTeleportFinish(DI.Instance.MappedEvent, mainEntity.Value);
        }
        else if (DI.Instance.MappingPolicyDir.IsMonsterTamerMapped(character, out var tamerEntity))
        {
            var localTamer = tamerEntity.Value.GetLocalTamer();

            if (!localTamer.IsTamerSynced || !tamerEntity.Value.IsTamerValid || tamerEntity.Value.Pawn == null)
                return;

            if (DI.Instance.MappedField.CanLoadFromGame<AnimationComponent>(tamerEntity.Value, out var loadAnim))
            {
                loadAnim.SetFromGame(AnimationComponent.Fields.Velocity, __instance.Velocity.ToVector3());
                loadAnim.SetFromGame(AnimationComponent.Fields.MoveAcceleration, __instance.MoveAcceleration.ToVector3());
            }
            else if (DI.Instance.MappedField.CanSyncToGame<AnimationComponent>(tamerEntity.Value, out var syncAnim))
            {
                syncAnim.SyncToGame(AnimationComponent.Fields.Velocity.In<BUC_ABPCharacterData>(), __instance);
                syncAnim.SyncToGame(AnimationComponent.Fields.MoveAcceleration.In<BUC_ABPCharacterData>(), __instance);
            }

            if (DI.Instance.MappedField.CanLoadFromGame<TransformComponent>(tamerEntity.Value, out var loadTrans))
            {
                loadTrans.SetFromGame(TransformComponent.Fields.Position, __instance.ActorLocation.ToVector3());
                if (character is BGU_CharacterAI ai && ai.GetActorGuid(out var guid) && guid == "UGuid.HYS.JiRuHuo01")
                {
                    loadTrans.SetFromGame(TransformComponent.Fields.Rotation, ai.Mesh.GetSocketRotation(new FName("Head")).ToVector3());
                }
                else
                {
                    loadTrans.SetFromGame(TransformComponent.Fields.Rotation, __instance.ActorRotation.ToVector3());
                }
            }
            else if (DI.Instance.MappedField.CanSyncToGame<TransformComponent>(tamerEntity.Value, out var syncTrans))
            {
                var events = BUS_EventCollectionCS.Get(character);
                syncTrans.SyncToGame(static (comp, pair) =>
                {
                    var location = comp.Position.ToFVector();
                    var rotation = comp.Rotation.ToFRotator();

                    if (!location.Equals(pair.__instance.ActorLocation, Constants.FloatComparisonTolerance))
                    {
                        pair.events.Evt_InterpolationMove.Invoke(location, rotation, Constants.ToleratedLatencyMs / 1000f, true, false, false, true);
                    }
                }, (events, __instance));

                if (__instance.RealWorldVelocity.Equals(FVector.ZeroVector, Constants.FloatComparisonTolerance))
                {
                    __instance.Velocity = FVector.ZeroVector;
                    // mainEntity.Velocity = FVector.ZeroVector.ToVector3();
                    __instance.MoveAcceleration = FVector.ZeroVector;
                    // mainEntity.MoveAcceleration = FVector.ZeroVector.ToVector3();
                    __instance.LastVelocity = FVector.ZeroVector;
                }
            }
        }
    }

    private static void RestrictPlayerLocation(MainCharacterEntity mainEntity, BUC_ABPCharacterData characterData)
    {
        ref var localMainComp = ref mainEntity.GetLocalState();

        var distanceSq = localMainComp.JoiningSequenceLocation.Vector_DistanceSquared(characterData.ActorLocation);
        if (distanceSq > Constants.RestrictedMovementRadiusSquare)
        {
            characterData.ActorLocation = localMainComp.JoiningSequenceLocation + Constants.RestrictedMovementRadius * (characterData.ActorLocation - localMainComp.JoiningSequenceLocation).GetSafeNormal(); // cast from above
            mainEntity.Pawn?.SetActorLocation(characterData.ActorLocation, false, out _, true);
        }
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchSpiderMove
{
    [HarmonyTargetMethodHint("b1.BGU.BUAnim.BGAnimSpider", "BlueprintUpdateAnimation_Implementation")]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BGU.BUAnim.BGAnimSpider:BlueprintUpdateAnimation_Implementation");
    }

    public static void Postfix(AActor ___Owner, UBGUCharacterMovementComponent ___MovementComp)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        var ai = ___Owner as BGU_CharacterAI;

        if (ai == null)
            return;

        if (DI.Instance.MappingPolicyDir.IsMonsterTamerMapped(ai, out var tamerEntity))
        {
            var localTamer = tamerEntity.Value.GetLocalTamer();

            if (!localTamer.IsTamerSynced || !tamerEntity.Value.IsTamerValid || tamerEntity.Value.Pawn == null)
                return;

            if (DI.Instance.MappedField.CanLoadFromGame<AnimationComponent>(tamerEntity.Value, out var loadAnim))
            {
                loadAnim.SetFromGame(AnimationComponent.Fields.Velocity, ___MovementComp.Velocity.ToVector3());
                // loadAnim.SetFromGame(AnimationComponent.Fields.MoveAcceleration, moveComp.MoveAcceleration.ToVector3());
            }
            else if (DI.Instance.MappedField.CanSyncToGame<AnimationComponent>(tamerEntity.Value, out var syncAnim))
            {
                syncAnim.SyncToGame(static (comp, move) => { move.Velocity = comp.Velocity.ToFVector(); }, ___MovementComp);
                // TODO: Acceleration?
            }

            if (DI.Instance.MappedField.CanLoadFromGame<TransformComponent>(tamerEntity.Value, out var loadTrans))
            {
                var trans = ai.GetActorTransform();
                loadTrans.SetFromGame(TransformComponent.Fields.Position, trans.Translation.ToVector3());
                loadTrans.SetFromGame(TransformComponent.Fields.Rotation, trans.Rotation.Rotator().ToVector3());
            }
            else if (DI.Instance.MappedField.CanSyncToGame<TransformComponent>(tamerEntity.Value, out var syncTrans))
            {
                var events = BUS_EventCollectionCS.Get(ai);
                syncTrans.SyncToGame(static (comp, pair) =>
                {
                    var location = comp.Position.ToFVector();
                    var rotation = comp.Rotation.ToFRotator();

                    if (!location.Equals(pair.ai.GetActorLocation(), Constants.FloatComparisonTolerance))
                    {
                        pair.events.Evt_InterpolationMove.Invoke(location, rotation, Constants.ToleratedLatencyMs / 1000f, true, false, false, true);
                    }
                }, (events, ai));

                if (___MovementComp.Velocity.Equals(FVector.ZeroVector, Constants.FloatComparisonTolerance))
                {
                    ___MovementComp.Velocity = FVector.ZeroVector;
                }
            }
        }
    }
}

[HarmonyPatch(typeof(BUS_MovementSystem), "TickForInterpolationMove")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchTickForInterpolationMove
{
    public static void Postfix(BUS_MovementSystem __instance, BUC_MovementData ___MovementData, float DeltaTime, bool bForceUpdate = false)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        if (!___MovementData.IM_EnableMove)
            return;

        var owner = __instance.GetOwner() as BGUCharacterCS;
        if (owner is BGU_CharacterAI ai && ai.GetActorGuid(out var guid) && guid == "UGuid.HYS.JiRuHuo01")
        {
            var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(owner);
            if (!tamerEntity.HasValue)
                return;

            var boneNameLock = new FName(Constants.ChestCameraLockNode);
            var socketOffset = ai.Mesh.GetSocketTransform(boneNameLock, ERelativeTransformSpace.RTS_Component).GetLocation();

            var trans = tamerEntity.Value.GetTransform();
            var targetCenterPosition = trans.Position.ToFVector();
            var targetCenterRotation = trans.Rotation.ToFRotator();

            var rotation = targetCenterRotation; // TODO: Check interpolation: FMath.RInterpTo(meshTransform.Rotator(), targetCenterRotation, DeltaTime, 16f);
            var outRotation = rotation;
            var rotatedOffset = rotation.RotateVector(socketOffset);
            var outLocation = targetCenterPosition - rotatedOffset;

            ai.Mesh.SetWorldLocationAndRotation(outLocation, outRotation, false, out _, false);
        }
    }
}

[HarmonyPatch(typeof(BUS_UnitStateSystem), "OnUnitSimpleStateSet")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchOnUnitSimpleStateSet
{
    public static void Postfix(EBGUSimpleState SimpleState, bool IsRemove, BUS_UnitStateSystem __instance)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        var owner = __instance.GetOwner();

        if (!DI.Instance.MappedEntity.IsMapped(owner, out var entity))
            return;

        if (SimpleState is EBGUSimpleState.Immobilizing or EBGUSimpleState.InAnimationSyncing or EBGUSimpleState.PreAnimationSyncing)
            return;

        DI.Instance.MappedEvent.NotifyEcsIfApplicable(new UnitSimpleStateEvent(entity.Value, SimpleState, IsRemove), entity.Value);
    }
}

[HarmonyPatch(typeof(BUS_UnitStateSystem), "OnUnitStateTrigger")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchOnUnitStateTrigger
{
    public static void Postfix(EBUStateTrigger Trigger, float Time, bool NeedForceUpdate, BUS_UnitStateSystem __instance)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        if (Trigger == EBUStateTrigger.Die)
            return;

        var owner = __instance.GetOwner();

        if (!DI.Instance.MappedEntity.IsMapped(owner, out var entity))
            return;

        DI.Instance.MappedEvent.NotifyEcsIfApplicable(new UnitStateTriggerEvent(entity.Value, Trigger, Time, NeedForceUpdate), entity.Value);
    }
}

[HarmonyPatch(typeof(BUS_ABPHelperComp), "OnChangeMotionMatchingState")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchOnChangeMotionMatchingState
{
    public static void Postfix(EState_MM MMState, BUS_ABPHelperComp __instance)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        var owner = __instance.GetOwner();

        if (!DI.Instance.MappingPolicyDir.IsMonsterTamerMapped(owner as BGUCharacterCS, out var entity))
            return;

        DI.Instance.MappedEvent.NotifyEcsIfApplicable(new MotionMatchingStateEvent(entity.Value, MMState), entity.Value.Entity);
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchBuffBegin
{
    [HarmonyTargetMethodHint("b1.BUS_BuffComp", "BuffBegin")]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BUS_BuffComp:BuffBegin");
    }

    public static void Postfix(UActorCompBaseCS __instance, int BuffID, float Duration)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        if (UnsynchronizedBuffsData.Ids.Contains(BuffID))
            return;

        var owner = __instance.GetOwner();

        if (!DI.Instance.MappedEntity.IsMapped(owner, out var entity))
            return;

        DI.Instance.MappedEvent.NotifyEcsIfApplicable(new AddBuffEvent(entity.Value, BuffID, Duration), entity.Value);
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchBuffRemove
{
    [HarmonyTargetMethodHint("b1.BUS_BuffComp", "BuffRemove")]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BUS_BuffComp:BuffRemove");
    }

    public static void Postfix(UActorCompBaseCS __instance, int BuffID, EBuffEffectTriggerType RemoveTriggerType, int InLayer, bool WithTriggerRemoveEffect)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        if (UnsynchronizedBuffsData.Ids.Contains(BuffID))
            return;

        var owner = __instance.GetOwner();

        if (!DI.Instance.MappedEntity.IsMapped(owner, out var entity))
            return;

        DI.Instance.MappedEvent.NotifyEcsIfApplicable(new RemoveBuffEvent(entity.Value, BuffID, RemoveTriggerType, InLayer, WithTriggerRemoveEffect), entity.Value);
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchBuffRemoveImmediately
{
    [HarmonyTargetMethodHint("b1.BUS_BuffComp", "BuffRemoveImmediately")]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BUS_BuffComp:BuffRemoveImmediately");
    }

    public static void Postfix(UActorCompBaseCS __instance, int BuffID, EBuffEffectTriggerType RemoveTriggerType, bool WithTriggerRemoveEffect)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        if (UnsynchronizedBuffsData.Ids.Contains(BuffID))
            return;

        var owner = __instance.GetOwner();

        if (!DI.Instance.MappedEntity.IsMapped(owner, out var entity))
            return;

        DI.Instance.MappedEvent.NotifyEcsIfApplicable(new RemoveBuffEvent(entity.Value, BuffID, RemoveTriggerType, -1, WithTriggerRemoveEffect), entity.Value);
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchBuffAllRemove
{
    [HarmonyTargetMethodHint("b1.BUS_BuffComp", "BuffAllRemove")]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method("b1.BUS_BuffComp:BuffAllRemove");
    }

    public static void Postfix(UActorCompBaseCS __instance, EBuffEffectTriggerType RemoveTriggerType, bool WithTriggerRemoveEffect)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        var owner = __instance.GetOwner();

        if (!DI.Instance.MappedEntity.IsMapped(owner, out var entity))
            return;

        DI.Instance.MappedEvent.NotifyEcsIfApplicable(new RemoveAllBuffsEvent(entity.Value, RemoveTriggerType, WithTriggerRemoveEffect), entity.Value);
    }
}

[HarmonyPatch(typeof(BGUCharacterCS), "SetTeamIDInCS")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchSetTeamIDInCS
{
    public static void Postfix(BGUCharacterCS __instance, int NewTeamID)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(__instance);
        if (!tamerEntity.HasValue || !DI.Instance.ClientOwnership.OwnsEntity(tamerEntity.Value.Entity))
            return;

        tamerEntity.Value.SetTeam(new TeamComponent { TeamId = NewTeamID });
    }
}

[HarmonyPatch]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchBeAttackedDeadEventSettlementProcess
{
    [HarmonyTargetMethodHint("b1.BUS_BeAttackedComp.BeAttackedEvent_Dead", "EventSettlementProcess")]
    private static MethodBase TargetMethod()
    {
        var innerType = AccessTools.Inner(typeof(BUS_BeAttackedComp), "BeAttackedEvent_Dead");
        return AccessTools.Method(innerType, "EventSettlementProcess");
    }

    public static bool Prefix(BGUCharacterCS ___VictimChr)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(___VictimChr);
        if (!tamerEntity.HasValue)
            return true;

        // Owned entity - do not trigger unit dead
        if (!DI.Instance.ClientOwnership.OwnsEntity(tamerEntity.Value.Entity))
            return false;

        return true;
    }
}

[HarmonyPatch(typeof(CharacterAttrDataInitTemplate), nameof(CharacterAttrDataInitTemplate.InitDataPreBeginPlay))]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal static class PatchTamerStatResetOnBeginPlay
{
    public static void Postfix(AActor ___Owner)
    {
        if (___Owner is not BGU_CharacterAI ai)
            return;

        var tamer = ai.GetTamerOwner();

        if (tamer.IsNullOrDestroyed())
            return; // no tamer

        var tamerEntity = DI.Instance.PawnState.GetEntityByTamer(tamer);

        if (!tamerEntity.HasValue)
            return; // not found

        if (!DI.Instance.ClientOwnership.OwnsEntity(tamerEntity.Value.Entity))
            return; // not owned

        ref var localTamer = ref tamerEntity.Value.GetLocalTamer();

        if (!localTamer.IsTamerSynced)
            return; // not synced

        ref var hpComp = ref tamerEntity.Value.GetHp();

        hpComp.HpMultiplier = 1; // Reset multiplier so that the HP scaling system will re-scale it again
    }
}

[HarmonyPatch(typeof(BUC_BattleStateData), "IsUnitInBattle")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchIsUnitInBattle
{
    public static bool Prefix(BUC_BattleStateData __instance, ref bool __result)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        if (!__instance.IsPlayerUnit)
            return true;

        var configuration = DI.Instance.GameplayConfiguration;
        if (configuration.EnableCustomIsPlayerInBattle)
        {
            __result = configuration.IsPlayerInBattle();
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(BIC_GlobalActorData), nameof(BIC_GlobalActorData.GetActorEntity))]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchGetActorEntity
{
    public static bool Prefix(BIC_GlobalActorData __instance, ref bool __result, string UnitGuid, out Entity Entity)
    {
        Entity = Entity.Null;
        if (string.IsNullOrEmpty(UnitGuid))
        {
            __result = false;
            return false;
        }

        if (__instance.ActorGuid2Entity.TryGetValue(UnitGuid, out var value))
        {
            var count = value.Count;
            // Return local player entity if player guid is queried.
            if (count > 1 && DI.Instance.PlayerState.LocalMainCharacter.HasValue && value[0] is BGUPlayerCharacterCS)
            {
                Entity = DI.Instance.PlayerState.LocalMainCharacter.Value.Pawn.ToEntity();
                if (Entity != Entity.Null)
                {
                    __result = true;
                    return false;
                }
            }

            if (count > 0)
            {
                for (var num = count - 1; num >= 0; num--)
                {
                    Entity = value[num].ToEntity();
                    if (Entity != Entity.Null)
                    {
                        __result = true;
                        return false;
                    }
                }
            }
        }

        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(BGU_AbnormalStateHandlerBase), "PlayDBC_ByType")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchPlayDBC_ByType
{
    public static void Postfix(BGUCharacterCS ___OwnerChr, EAbnormalStateType ___AbnormalType, EAbnromalDispActionType ActionType)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        if (!DI.Instance.MappedEntity.IsMapped(___OwnerChr, out var entity))
            return;

        DI.Instance.MappedEvent.NotifyEcsIfApplicable(new PlayBaneEffectEvent(entity.Value, ___AbnormalType, ActionType), entity.Value);
    }
}

[HarmonyPatch(typeof(BGU_AbnormalStateHandlerBase), "EndAllDBC")]
[HarmonyPatchCategory(PatchCategory.Connected)]
internal class PatchEndAllDBC
{
    public static void Postfix(BGUCharacterCS ___OwnerChr, EAbnormalStateType ___AbnormalType)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        if (!DI.Instance.MappedEntity.IsMapped(___OwnerChr, out var entity))
            return;

        DI.Instance.MappedEvent.NotifyEcsIfApplicable(new StopBaneEffectEvent(entity.Value, ___AbnormalType), entity.Value);
    }
}