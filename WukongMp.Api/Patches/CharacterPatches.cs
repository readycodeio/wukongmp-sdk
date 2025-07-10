using System.Reflection;
using b1;
using BtlShare;
using HarmonyLib;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Wukong.Components;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.DTO;
using WukongMp.Api.ECS;
using WukongMp.Api.Old;
using WukongMp.Api.Old.State;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Patches
{
    [HarmonyPatch(typeof(BUC_AttrContainer), nameof(BUC_AttrContainer.OnTick))]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public static class PatchAttrs
    {
        public static void Postfix(BUC_AttrContainer __instance)
        {
            if (!DI.Instance.RelayClient.InRoom)
                return;

            var players = DI.Instance.Players;

            if (__instance.Owner.IsNullOrDestroyed())
            {
                Logging.LogError("Owner is null or destroyed");
                return;
            }

            if (DI.Instance.RelayClient.IsMasterClient)
            {
                // master client always has the latest data for himself, but may need to apply it for others
                if (__instance.Owner == players.LocalPlayerState.Pawn)
                    return;

                var playerState = players.GetPlayerByActor(__instance.Owner);
                if (playerState != null)
                {
                    foreach (var (attr, value) in playerState.Attributes)
                    {
                        __instance.SetFloatValue(attr, value);
                    }
                }

                return;
            }

            // for clients, their own attributes are already set by them, and they do not care about attributes of other clients / monsters
            // because it's the master client that ultimately calculates damage in combat

            if (__instance.Owner == players.LocalPlayerState.Pawn)
            {
                // local player (client)
                if (players.LocalPlayerState.Hp <= -80000)
                {
                    Logging.LogWarning("Would set HP to {HP}, but will not (OOB fall damage)", players.LocalPlayerState.Hp);
                    return;
                }

                var currentHp = __instance.GetFloatValue(EBGUAttrFloat.Hp);

                if (players.LocalPlayerState.Hp.Equals(currentHp, Constants.FloatComparisonTolerance))
                {
                    return; // do not reapply the same value
                }

                var set = __instance.SetFloatValue(EBGUAttrFloat.Hp, players.LocalPlayerState.Hp);

                if (!set.Equals(players.LocalPlayerState.Hp, Constants.FloatComparisonTolerance))
                {
                    Logging.LogWarning("Attempted to set player {PlayerName} HP to {DesiredHp}, instead set to {SetHp}", players.LocalPlayerState.NickName, players.LocalPlayerState.Hp, set);
                    DI.Instance.PlayerProperty.CachePlayerProperty(nameof(PlayerState.Hp), set);
                }

                if (players.LocalPlayerState.IsDead)
                {
                    var events = BUS_EventCollectionCS.Get(__instance.Owner);

                    if (events == null)
                    {
                        Logging.LogError("events are null");
                        return;
                    }

                    Logging.LogDebug("Applying unit dead for player {PlayerId}", players.LocalPlayerState.PlayerId);

                    GameLoopPatch.QueueOnGameThread(() => { events.Evt_UnitDead!.Invoke(__instance.Owner, EDeadReason.SkillDamage); }, "Evt_UnitDead");
                }
            }
            else
            {
                var playerState = players.GetPlayerByActor(__instance.Owner);

                // remote player
                if (playerState != null)
                {
                    // set their attributes
                    foreach (var (attr, value) in playerState.Attributes)
                    {
                        __instance.SetFloatValue(attr, value);
                    }

                    if (playerState.Hp <= -80000)
                    {
                        Logging.LogWarning("Would set HP to {HP} but will not (OOB fall damage)", playerState.Hp);
                        return;
                    }

                    if (playerState.Hp.Equals(__instance.GetFloatValue(EBGUAttrFloat.Hp), Constants.FloatComparisonTolerance))
                    {
                        return; // do not reapply the same value
                    }

                    Logging.LogTrace("(remote) Hp change from {From} to {To}", __instance.GetFloatValue(EBGUAttrFloat.Hp), playerState.Hp);
                    var set = __instance.SetFloatValue(EBGUAttrFloat.Hp, playerState.Hp);

                    if (!set.Equals(playerState.Hp, Constants.FloatComparisonTolerance))
                    {
                        Logging.LogWarning("Attempted to set player {PlayerName} HP to {DesiredHp}, instead set to {SetHp}", playerState.NickName, playerState.Hp, set);
                    }

                    if (playerState.IsDead)
                    {
                        var events = BUS_EventCollectionCS.Get(__instance.Owner);

                        if (events == null)
                        {
                            Logging.LogError("events are null");
                            return;
                        }

                        Logging.LogDebug("Applying unit dead for player {PlayerId}", playerState.PlayerId);
                        GameLoopPatch.QueueOnGameThread(() => { events.Evt_UnitDead!.Invoke(__instance.Owner, EDeadReason.SkillDamage); }, "Evt_UnitDead");
                    }
                }
                else
                {
                    var entity = DI.Instance.PawnRegistry.GetMonsterByActor(__instance.Owner as BGUCharacterCS);
                    if (!entity.HasValue)
                        return;

                    if (!entity.Value.GetComponent<LocalTamerComponent>().IsTamerSynced)
                    {
                        Logging.LogDebug("Monster {Name} is not synced, skipping HP update", __instance.Owner.GetName());
                        return;
                    }

                    var hpComp = entity.Value.GetComponent<HpComponent>();

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
        }
    }


    [HarmonyPatch(typeof(BUS_AttrComp), "SetFloatValue")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public static class PatchHp
    {
        public static bool Prefix(EBGUAttrFloat AttrID)
        {
            if (!DI.Instance.RelayClient.InRoom)
                return true;

            return AttrID != EBGUAttrFloat.Hp || DI.Instance.RelayClient.IsMasterClient;
        }

        public static void Postfix(BUS_AttrComp __instance, EBGUAttrFloat AttrID)
        {
            if (!DI.Instance.RelayClient.InRoom)
                return;

            var players = DI.Instance.Players;
            var owner = __instance.GetOwner();

            if (owner.IsNullOrDestroyed())
            {
                Logging.LogError("Owner is null or destroyed");
                return;
            }

            var result = Traverse.Create(__instance).Field<BUC_AttrContainer>("AttrContainer").Value.GetFloatValue(AttrID);

            if (AttrID == EBGUAttrFloat.Hp)
            {
                // I am a server
                if (DI.Instance.RelayClient.IsMasterClient)
                {
                    // I was damaged, set my Hp
                    if (owner == players.LocalPlayerState.Pawn)
                    {
                        if (!players.LocalPlayerState.Hp.Equals(result, Constants.FloatComparisonTolerance))
                        {
                            players.LocalPlayerState.Hp = result;
                            DI.Instance.PlayerProperty.CachePlayerProperty(nameof(PlayerState.Hp), result);
                        }

                        return;
                    }

                    // remote player was damaged, set his properties
                    var remotePlayer = players.GetPlayerByActor(owner);
                    if (remotePlayer != null)
                    {
                        if (!remotePlayer.Hp.Equals(result, Constants.FloatComparisonTolerance))
                        {
                            remotePlayer.Hp = result;
                            DI.Instance.PlayerProperty.SetRemotePlayerProperty(remotePlayer.PlayerId, nameof(PlayerState.Hp), result);
                        }

                        return;
                    }

                    // monster was damaged
                    var entity = DI.Instance.PawnRegistry.GetMonsterByActor(owner as BGUCharacterCS);
                    if (!entity.HasValue || !entity.Value.GetComponent<LocalTamerComponent>().IsTamerSynced)
                    {
                        Logging.LogDebug("Monster {Name} is not synced, skipping HP update", owner.GetName());
                        return;
                    }

                    ref var hpComp = ref entity.Value.GetComponent<HpComponent>();

                    hpComp.HpMaxBase = Traverse.Create(__instance).Field<BUC_AttrContainer>("AttrContainer").Value.GetFloatValue(EBGUAttrFloat.HpMaxBase);
                    hpComp.Hp = result;
                }

                // I am a client
                return;
            }

            // only sync attributes that influence combat and are client-authoritative
            if (Constants.SyncedAttributes.Contains(AttrID) && owner == players.LocalPlayerState.Pawn)
            {
                if (players.LocalPlayerState.Attributes.TryGetValue(AttrID, out var existing)
                    && existing.Equals(result, Constants.FloatComparisonTolerance))
                {
                    return;
                }

                players.LocalPlayerState.Attributes[AttrID] = result;
                DI.Instance.PlayerProperty.CachePlayerAttribute(AttrID, result);

                // some attributes may influence other attributes
                var calc = AttrMgr<EBGUAttrFloat, float>.getInstance().GetCalc(AttrID, out var valid);
                if (valid)
                {
                    Logging.LogTrace("Also updating {DependentAttr} because of {Attr}", calc.finalVal, AttrID);

                    var finalVal = Traverse.Create(__instance).Field<BUC_AttrContainer>("AttrContainer").Value.GetFloatValue(calc.finalVal);
                    players.LocalPlayerState.Attributes[calc.finalVal] = finalVal;
                    DI.Instance.PlayerProperty.CachePlayerAttribute(calc.finalVal, finalVal);
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
            if (!DI.Instance.RelayClient.InRoom)
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

            var players = DI.Instance.Players;

            if (character == players.LocalPlayerState.Pawn)
            {
                var localState = players.LocalPlayerState;

                if (localState.IsWaitingForSequence)
                {
                    // update local player location
                    RestrictPlayerLocation(localState, __instance);
                }

                if (localState.IsFlying != __instance.IsFlying)
                {
                    players.LocalPlayerState.IsFlying = __instance.IsFlying;
                    DI.Instance.PlayerProperty.CachePlayerProperty(nameof(PlayerState.IsFlying), players.LocalPlayerState.IsFlying);
                }

                if (localState.IsFalling != __instance.IsFalling)
                {
                    players.LocalPlayerState.IsFalling = __instance.IsFalling;
                    DI.Instance.PlayerProperty.CachePlayerProperty(nameof(PlayerState.IsFalling), players.LocalPlayerState.IsFalling);
                }

                if (localState.IsLandingMove != __instance.IsLandingMove)
                {
                    players.LocalPlayerState.IsLandingMove = __instance.IsLandingMove;
                    DI.Instance.PlayerProperty.CachePlayerProperty(nameof(PlayerState.IsLandingMove), players.LocalPlayerState.IsLandingMove);
                }

                if (!players.LocalPlayerState.Velocity.Equals(__instance.Velocity, Constants.FloatComparisonTolerance))
                {
                    players.LocalPlayerState.Velocity = __instance.Velocity;
                    DI.Instance.PlayerProperty.CachePlayerProperty(nameof(PlayerState.Velocity), players.LocalPlayerState.Velocity);
                }

                if (!players.LocalPlayerState.MoveAcceleration.Equals(__instance.MoveAcceleration, Constants.FloatComparisonTolerance))
                {
                    players.LocalPlayerState.MoveAcceleration = __instance.MoveAcceleration;
                    DI.Instance.PlayerProperty.CachePlayerProperty(nameof(PlayerState.MoveAcceleration), players.LocalPlayerState.MoveAcceleration);
                }

                if (!players.LocalPlayerState.Location.Equals(__instance.ActorLocation, Constants.FloatComparisonTolerance))
                {
                    players.LocalPlayerState.Location = __instance.ActorLocation;
                    DI.Instance.PlayerProperty.CachePlayerProperty(nameof(PlayerState.Location), players.LocalPlayerState.Location);
                }

                if (!players.LocalPlayerState.Rotation.Equals(__instance.ActorRotation, Constants.FloatComparisonTolerance))
                {
                    players.LocalPlayerState.Rotation = __instance.ActorRotation;
                    DI.Instance.PlayerProperty.CachePlayerProperty(nameof(PlayerState.Rotation), players.LocalPlayerState.Rotation);
                }

                DI.Instance.Synchronizer.UpdatePlayer(localState, DeltaTime);
            }
            else
            {
                var playerState = players.GetPlayerByActor(character);

                if (playerState != null)
                {
                    var events = BUS_EventCollectionCS.Get(character);

                    __instance.IsFlying = playerState.IsFlying;
                    __instance.IsFalling = playerState.IsFalling;
                    __instance.IsLandingMove = playerState.IsLandingMove;
                    __instance.Velocity = playerState.Velocity;

                    if (__instance.Velocity.Equals(FVector.ZeroVector, Constants.FloatComparisonTolerance))
                    {
                        __instance.Velocity = FVector.ZeroVector;
                        playerState.Velocity = FVector.ZeroVector;

                        // without these 5 lines the character will not jump
                        __instance.MovementComp.Velocity = new FVector(0, 0, __instance.MovementComp.Velocity.Z);
                        __instance.RealWorldVelocity = new FVector(0, 0, __instance.RealWorldVelocity.Z);
                        __instance.MovementComp.MovementMode = EMovementMode.MOVE_None;

                        events.Evt_StopCurrentMove.Invoke();
                        events.Evt_MovementForceStop.Invoke();
                    }

                    __instance.MoveAcceleration = playerState.MoveAcceleration;
                    if (__instance.MoveAcceleration.Equals(FVector.ZeroVector, Constants.FloatComparisonTolerance))
                    {
                        __instance.MoveAcceleration = FVector.ZeroVector;
                        playerState.MoveAcceleration = FVector.ZeroVector;
                    }

                    if (!playerState.Location.Equals(__instance.ActorLocation, Constants.FloatComparisonTolerance))
                    {
                        events.Evt_InterpolationMove.Invoke(playerState.Location, playerState.Rotation, Constants.ToleratedLatencyMs / 1000f, true, false, false, true);
                    }

                    DI.Instance.Synchronizer.UpdatePlayer(playerState, DeltaTime);
                }
                else
                {
                    // maybe it's a monster
                    var entity = DI.Instance.PawnRegistry.GetMonsterByActor(character);

                    if (entity.HasValue)
                    {
                        if (!entity.Value.GetComponent<LocalTamerComponent>().IsTamerSynced)
                        {
                            return;
                        }

                        if (DI.Instance.RelayClient.IsMasterClient)
                        {
                            ref var anim = ref entity.Value.GetComponent<AnimationComponent>();
                            anim.Velocity = __instance.Velocity.ToVector3();
                            anim.MoveAcceleration = __instance.MoveAcceleration.ToVector3();

                            ref var trans = ref entity.Value.GetComponent<TranslationComponent>();
                            trans.Position = __instance.ActorLocation.ToVector3();
                            trans.Rotation = __instance.ActorRotation.ToVector3();
                        }
                        else
                        {
                            var anim = entity.Value.GetComponent<AnimationComponent>();
                            var tamer = entity.Value.GetComponent<LocalTamerComponent>();

                            __instance.Velocity = anim.Velocity.ToFVector();
                            __instance.MoveAcceleration = anim.MoveAcceleration.ToFVector();
                            __instance.MovementComp.Velocity = anim.Velocity.ToFVector();

                            var events = BUS_EventCollectionCS.Get(tamer.Pawn);

                            var trans = entity.Value.GetComponent<TranslationComponent>();
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

        private static void RestrictPlayerLocation(PlayerState localState, BUC_ABPCharacterData characterData)
        {
            var distanceSq = localState.SequenceLocation.Vector_DistanceSquared(characterData.ActorLocation);
            if (distanceSq > Constants.RestrictedMovementRadiusSquare)
            {
                characterData.ActorLocation = localState.SequenceLocation + Constants.RestrictedMovementRadius * (characterData.ActorLocation - localState.SequenceLocation).GetSafeNormal(); // cast from above
                localState.Pawn?.SetActorLocation(characterData.ActorLocation, false, out _, true);
            }
        }
    }

    [HarmonyPatch(typeof(BGU_UnrealWorldUtil), "DestroyActor")]
    [HarmonyPatchCategory(Constants.PvpPatches)]
    public class PatchDestroyActor
    {
        public static void Postfix(AActor Actor)
        {
            if (!DI.Instance.RelayClient.InRoom)
                return;

            if (Actor is BGUCharacterCS character)
            {
                var entity = DI.Instance.PawnRegistry.GetMonsterByActor(character);
                if (entity.HasValue)
                {
                    Logging.LogWarning("DestroyActor called for not cleaned up monster: {Name}", Actor.GetFullName());

                    var netId = entity.Value.GetComponent<NetworkIdComponent>();

                    // only clean up own monsters
                    if (netId.Creator != DI.Instance.RelayClient.PlayerId)
                    {
                        Logging.LogWarning("Skipping cleanup for remote monster");
                        return;
                    }

                    Logging.LogDebug("Cleaning up monster: {Name}", Actor.GetFullName());
                    TamerUtils.CleanupMonster(entity.Value);
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
            if (!DI.Instance.RelayClient.InRoom)
                return;

            if (DI.Instance.RelayClient.IsMasterClient)
            {
                var owner = __instance.GetOwner();
                var entity = DI.Instance.PawnRegistry.GetMonsterByActor(owner);
                if (entity.HasValue)
                {
                    if (SimpleState == EBGUSimpleState.Immobilizing)
                        return;

                    var netId = entity.Value.GetComponent<NetworkIdComponent>();

                    DI.Instance.Rpc.SendUnitSimpleState(new SimpleStateData(netId, SimpleState, IsRemove));
                    Logging.LogTrace("Simple state: {State} with isRemove: {Remove} set for: {Actor}", SimpleState, IsRemove, owner.GetName());
                }
            }
        }
    }

    [HarmonyPatch(typeof(BUS_UnitStateSystem), "OnUnitStateTrigger")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchOnUnitStateTrigger
    {
        public static void Postfix(EBUStateTrigger Trigger, float Time, bool NeedForceUpdate, BUS_UnitStateSystem __instance)
        {
            if (!DI.Instance.RelayClient.InRoom)
                return;

            var players = DI.Instance.Players;
            var owner = __instance.GetOwner();
            if (DI.Instance.RelayClient.IsMasterClient)
            {
                var entity = DI.Instance.PawnRegistry.GetMonsterByActor(owner);
                if (entity.HasValue)
                {
                    if (Trigger == EBUStateTrigger.Die)
                        return;

                    var netId = entity.Value.GetComponent<NetworkIdComponent>();

                    DI.Instance.Rpc.SendUnitStateTrigger(new StateTriggerData(netId, Trigger, Time, NeedForceUpdate));
                    Logging.LogTrace("Trigger state {State} triggered for {Actor}", Trigger, owner.GetName());
                }
            }

            if (owner == players.LocalPlayerState.Pawn)
            {
                DI.Instance.Rpc.SendUnitStateTrigger(new StateTriggerData(NetworkIdComponent.FromPlayerId(players.LocalPlayerState.PlayerId), Trigger, Time, NeedForceUpdate));
                Logging.LogTrace("Trigger state {State} triggered for player {Actor}", Trigger, owner.GetName());
            }
        }
    }

    [HarmonyPatch(typeof(BUS_ABPHelperComp), "OnChangeMotionMatchingState")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchOnChangeMotionMatchingState
    {
        public static void Postfix(EState_MM MMState, BUS_ABPHelperComp __instance)
        {
            if (!DI.Instance.RelayClient.InRoom)
                return;

            if (DI.Instance.RelayClient.IsMasterClient)
            {
                var owner = __instance.GetOwner();
                var entity = DI.Instance.PawnRegistry.GetMonsterByActor(owner);

                if (!entity.HasValue)
                    return;

                var netId = entity.Value.GetComponent<NetworkIdComponent>();
                DI.Instance.Rpc.SendMotionMatchingState(new MotionMatchingStateData(netId, MMState));
            }
        }
    }
    
    [HarmonyPatch]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchBuffBegin
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method("b1.BUS_BuffComp:BuffBegin");
        }

        public static void Postfix(UActorCompBaseCS __instance, int BuffID, float Duration)
        {
            if (!DI.Instance.RelayClient.InRoom)
                return;

            if (DI.Instance.RelayClient.IsMasterClient)
            {
                var character = __instance.GetOwner();
                var entity = DI.Instance.PawnRegistry.GetMonsterByActor(character);
                if (entity != null)
                {
                    Logging.LogDebug("BuffBegin called for {Actor} with BuffID={BuffId}, Duration={Duration}", character.GetName(), BuffID, Duration);
                    var netPeer = entity.Value.GetComponent<NetworkIdComponent>();
                    // DI.Instance.Rpc.SendUnitAddBuff(new BuffAddData(netPeer, BuffID, Duration));
                }
            }
            else if (GameUtils.GetControlledPawn() == __instance.GetOwner())
            {
                DI.Instance.Rpc.SendAddBuff(new BuffAddData(BuffID, Duration));
            }
        }
    }

    [HarmonyPatch]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchBuffRemove
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method("b1.BUS_BuffComp:BuffRemove");
        }

        public static void Postfix(UActorCompBaseCS __instance, int BuffID, EBuffEffectTriggerType RemoveTriggerType, int InLayer, bool WithTriggerRemoveEffect)
        {
            if (!DI.Instance.RelayClient.InRoom)
                return;

            if (DI.Instance.RelayClient.IsMasterClient)
            {
                var character = __instance.GetOwner();
                var entity = DI.Instance.PawnRegistry.GetMonsterByActor(character);
                if (entity != null)
                {
                    Logging.LogDebug("BuffRemove called for {Actor} with BuffID={BuffId}, RemoveTriggerType={TriggerType}, InLayer={Layer}, WithTriggerRemoveEffect={WithEffect}",
                        character.GetName(), BuffID, RemoveTriggerType, InLayer, WithTriggerRemoveEffect);
                    var netPeer = entity.Value.GetComponent<NetworkIdComponent>();
                    // DI.Instance.Rpc.SendUnitRemoveBuff(new BuffRemoveData(netPeer, BuffID, RemoveTriggerType, InLayer, WithTriggerRemoveEffect));
                }
                else if (GameUtils.GetControlledPawn() == __instance.GetOwner())
                {
                    DI.Instance.Rpc.SendRemoveBuff(new BuffRemoveData(BuffID, RemoveTriggerType, InLayer, WithTriggerRemoveEffect));
                }
            }
        }
    }

    [HarmonyPatch]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchBuffRemoveImmediately
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method("b1.BUS_BuffComp:BuffRemoveImmediately");
        }

        public static void Postfix(UActorCompBaseCS __instance, int BuffID, EBuffEffectTriggerType RemoveTriggerType, bool WithTriggerRemoveEffect)
        {
            if (!DI.Instance.RelayClient.InRoom)
                return;

            if (DI.Instance.RelayClient.IsMasterClient)
            {
                var character = __instance.GetOwner();
                var entity = DI.Instance.PawnRegistry.GetMonsterByActor(character);
                if (entity != null)
                {
                    var netPeer = entity.Value.GetComponent<NetworkIdComponent>();
                    // DI.Instance.Rpc.SendUnitRemoveBuff(new BuffRemoveData(netPeer, BuffID, RemoveTriggerType, -1, WithTriggerRemoveEffect));
                }
                else if (GameUtils.GetControlledPawn() == __instance.GetOwner())
                {
                    DI.Instance.Rpc.SendRemoveBuff(new BuffRemoveData(BuffID, RemoveTriggerType, -1, WithTriggerRemoveEffect));
                }
            }
        }
    }

    [HarmonyPatch]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchBuffAllRemove
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method("b1.BUS_BuffComp:BuffAllRemove");
        }

        public static void Postfix(UActorCompBaseCS __instance, EBuffEffectTriggerType RemoveTriggerType, bool WithTriggerRemoveEffect)
        {
            if (!DI.Instance.RelayClient.InRoom)
                return;

            if (DI.Instance.RelayClient.IsMasterClient)
            {
                var character = __instance.GetOwner();
                var entity = DI.Instance.PawnRegistry.GetMonsterByActor(character);
                if (entity != null)
                {
                    var netPeer = entity.Value.GetComponent<NetworkIdComponent>();
                    // DI.Instance.Rpc.SendUnitRemoveAllBuffs(new BuffRemoveAllData(netPeer, RemoveTriggerType, WithTriggerRemoveEffect));
                }
                else if (GameUtils.GetControlledPawn() == __instance.GetOwner())
                {
                    DI.Instance.Rpc.SendRemoveAllBuffs(new BuffRemoveAllData(RemoveTriggerType, WithTriggerRemoveEffect));
                }
            }
        }
    }
}