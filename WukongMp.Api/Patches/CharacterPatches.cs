using System.Reflection;
using b1;
using BtlShare;
using HarmonyLib;
using PreludeLib.Attributes;
using PreludeLib.Compat;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.DTO;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Patches
{
    [HarmonyPatch(typeof(BUC_AttrContainer), nameof(BUC_AttrContainer.OnTick))]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public static class PatchAttrs
    {
        public static void Postfix(BUC_AttrContainer __instance)
        {
            if (!DI.Instance.AreaState.InRoom)
                return;

            if (DI.Instance.PlayerState.LocalMainCharacter == null)
                return;

            if (__instance.Owner.IsNullOrDestroyed())
            {
                Logging.LogError("Owner is null or destroyed");
                return;
            }

            if (__instance.Owner == DI.Instance.PlayerState.LocalMainCharacter.Value.GetLocalState().Pawn)
            {
                return; // players own their characters
            }

            var mainEntity = DI.Instance.PawnState.GetEntityByPlayerPawn(__instance.Owner);

            // remote player - sync properties and HP

            if (mainEntity != null)
            {
                ref var mainComp = ref mainEntity.Value.GetState();

                // set their attributes
                foreach (var (attr, value) in mainComp.Attributes)
                {
                    __instance.SetFloatValue((EBGUAttrFloat)attr, value);
                }

                if (mainComp.Hp <= -80000)
                {
                    Logging.LogError("Would set HP to {HP} but will not (OOB fall damage)", mainComp.Hp);
                    return;
                }

                if (mainComp.Hp.Equals(__instance.GetFloatValue(EBGUAttrFloat.Hp), Constants.FloatComparisonTolerance))
                {
                    return; // do not reapply the same value
                }

                __instance.SetFloatValue(EBGUAttrFloat.Hp, mainComp.Hp);
                return;
            }

            // remote monster - sync HP
            var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(__instance.Owner as BGUCharacterCS);
            if (!tamerEntity.HasValue)
                return;

            // owned, skip
            if (DI.Instance.ClientOwnership.OwnsEntity(tamerEntity.Value.Entity))
                return;

            ref var localTamer = ref tamerEntity.Value.GetLocalTamer();

            if (!localTamer.IsTamerSynced)
            {
                Logging.LogDebug("Monster {Name} is not synced, skipping HP update", __instance.Owner.GetName());
                return;
            }

            ref var hpComp = ref tamerEntity.Value.GetHp();

            if (!hpComp.HpMaxBase.Equals(__instance.GetFloatValue(EBGUAttrFloat.HpMaxBase), Constants.FloatComparisonTolerance))
            {
                __instance.SetFloatValue(EBGUAttrFloat.HpMaxBase, hpComp.HpMaxBase);
            }

            if (!hpComp.Hp.Equals(__instance.GetFloatValue(EBGUAttrFloat.Hp), Constants.FloatComparisonTolerance))
            {
                __instance.SetFloatValue(EBGUAttrFloat.Hp, hpComp.Hp);
            }
        }
    }


    [HarmonyPatch(typeof(BUS_AttrComp), "SetFloatValue")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public static class PatchHp
    {
        public static bool Prefix(BUS_AttrComp __instance, EBGUAttrFloat AttrID)
        {
            if (!DI.Instance.AreaState.InRoom)
                return true;

            if (AttrID == EBGUAttrFloat.Hp)
            {
                var owner = __instance.GetOwner();
                var netId = DI.Instance.PawnState.GetNetworkIdByActor(owner);
                if (netId.HasValue)
                    return DI.Instance.ClientOwnership.OwnsEntity(netId.Value);
            }

            return true;
        }

        public static void Postfix(BUS_AttrComp __instance, BUC_AttrContainer ___AttrContainer, EBGUAttrFloat AttrID)
        {
            if (!DI.Instance.AreaState.InRoom)
                return;

            var playerState = DI.Instance.PlayerState;
            var owner = __instance.GetOwner();

            if (owner.IsNullOrDestroyed())
            {
                Logging.LogError("Owner is null or destroyed");
                return;
            }

            var result = ___AttrContainer.GetFloatValue(AttrID);

            var mainEntity = playerState.LocalMainCharacter;

            if (AttrID == EBGUAttrFloat.Hp)
            {
                if (mainEntity != null && owner == mainEntity.Value.GetLocalState().Pawn)
                {
                    ref var mainComp = ref mainEntity.Value.GetState();

                    if (!mainComp.Hp.Equals(result, Constants.FloatComparisonTolerance))
                    {
                        mainComp.Hp = result;

                        if (mainComp.Hp > 0)
                        {
                            mainComp.IsDead = false;
                        }
                    }
                }
                else
                {
                    var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(owner as BGUCharacterCS);

                    if (!tamerEntity.HasValue)
                        return; // not found

                    if (!DI.Instance.ClientOwnership.OwnsEntity(tamerEntity.Value.Entity))
                        return; // not owned

                    ref var localTamer = ref tamerEntity.Value.GetLocalTamer();

                    if (!localTamer.IsTamerSynced)
                        return; // not synced

                    ref var hpComp = ref tamerEntity.Value.GetHp();

                    hpComp.HpMaxBase = ___AttrContainer.GetFloatValue(EBGUAttrFloat.HpMaxBase);
                    hpComp.Hp = result;
                }
            }

            if (mainEntity != null && Constants.SyncedAttributes.Contains(AttrID) && owner == mainEntity.Value.GetLocalState().Pawn)
            {
                ref var mainComp = ref mainEntity.Value.GetState();

                if (mainComp.Attributes.TryGetAttribute((byte)AttrID, out var existing)
                    && existing.Equals(result, Constants.FloatComparisonTolerance))
                {
                    return;
                }

                mainComp.Attributes.SetAttribute((byte)AttrID, result);

                // some attributes may influence other attributes
                var calc = AttrMgr<EBGUAttrFloat, float>.getInstance().GetCalc(AttrID, out var valid);
                if (valid)
                {
                    var finalVal = ___AttrContainer.GetFloatValue(calc.finalVal);
                    mainComp.Attributes.SetAttribute((byte)calc.finalVal, finalVal);
                }
            }
        }
    }

    // NOTE: Runs multithreaded
    [HarmonyPatch(typeof(BUC_ABPCharacterData), nameof(BUC_ABPCharacterData.Update_GameThread))]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchCharacterAnimation
    {
        public static void Postfix(BUC_ABPCharacterData? __instance, AActor Owner, IBUC_ABPHelperData HelperData, float DeltaTime)
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

            var playerState = DI.Instance.PlayerState;
            var pawnState = DI.Instance.PawnState;

            var mainEntity = playerState.LocalMainCharacter;

            if (mainEntity != null && character == mainEntity.Value.GetLocalState().Pawn)
            {
                ref var main = ref mainEntity.Value.GetState();
                ref var localMain = ref mainEntity.Value.GetLocalState();

                if (localMain.IsWaitingForSequence)
                {
                    // update local player location
                    RestrictPlayerLocation(mainEntity.Value, __instance);
                }

                if (main.IsFlying != __instance.IsFlying)
                {
                    main.IsFlying = __instance.IsFlying;
                }

                if (main.IsFalling != __instance.IsFalling)
                {
                    main.IsFalling = __instance.IsFalling;
                }

                if (main.IsLandingMove != __instance.IsLandingMove)
                {
                    main.IsLandingMove = __instance.IsLandingMove;
                }

                if (!main.Velocity.ToFVector().Equals(__instance.Velocity, Constants.FloatComparisonTolerance))
                {
                    main.Velocity = __instance.Velocity.ToVector3();
                }

                if (!main.MoveAcceleration.ToFVector().Equals(__instance.MoveAcceleration, Constants.FloatComparisonTolerance))
                {
                    main.MoveAcceleration = __instance.MoveAcceleration.ToVector3();
                }

                if (!main.Location.ToFVector().Equals(__instance.ActorLocation, Constants.FloatComparisonTolerance))
                {
                    main.Location = __instance.ActorLocation.ToVector3();
                }

                if (!main.Rotation.ToFRotator().Equals(__instance.ActorRotation, Constants.FloatComparisonTolerance))
                {
                    main.Rotation = __instance.ActorRotation.ToVector3();
                }

                TeleportUtils.CheckForTeleportFinish(mainEntity.Value);
            }
            else
            {
                var otherMainEntity = pawnState.GetEntityByPlayerPawn(character);

                if (otherMainEntity != null)
                {
                    ref var otherMain = ref otherMainEntity.Value.GetState();

                    var events = BUS_EventCollectionCS.Get(character);

                    __instance.IsFlying = otherMain.IsFlying;
                    __instance.IsFalling = otherMain.IsFalling;
                    __instance.IsLandingMove = otherMain.IsLandingMove;
                    __instance.Velocity = otherMain.Velocity.ToFVector();

                    if (__instance.Velocity.Equals(FVector.ZeroVector, Constants.FloatComparisonTolerance))
                    {
                        __instance.Velocity = FVector.ZeroVector;
                        otherMain.Velocity = FVector.ZeroVector.ToVector3();
                    }

                    __instance.MoveAcceleration = otherMain.MoveAcceleration.ToFVector();
                    if (__instance.MoveAcceleration.Equals(FVector.ZeroVector, Constants.FloatComparisonTolerance))
                    {
                        __instance.MoveAcceleration = FVector.ZeroVector;
                        otherMain.MoveAcceleration = FVector.ZeroVector.ToVector3();
                    }

                    if (!otherMain.Location.ToFVector().Equals(__instance.ActorLocation, Constants.FloatComparisonTolerance))
                    {
                        events.Evt_InterpolationMove.Invoke(otherMain.Location.ToFVector(), otherMain.Rotation.ToFRotator(), Constants.ToleratedLatencyMs / 1000f, true, false, false, true);
                    }

                    if (__instance.RealWorldVelocity.Equals(FVector.ZeroVector, Constants.FloatComparisonTolerance))
                    {
                        __instance.Velocity = FVector.ZeroVector;
                        otherMain.Velocity = FVector.ZeroVector.ToVector3();
                        __instance.MoveAcceleration = FVector.ZeroVector;
                        otherMain.MoveAcceleration = FVector.ZeroVector.ToVector3();
                        __instance.LastVelocity = FVector.ZeroVector;
                    }

                    TeleportUtils.CheckForTeleportFinish(otherMainEntity.Value);
                }
                else
                {
                    // maybe it's a monster
                    var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(character);

                    if (tamerEntity.HasValue)
                    {
                        ref var localTamer = ref tamerEntity.Value.GetLocalTamer();

                        if (!localTamer.IsTamerSynced || !localTamer.IsTamerValid || localTamer.Pawn == null)
                            return;

                        if (DI.Instance.ClientOwnership.OwnsEntity(tamerEntity.Value.Entity))
                        {
                            ref var anim = ref tamerEntity.Value.GetAnimation();
                            anim.Velocity = __instance.Velocity.ToVector3();
                            anim.MoveAcceleration = __instance.MoveAcceleration.ToVector3();

                            ref var trans = ref tamerEntity.Value.GetTransform();
                            trans.Position = __instance.ActorLocation.ToVector3();

                            if (character is BGU_CharacterAI ai && ai.GetActorGuid(out var guid) && guid == "UGuid.HYS.JiRuHuo01")
                            {
                                trans.Rotation = ai.Mesh.GetSocketRotation(new FName("Head")).ToVector3();
                            }
                            else
                            {
                                trans.Rotation = __instance.ActorRotation.ToVector3();
                            }
                        }
                        else
                        {
                            ref var anim = ref tamerEntity.Value.GetAnimation();

                            __instance.Velocity = anim.Velocity.ToFVector();
                            __instance.MoveAcceleration = anim.MoveAcceleration.ToFVector();
                            __instance.MovementComp.Velocity = anim.Velocity.ToFVector();

                            var events = BUS_EventCollectionCS.Get(localTamer.Pawn);

                            ref var trans = ref tamerEntity.Value.GetTransform();
                            var location = trans.Position.ToFVector();
                            var rotation = trans.Rotation.ToFRotator();

                            if (!location.Equals(__instance.ActorLocation, Constants.FloatComparisonTolerance) ||
                                !rotation.Equals(__instance.ActorRotation, Constants.FloatComparisonTolerance))
                            {
                                events.Evt_InterpolationMove.Invoke(location, rotation, Constants.ToleratedLatencyMs / 1000f, true, false, false, true);
                            }
                        }
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
                localMainComp.Pawn?.SetActorLocation(characterData.ActorLocation, false, out _, true);
            }
        }
    }

    [HarmonyPatch(typeof(BUS_MovementSystem), "TickForInterpolationMove")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchTickForInterpolationMove
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
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchOnUnitSimpleStateSet
    {
        public static void Postfix(EBGUSimpleState SimpleState, bool IsRemove, BUS_UnitStateSystem __instance)
        {
            if (!DI.Instance.AreaState.InRoom)
                return;

            var owner = __instance.GetOwner();
            NetworkId? netId = null;

            var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(owner);
            if (tamerEntity.HasValue && DI.Instance.ClientOwnership.OwnsEntity(tamerEntity.Value.Entity))
            {
                netId = tamerEntity.Value.GetMeta().NetId;
            }
            else
            {
                var playerEntity = DI.Instance.PlayerState.LocalMainCharacter;
                if (playerEntity.HasValue && playerEntity.Value.GetLocalState().Pawn == owner)
                {
                    netId = playerEntity.Value.GetMeta().NetId;
                }
            }

            if (netId.HasValue)
            {
                if (SimpleState == EBGUSimpleState.Immobilizing)
                    return;

                DI.Instance.Rpc.SendUnitSimpleState(new SimpleStateData(netId.Value, SimpleState, IsRemove));
            }
        }
    }

    [HarmonyPatch(typeof(BUS_UnitStateSystem), "OnUnitStateTrigger")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchOnUnitStateTrigger
    {
        public static void Postfix(EBUStateTrigger Trigger, float Time, bool NeedForceUpdate, BUS_UnitStateSystem __instance)
        {
            if (!DI.Instance.AreaState.InRoom)
                return;

            if (Trigger == EBUStateTrigger.Die)
                return;

            var playerState = DI.Instance.PlayerState;
            var owner = __instance.GetOwner();

            var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(owner);
            if (tamerEntity.HasValue && DI.Instance.ClientOwnership.OwnsEntity(tamerEntity.Value.Entity))
            {
                var netId = tamerEntity.Value.GetMeta().NetId;
                DI.Instance.Rpc.SendUnitStateTrigger(new StateTriggerData(netId, Trigger, Time, NeedForceUpdate));
            }

            if (owner == playerState.LocalMainCharacter?.GetLocalState().Pawn)
            {
                var mainEntity = playerState.LocalMainCharacter;
                var netId = mainEntity.Value.GetMeta().NetId;

                DI.Instance.Rpc.SendUnitStateTrigger(new StateTriggerData(netId, Trigger, Time, NeedForceUpdate));
            }
        }
    }

    [HarmonyPatch(typeof(BUS_ABPHelperComp), "OnChangeMotionMatchingState")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchOnChangeMotionMatchingState
    {
        public static void Postfix(EState_MM MMState, BUS_ABPHelperComp __instance)
        {
            if (!DI.Instance.AreaState.InRoom)
                return;

            var owner = __instance.GetOwner();
            var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(owner);

            if (!tamerEntity.HasValue || !DI.Instance.ClientOwnership.OwnsEntity(tamerEntity.Value.Entity))
                return;

            var netId = tamerEntity.Value.GetMeta().NetId;
            DI.Instance.Rpc.SendMotionMatchingState(new MotionMatchingStateData(netId, MMState));
        }
    }

    [HarmonyPatch]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchBuffBegin
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
            var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(owner);

            if (tamerEntity.HasValue && DI.Instance.ClientOwnership.OwnsEntity(tamerEntity.Value.Entity))
            {
                var netId = tamerEntity.Value.GetMeta().NetId;
                DI.Instance.Rpc.SendAddBuff(new BuffAddData(netId, BuffID, Duration));
                return;
            }

            var myEntity = DI.Instance.PlayerState.LocalMainCharacter;
            if (myEntity.HasValue && myEntity.Value.GetLocalState().Pawn == owner)
            {
                var myId = myEntity.Value.GetMeta().NetId;
                DI.Instance.Rpc.SendAddBuff(new BuffAddData(myId, BuffID, Duration));
            }
        }
    }

    [HarmonyPatch]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchBuffRemove
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
            var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(owner);
            if (tamerEntity.HasValue && DI.Instance.ClientOwnership.OwnsEntity(tamerEntity.Value.Entity))
            {
                var netId = tamerEntity.Value.GetMeta().NetId;
                DI.Instance.Rpc.SendRemoveBuff(new BuffRemoveData(netId, BuffID, RemoveTriggerType, InLayer, WithTriggerRemoveEffect));
                return;
            }

            var myEntity = DI.Instance.PlayerState.LocalMainCharacter;
            if (myEntity.HasValue && myEntity.Value.GetLocalState().Pawn == owner)
            {
                var myId = myEntity.Value.GetMeta().NetId;
                DI.Instance.Rpc.SendRemoveBuff(new BuffRemoveData(myId, BuffID, RemoveTriggerType, InLayer, WithTriggerRemoveEffect));
            }
        }
    }

    [HarmonyPatch]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchBuffRemoveImmediately
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
            var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(owner);
            if (tamerEntity.HasValue && DI.Instance.ClientOwnership.OwnsEntity(tamerEntity.Value.Entity))
            {
                var netId = tamerEntity.Value.GetMeta().NetId;
                DI.Instance.Rpc.SendRemoveBuff(new BuffRemoveData(netId, BuffID, RemoveTriggerType, -1, WithTriggerRemoveEffect));
                return;
            }

            var myEntity = DI.Instance.PlayerState.LocalMainCharacter;
            if (myEntity.HasValue && myEntity.Value.GetLocalState().Pawn == owner)
            {
                var myId = myEntity.Value.GetMeta().NetId;
                DI.Instance.Rpc.SendRemoveBuff(new BuffRemoveData(myId, BuffID, RemoveTriggerType, -1, WithTriggerRemoveEffect));
            }
        }
    }

    [HarmonyPatch]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchBuffAllRemove
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
            var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(owner);
            if (tamerEntity.HasValue && DI.Instance.ClientOwnership.OwnsEntity(tamerEntity.Value.Entity))
            {
                var netId = tamerEntity.Value.GetMeta().NetId;
                DI.Instance.Rpc.SendRemoveAllBuffs(new BuffRemoveAllData(netId, RemoveTriggerType, WithTriggerRemoveEffect));
                return;
            }

            var myEntity = DI.Instance.PlayerState.LocalMainCharacter;
            if (myEntity.HasValue && myEntity.Value.GetLocalState().Pawn == owner)
            {
                var myId = myEntity.Value.GetMeta().NetId;
                DI.Instance.Rpc.SendRemoveAllBuffs(new BuffRemoveAllData(myId, RemoveTriggerType, WithTriggerRemoveEffect));
            }
        }
    }

    [HarmonyPatch(typeof(BGUCharacterCS), "SetTeamIDInCS")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchSetTeamIDInCS
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
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchBeAttackedDeadEventSettlementProcess
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
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public static class PatchTamerStatResetOnBeginPlay
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
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchIsUnitInBattle
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
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchGetActorEntity
    {
        public static bool Prefix(BIC_GlobalActorData __instance, ref bool __result, string UnitGuid, out b1.ECS.Entity Entity)
        {
            Entity = b1.ECS.Entity.Null;
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
                    Entity = ECSExtension.ToEntity(DI.Instance.PlayerState.LocalMainCharacter.Value.GetLocalState().Pawn);
                    if (Entity != b1.ECS.Entity.Null)
                    {
                        __result = true;
                        return false;
                    }
                }
                if (count > 0)
                {
                    for (var num = count - 1; num >= 0; num--)
                    {
                        Entity = ECSExtension.ToEntity(value[num]);
                        if (Entity != b1.ECS.Entity.Null)
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
}