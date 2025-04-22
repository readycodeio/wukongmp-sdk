using b1;
using BtlShare;
using HarmonyLib;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongApi.State;

namespace WukongApi.Patches
{
    [HarmonyPatch(typeof(BUS_ABPHelperComp), "OnTickImpl")]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchTick
    {
        public static void Postfix(float DeltaTime, bool IsThreadTick)
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return;

            if (IsThreadTick)
            {
                var client = WukongMP.Instance.Client;
                client.SetCachedPlayerProperties();

                if (client.IsMasterClient)
                {
                    client.SendUpdatedMonsterProperties();
                }
            }
        }
    }

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

                    Logging.LogDebug("(remote) Hp change from {From} to {To}", __instance.GetFloatValue(EBGUAttrFloat.Hp), playerState.Hp);
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
                    var monster = client.GetMonsterByCharacter(__instance.Owner as BGUCharacterCS);

                    // monster
                    if (monster is { IsSynced: true })
                    {
                        if (monster.Hp.Equals(__instance.GetFloatValue(EBGUAttrFloat.Hp), Constants.FloatComparisonTolerance))
                        {
                            return; // do not reapply the same value
                        }

                        __instance.SetFloatValue(EBGUAttrFloat.Hp, monster.Hp);

                        if (monster.Hp <= 0)
                        {
                            var events = BUS_EventCollectionCS.Get(__instance.Owner);
                            GameLoopPatch.QueueOnGameThread(() =>
                            {
                                events.Evt_UnitDead.Invoke(__instance.Owner, EDeadReason.SkillDamage);
                                BGU_UnrealWorldUtil.DestroyActor(monster.MarkerActor);
                            }, "Evt_UnitDead"); // TODO: Sync other dead reasons?
                        }
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
                    var monster = client.GetMonsterByCharacter(owner as BGUCharacterCS);
                    if (monster is { IsSynced: true })
                    {
                        if (!monster.Hp.Equals(result, Constants.FloatComparisonTolerance))
                        {
                            monster.Hp = result;
                            client.CacheMonsterProperty(monster.Guid, AttrID.ToString(), result);

                            if (result <= 0)
                            {
                                // remove dead monster from sync
                                var events = BUS_EventCollectionCS.Get(monster.Pawn);
                                GameLoopPatch.QueueOnGameThread(() =>
                                {
                                    events.Evt_UnitDead.Invoke(monster.Pawn, EDeadReason.SkillDamage);
                                    BGU_UnrealWorldUtil.DestroyActor(monster.MarkerActor);
                                }, "Evt_UnitDead");
                            }
                        }

                        return;
                    }

                    // unsynced monster or sth else
                    return;
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

    [HarmonyPatch(typeof(BUC_ABPCharacterData), nameof(BUC_ABPCharacterData.Update_GameThread))]
    [HarmonyPatchCategory(Constants.ConnectedPatches)]
    public class PatchCharacterAnimation
    {
        public static void Postfix(BUC_ABPCharacterData __instance, AActor Owner, IBUC_ABPHelperData HelperData, float DeltaTime)
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

                if (!localState.Velocity.Equals(__instance.Velocity, Constants.FloatComparisonTolerance))
                {
                    client.LocalPlayerState.Velocity = __instance.Velocity;
                    client.CachePlayerProperty(nameof(PlayerState.Velocity), client.LocalPlayerState.Velocity);
                }

                if (!localState.MoveAcceleration.Equals(__instance.MoveAcceleration, Constants.FloatComparisonTolerance))
                {
                    client.LocalPlayerState.MoveAcceleration = __instance.MoveAcceleration;
                    client.CachePlayerProperty(nameof(PlayerState.MoveAcceleration), client.LocalPlayerState.MoveAcceleration);
                }

                if (!localState.Location.Equals(__instance.ActorLocation, Constants.FloatComparisonTolerance))
                {
                    client.LocalPlayerState.Location = __instance.ActorLocation;
                    client.CachePlayerProperty(nameof(PlayerState.Location), client.LocalPlayerState.Location);
                }

                if (!localState.Rotation.Equals(__instance.ActorRotation, Constants.FloatComparisonTolerance))
                {
                    client.LocalPlayerState.Rotation = __instance.ActorRotation;
                    client.CachePlayerProperty(nameof(PlayerState.Rotation), client.LocalPlayerState.Rotation);
                }

                WukongMP.Instance.UpdatePlayer(localState);
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

                    WukongMP.Instance.UpdatePlayer(playerState);
                }
                else
                {
                    // maybe it's a monster
                    var monsterState = client.GetMonsterByCharacter(character);

                    if (monsterState is { IsSynced: true })
                    {
                        if (client.IsMasterClient)
                        {
                            if (!monsterState.Velocity.Equals(__instance.Velocity, Constants.FloatComparisonTolerance))
                            {
                                monsterState.Velocity = __instance.Velocity;
                                client.CacheMonsterProperty(monsterState.Guid, nameof(MonsterState.Velocity), monsterState.Velocity);
                            }

                            if (!monsterState.MoveAcceleration.Equals(__instance.MoveAcceleration, Constants.FloatComparisonTolerance))
                            {
                                monsterState.MoveAcceleration = __instance.MoveAcceleration;
                                client.CacheMonsterProperty(monsterState.Guid, nameof(MonsterState.MoveAcceleration), monsterState.MoveAcceleration);
                            }

                            if (!monsterState.Location.Equals(__instance.ActorLocation, Constants.FloatComparisonTolerance))
                            {
                                monsterState.Location = __instance.ActorLocation;
                                client.CacheMonsterProperty(monsterState.Guid, nameof(MonsterState.Location), monsterState.Location);
                            }

                            if (!monsterState.Rotation.Equals(__instance.ActorRotation, Constants.FloatComparisonTolerance))
                            {
                                monsterState.Rotation = __instance.ActorRotation;
                                client.CacheMonsterProperty(monsterState.Guid, nameof(MonsterState.Rotation), monsterState.Rotation);
                            }

                            if (!monsterState.MaxSpeed.Equals(__instance.MaxSpeed, Constants.FloatComparisonTolerance))
                            {
                                monsterState.MaxSpeed = __instance.MaxSpeed;
                                client.CacheMonsterProperty(monsterState.Guid, nameof(MonsterState.MaxSpeed), monsterState.MaxSpeed);
                            }
                        }
                        else
                        {
                            __instance.MaxSpeed = monsterState.MaxSpeed;
                            __instance.Velocity = monsterState.Velocity;
                            __instance.MoveAcceleration = monsterState.MoveAcceleration;
                            __instance.MovementComp.Velocity = monsterState.Velocity;

                            var events = BUS_EventCollectionCS.Get(monsterState.Pawn);

                            if (!monsterState.Location.Equals(__instance.ActorLocation, Constants.FloatComparisonTolerance))
                            {
                                events.Evt_InterpolationMove.Invoke(monsterState.Location, monsterState.Rotation, Constants.ToleratedLatencyMs / 1000f, true, false, false, true);
                            }
                        }

                        WukongMP.Instance.UpdateMonster(monsterState);
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

            var client = WukongMP.Instance.Client;
            if (Actor is BGUCharacterCS character)
            {
                var monsterState = client.GetMonsterByCharacter(character);
                if (monsterState != null)
                {
                    Logging.LogDebug("DestroyActor called for not cleaned up monster: {Name}", Actor.GetFullName());
                    WukongMP.Instance.CleanupMonster(monsterState);
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
                var character = client.GetMonsterByActor(owner);
                if (character != null)
                {
                    if (SimpleState == EBGUSimpleState.Immobilizing)
                        return;

                    client.SendUnitSimpleState(character.PeerId, SimpleState, IsRemove);
                    Logging.LogDebug("Simple state: {State} with isRemove: {Remove} set for: {Actor}", SimpleState, IsRemove, owner.GetName());
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
                var character = client.GetMonsterByActor(owner);
                if (character != null)
                {
                    client.SendUnitStateTrigger(character.PeerId, Trigger, Time, NeedForceUpdate);
                    Logging.LogDebug("Trigger state {State} triggered for {Actor}", Trigger, owner.GetName());
                }
            }
            if (owner == client.LocalPlayerState.Pawn)
            {
                client.SendUnitStateTrigger(client.LocalPlayerState.PeerId, Trigger, Time, NeedForceUpdate);
                Logging.LogDebug("Trigger state {State} triggered for player {Actor}", Trigger, owner.GetName());
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
                var character = client.GetMonsterByActor(owner);
                if (character != null)
                {
                    client.SendMotionMatchingState(character.PeerId, MMState);
                    Logging.LogDebug("Motion matching state changed to {State} for {Actor}", MMState, owner.GetName());
                }
            }
        }
    }
}