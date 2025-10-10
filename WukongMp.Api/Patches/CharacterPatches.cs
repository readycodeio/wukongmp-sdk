using b1;
using BtlShare;
using HarmonyLib;
using System.Reflection;
using PreludeLib.Attributes;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Compat;
using WukongMp.Api.Configuration;
using WukongMp.Api.DTO;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Patches
{
    [HarmonyPatch(typeof(BUC_AttrContainer), nameof(BUC_AttrContainer.OnTick))]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public static class CoopPatchAttrs
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

            var mainEntity = DI.Instance.PawnState.GetByEntityByPlayerPawn(__instance.Owner);

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

                var set = __instance.SetFloatValue(EBGUAttrFloat.Hp, mainComp.Hp);

                if (!set.Equals(mainComp.Hp, Constants.FloatComparisonTolerance))
                {
                    Logging.LogDebug("Attempted to set player {PlayerName} HP to {DesiredHp}, instead set to {SetHp}", mainComp.CharacterNickName, mainComp.Hp, set);
                }

                if (mainComp.IsDead)
                {
                    var events = BUS_EventCollectionCS.Get(__instance.Owner);

                    if (events == null)
                    {
                        Logging.LogError("events are null");
                        return;
                    }

                    Logging.LogDebug("Applying unit dead for player {PlayerId}", mainComp.PlayerId);
                    events.Evt_UnitDead!.Invoke(__instance.Owner, EDeadReason.SkillDamage);
                }

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

    [HarmonyPatch(typeof(CharacterAttrDataInitTemplate), nameof(CharacterAttrDataInitTemplate.InitDataPreBeginPlay))]
    [HarmonyPatchCategory(Constants.CoopPatches)]
    public static class PatchTamerStatResetOnBeginPlay
    {
        public static void Postfix(AActor ___Owner)
        {
            if (___Owner is not BGU_CharacterAI ai)
                return;

            var tamer = ai.GetTamerOwner();

            if (tamer.IsNullOrDestroyed())
                return; // no tamer

            var tamerEntity = DI.Instance.PawnState.GetByEntityByTamer(tamer);

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


    [HarmonyPatch(typeof(BUS_AttrComp), "SetFloatValue")]
    [HarmonyPatchCategory(Constants.CoopPatches)]
    public static class CoopPatchHp
    {
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

                TeleportUtils.UpdatePlayerPosition(mainEntity.Value, DeltaTime);
            }
            else
            {
                var otherMainEntity = pawnState.GetByEntityByPlayerPawn(character);

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

                    TeleportUtils.UpdatePlayerPosition(otherMainEntity.Value, DeltaTime);
                }
                else
                {
                    // maybe it's a monster
                    var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(character);

                    if (tamerEntity.HasValue)
                    {
                        ref var localTamer = ref tamerEntity.Value.GetLocalTamer();

                        if (!localTamer.IsTamerSynced)
                        {
                            return;
                        }

                        if (DI.Instance.ClientOwnership.OwnsEntity(tamerEntity.Value.Entity))
                        {
                            ref var anim = ref tamerEntity.Value.GetAnimation();
                            anim.Velocity = __instance.Velocity.ToVector3();
                            anim.MoveAcceleration = __instance.MoveAcceleration.ToVector3();

                            ref var trans = ref tamerEntity.Value.GetTranslation();
                            trans.Position = __instance.ActorLocation.ToVector3();
                            trans.Rotation = __instance.ActorRotation.ToVector3();
                        }
                        else
                        {
                            ref var anim = ref tamerEntity.Value.GetAnimation();

                            __instance.Velocity = anim.Velocity.ToFVector();
                            __instance.MoveAcceleration = anim.MoveAcceleration.ToFVector();
                            __instance.MovementComp.Velocity = anim.Velocity.ToFVector();

                            var events = BUS_EventCollectionCS.Get(localTamer.Pawn);

                            ref var trans = ref tamerEntity.Value.GetTranslation();
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

    [HarmonyPatch(typeof(BGU_UnrealWorldUtil), "DestroyActor")]
    [HarmonyPatchCategory(Constants.PvpPatches)]
    public class PatchDestroyActor
    {
        public static void Postfix(AActor Actor)
        {
            if (!DI.Instance.AreaState.InRoom)
                return;

            if (Actor is BGUCharacterCS character)
            {
                var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(character);
                if (tamerEntity.HasValue)
                {
                    Logging.LogDebug("DestroyActor called for not cleaned up monster: {Name}", Actor.GetFullName());

                    // only clean up own monsters
                    if (!DI.Instance.ClientOwnership.OwnsEntity(tamerEntity.Value.Entity))
                    {
                        Logging.LogDebug("Skipping cleanup for remote monster");
                        return;
                    }

                    Logging.LogDebug("Cleaning up monster: {Name}", Actor.GetFullName());
                    TamerUtils.CleanupMonster(tamerEntity.Value);
                }

                var tamer = character.GetTamerOwner();
                if (tamer != null)
                {
                    BGU_UnrealWorldUtil.DestroyActor(tamer);
                }
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
            var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(owner);
            if (tamerEntity.HasValue && DI.Instance.ClientOwnership.OwnsEntity(tamerEntity.Value.Entity))
            {
                if (SimpleState == EBGUSimpleState.Immobilizing)
                    return;

                var netId = tamerEntity.Value.GetMeta().NetId;

                DI.Instance.Rpc.SendUnitSimpleState(new SimpleStateData(netId, SimpleState, IsRemove));
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

            var playerState = DI.Instance.PlayerState;
            var owner = __instance.GetOwner();

            var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(owner);
            if (tamerEntity.HasValue && DI.Instance.ClientOwnership.OwnsEntity(tamerEntity.Value.Entity))
            {
                if (Trigger == EBUStateTrigger.Die)
                    return;

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
}