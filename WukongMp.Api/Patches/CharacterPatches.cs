using b1;
using BtlShare;
using HarmonyLib;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.DTO;
using WukongMp.Api.ECS;
using WukongMp.Api.Old.State;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Patches
{
    [HarmonyPatch(typeof(BUC_AttrContainer), nameof(BUC_AttrContainer.OnTick))]
    [HarmonyPatchCategory(Constants.CoopPatches)]
    public static class CoopPatchAttrs
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

            if (__instance.Owner == client.LocalPlayerState.Pawn)
            {
                return; // players own their characters
            }

            var playerState = client.GetPlayerByActor(__instance.Owner);

            // remote player - sync properties and HP

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

                return;
            }

            // remote monster - sync HP

            var entity = WukongMpMod.Instance.GetMonsterByActor(__instance.Owner as BGUCharacterCS);
            if (!entity.HasValue)
                return;

            // owned, skip
            if (WukongMpMod.Instance.OwnsEntity(entity.Value))
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

    [HarmonyPatch(typeof(BUS_AttrComp), "SetFloatValue")]
    [HarmonyPatchCategory(Constants.CoopPatches)]
    public static class CoopPatchHp
    {
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
                if (owner == client.LocalPlayerState.Pawn)
                {
                    if (!client.LocalPlayerState.Hp.Equals(result, Constants.FloatComparisonTolerance))
                    {
                        client.LocalPlayerState.Hp = result;
                        client.CachePlayerProperty(nameof(PlayerState.Hp), result);
                    }
                }
                else
                {
                    var entity = WukongMpMod.Instance.GetMonsterByActor(owner as BGUCharacterCS);

                    if (!entity.HasValue)
                        return; // not found

                    if (!WukongMpMod.Instance.OwnsEntity(entity.Value))
                        return; // not owned

                    if (!entity.Value.GetComponent<LocalTamerComponent>().IsTamerSynced)
                        return; // not synced

                    ref var hpComp = ref entity.Value.GetComponent<HpComponent>();

                    hpComp.HpMaxBase = Traverse.Create(__instance).Field<BUC_AttrContainer>("AttrContainer").Value.GetFloatValue(EBGUAttrFloat.HpMaxBase);
                    hpComp.Hp = result;
                }
            }

            if (Constants.SyncedAttributes.Contains(AttrID) && owner == client.LocalPlayerState.Pawn)
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

                        if (WukongMpMod.Instance.OwnsEntity(entity.Value))
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

                    // only clean up own monsters
                    if (!WukongMpMod.Instance.OwnsEntity(entity.Value))
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

            var owner = __instance.GetOwner();
            var entity = WukongMpMod.Instance.GetMonsterByActor(owner);
            if (entity.HasValue && WukongMpMod.Instance.OwnsEntity(entity.Value))
            {
                if (SimpleState == EBGUSimpleState.Immobilizing)
                    return;

                var netId = entity.Value.GetComponent<MetadataComponent>().NetId;

                WukongMpMod.Instance.SendUnitSimpleState(new SimpleStateData(netId, SimpleState, IsRemove));
                Logging.LogTrace("Simple state: {State} with isRemove: {Remove} set for: {Actor}", SimpleState, IsRemove, owner.GetName());
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

            var entity = WukongMpMod.Instance.GetMonsterByActor(owner);
            if (entity.HasValue && WukongMpMod.Instance.OwnsEntity(entity.Value))
            {
                if (Trigger == EBUStateTrigger.Die)
                    return;

                var netId = entity.Value.GetComponent<MetadataComponent>().NetId;

                WukongMpMod.Instance.SendUnitStateTrigger(new StateTriggerData(netId, Trigger, Time, NeedForceUpdate));
                Logging.LogTrace("Trigger state {State} triggered for {Actor}", Trigger, owner.GetName());
            }


            if (owner == client.LocalPlayerState.Pawn)
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

            var client = WukongMpMod.Client;

            var owner = __instance.GetOwner();
            var entity = WukongMpMod.Instance.GetMonsterByActor(owner);

            if (!entity.HasValue || !WukongMpMod.Instance.OwnsEntity(entity.Value))
                return;

            var netId = entity.Value.GetComponent<MetadataComponent>().NetId;
            WukongMpMod.Instance.SendMotionMatchingState(new MotionMatchingStateData(netId, MMState));
        }
    }
}