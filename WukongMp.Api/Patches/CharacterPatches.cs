using b1;
using BtlShare;
using HarmonyLib;
using ReadyM.Relay.Common.ECS;
using ReadyM.Relay.Common.Wukong.Components;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.ECS;
using WukongMp.Api.State;

namespace WukongMp.Api.Patches
{
    [HarmonyPatch(typeof(BUC_AttrContainer), nameof(BUC_AttrContainer.OnTick))]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public static class PatchAttrs
    {
        public static void Postfix(BUC_AttrContainer __instance)
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return;

            var client = WukongMP.Instance.Client;

            if (__instance.Owner.IsNullOrDestroyed())
            {
                Logging.LogError("Owner is null or destroyed");
                return;
            }

            if (client.IsMasterClient)
            {
                // master client always has the latest data for himself, but may need to apply it for others
                if (__instance.Owner == client.LocalPlayerState.Pawn)
                    return;

                var playerState = client.GetPlayerByActor(__instance.Owner);
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

            if (__instance.Owner == client.LocalPlayerState.Pawn)
            {
                // local player (client)
                if (client.LocalPlayerState.Hp <= -80000)
                {
                    Logging.LogWarning("Would set HP to {HP}, but will not (OOB fall damage)", client.LocalPlayerState.Hp);
                    return;
                }

                var currentHp = __instance.GetFloatValue(EBGUAttrFloat.Hp);

                if (client.LocalPlayerState.Hp.Equals(currentHp, Constants.FloatComparisonTolerance))
                {
                    return; // do not reapply the same value
                }

                var set = __instance.SetFloatValue(EBGUAttrFloat.Hp, client.LocalPlayerState.Hp);

                if (!set.Equals(client.LocalPlayerState.Hp, Constants.FloatComparisonTolerance))
                {
                    Logging.LogWarning("Attempted to set player {PlayerName} HP to {DesiredHp}, instead set to {SetHp}", client.LocalPlayerState.NickName, client.LocalPlayerState.Hp, set);
                    client.CachePlayerProperty(nameof(PlayerState.Hp), set);
                }

                if (client.LocalPlayerState.IsDead)
                {
                    var events = BUS_EventCollectionCS.Get(__instance.Owner);

                    if (events == null)
                    {
                        Logging.LogError("events are null");
                        return;
                    }

                    Logging.LogDebug("Applying unit dead for player {PlayerId}", client.LocalPlayerState.PeerId);

                    GameLoopPatch.QueueOnGameThread(() => { events.Evt_UnitDead!.Invoke(__instance.Owner, EDeadReason.SkillDamage); }, "Evt_UnitDead");
                }
            }
            else
            {
                var playerState = client.GetPlayerByActor(__instance.Owner);

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

                        Logging.LogDebug("Applying unit dead for player {PlayerId}", playerState.PeerId);
                        GameLoopPatch.QueueOnGameThread(() => { events.Evt_UnitDead!.Invoke(__instance.Owner, EDeadReason.SkillDamage); }, "Evt_UnitDead");
                    }
                }
                else
                {
                    var entity = WukongApi.Instance.GetMonsterByActor(__instance.Owner as BGUCharacterCS);
                    if (!entity.HasValue)
                        return;

                    if (!entity.Value.GetComponent<LocalTamerComponent>().IsSynced)
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
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return true;

            return AttrID != EBGUAttrFloat.Hp || WukongMP.Instance.Client.IsMasterClient;
        }

        public static void Postfix(BUS_AttrComp __instance, EBGUAttrFloat AttrID)
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return;

            var client = WukongMP.Instance.Client;
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
                if (client.IsMasterClient)
                {
                    // I was damaged, set my Hp
                    if (owner == client.LocalPlayerState.Pawn)
                    {
                        if (!client.LocalPlayerState.Hp.Equals(result, Constants.FloatComparisonTolerance))
                        {
                            client.LocalPlayerState.Hp = result;
                            client.CachePlayerProperty(nameof(PlayerState.Hp), result);
                        }

                        return;
                    }

                    // remote player was damaged, set his properties
                    var remotePlayer = WukongMP.Instance.Client.GetPlayerByActor(owner);
                    if (remotePlayer != null)
                    {
                        if (!remotePlayer.Hp.Equals(result, Constants.FloatComparisonTolerance))
                        {
                            remotePlayer.Hp = result;
                            client.SetRemotePlayerProperty(remotePlayer.PeerId, nameof(PlayerState.Hp), result);
                        }

                        return;
                    }

                    // monster was damaged
                    var entity = WukongApi.Instance.GetMonsterByActor(owner as BGUCharacterCS);
                    if (!entity.HasValue || !entity.Value.GetComponent<LocalTamerComponent>().IsSynced)
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
            if (Constants.SyncedAttributes.Contains(AttrID) && owner == client.LocalPlayerState.Pawn)
            {
                if (client.LocalPlayerState.Attributes.TryGetValue(AttrID, out var existing)
                    && existing.Equals(result, Constants.FloatComparisonTolerance))
                {
                    return;
                }

                client.LocalPlayerState.Attributes[AttrID] = result;
                client.CachePlayerAttribute(AttrID, result);

                // some attributes may influence other attributes
                var calc = AttrMgr<EBGUAttrFloat, float>.getInstance().GetCalc(AttrID, out var valid);
                if (valid)
                {
                    Logging.LogTrace("Also updating {DependentAttr} because of {Attr}", calc.finalVal, AttrID);

                    var finalVal = Traverse.Create(__instance).Field<BUC_AttrContainer>("AttrContainer").Value.GetFloatValue(calc.finalVal);
                    client.LocalPlayerState.Attributes[calc.finalVal] = finalVal;
                    client.CachePlayerAttribute(calc.finalVal, finalVal);
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
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
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

            var client = WukongMP.Instance.Client;

            if (character == client.LocalPlayerState.Pawn)
            {
                var localState = client.LocalPlayerState;

                if (localState.IsFlying != __instance.IsFlying)
                {
                    client.LocalPlayerState.IsFlying = __instance.IsFlying;
                    client.CachePlayerProperty(nameof(PlayerState.IsFlying), client.LocalPlayerState.IsFlying);
                }

                if (localState.IsFalling != __instance.IsFalling)
                {
                    client.LocalPlayerState.IsFalling = __instance.IsFalling;
                    client.CachePlayerProperty(nameof(PlayerState.IsFalling), client.LocalPlayerState.IsFalling);
                }

                if (localState.IsLandingMove != __instance.IsLandingMove)
                {
                    client.LocalPlayerState.IsLandingMove = __instance.IsLandingMove;
                    client.CachePlayerProperty(nameof(PlayerState.IsLandingMove), client.LocalPlayerState.IsLandingMove);
                }

                if (!client.LocalPlayerState.Velocity.Equals(__instance.Velocity, Constants.FloatComparisonTolerance))
                {
                    client.LocalPlayerState.Velocity = __instance.Velocity;
                    client.CachePlayerProperty(nameof(PlayerState.Velocity), client.LocalPlayerState.Velocity);
                }

                if (!client.LocalPlayerState.MoveAcceleration.Equals(__instance.MoveAcceleration, Constants.FloatComparisonTolerance))
                {
                    client.LocalPlayerState.MoveAcceleration = __instance.MoveAcceleration;
                    client.CachePlayerProperty(nameof(PlayerState.MoveAcceleration), client.LocalPlayerState.MoveAcceleration);
                }

                if (!client.LocalPlayerState.Location.Equals(__instance.ActorLocation, Constants.FloatComparisonTolerance))
                {
                    client.LocalPlayerState.Location = __instance.ActorLocation;
                    client.CachePlayerProperty(nameof(PlayerState.Location), client.LocalPlayerState.Location);
                }

                if (!client.LocalPlayerState.Rotation.Equals(__instance.ActorRotation, Constants.FloatComparisonTolerance))
                {
                    client.LocalPlayerState.Rotation = __instance.ActorRotation;
                    client.CachePlayerProperty(nameof(PlayerState.Rotation), client.LocalPlayerState.Rotation);
                }

                WukongMP.Instance.UpdatePlayer(localState, DeltaTime);
            }
            else
            {
                var playerState = client.GetPlayerByActor(character);

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

                    WukongMP.Instance.UpdatePlayer(playerState, DeltaTime);
                }
                else
                {
                    // maybe it's a monster
                    var entity = WukongApi.Instance.GetMonsterByActor(character);

                    if (entity.HasValue)
                    {
                        if (!entity.Value.GetComponent<LocalTamerComponent>().IsSynced)
                        {
                            return;
                        }

                        if (client.IsMasterClient)
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
    }

    [HarmonyPatch(typeof(BGU_UnrealWorldUtil), "DestroyActor")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchDestroyActor
    {
        public static void Postfix(AActor Actor)
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return;

            if (Actor is BGUCharacterCS character)
            {
                var entity = WukongApi.Instance.GetMonsterByActor(character);
                if (entity.HasValue)
                {
                    Logging.LogWarning("DestroyActor called for not cleaned up monster: {Name}", Actor.GetFullName());
                    WukongMP.Instance.CleanupMonster(entity.Value);
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
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return;

            var client = WukongMP.Instance.Client;
            if (client.IsMasterClient)
            {
                var owner = __instance.GetOwner();
                var entity = WukongApi.Instance.GetMonsterByActor(owner);
                if (entity.HasValue)
                {
                    if (SimpleState == EBGUSimpleState.Immobilizing)
                        return;

                    var peerId = entity.Value.GetComponent<NetworkIdComponent>();

                    client.SendUnitSimpleState(peerId, SimpleState, IsRemove);
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
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return;

            var client = WukongMP.Instance.Client;
            var owner = __instance.GetOwner();
            if (client.IsMasterClient)
            {
                var entity = WukongApi.Instance.GetMonsterByActor(owner);
                if (entity.HasValue)
                {
                    if (Trigger == EBUStateTrigger.Die)
                        return;

                    var peerId = entity.Value.GetComponent<NetworkIdComponent>();

                    client.SendUnitStateTrigger(peerId, Trigger, Time, NeedForceUpdate);
                    Logging.LogTrace("Trigger state {State} triggered for {Actor}", Trigger, owner.GetName());
                }
            }

            if (owner == client.LocalPlayerState.Pawn)
            {
                client.SendUnitStateTrigger(NetworkIdComponent.FromPlayerPeerId(client.LocalPlayerState.PeerId), Trigger, Time, NeedForceUpdate);
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
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return;

            var client = WukongMP.Instance.Client;
            if (client.IsMasterClient)
            {
                var owner = __instance.GetOwner();
                var entity = WukongApi.Instance.GetMonsterByActor(owner);

                if (!entity.HasValue)
                    return;

                var monsterAnim = entity.Value.GetComponent<NetworkIdComponent>();
                client.SendMotionMatchingState(monsterAnim, MMState);
            }
        }
    }
}