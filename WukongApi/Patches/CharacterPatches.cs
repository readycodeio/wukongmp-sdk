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
                var photon = WukongMP.Instance.Photon;
                photon.SetCachedPlayerProperties();

                if (photon.IsMasterClient)
                {
                    photon.SendUpdatedMonsterProperties();
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

            var photon = WukongMP.Instance.Photon;

            if (__instance.Owner.IsNullOrDestroyed())
            {
                Logging.LogError("Owner is null or destroyed");
                return;
            }

            if (photon.IsMasterClient)
            {
                // master client always has the latest data for himself, but may need to apply it for others
                if (__instance.Owner == photon.LocalPlayerState.Pawn)
                    return;

                var playerState = photon.GetByActor(__instance.Owner);
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

            if (__instance.Owner == photon.LocalPlayerState.Pawn)
            {
                // local player (client)
                if (photon.LocalPlayerState.Hp <= -80000)
                {
                    Logging.LogWarning("Would set HP to {HP}, but will not (OOB fall damage)", photon.LocalPlayerState.Hp);
                    return;
                }

                var currentHp = __instance.GetFloatValue(EBGUAttrFloat.Hp);

                if (photon.LocalPlayerState.Hp.Equals(currentHp, Constants.FloatComparisonTolerance))
                {
                    return; // do not reapply the same value
                }

                var set = __instance.SetFloatValue(EBGUAttrFloat.Hp, photon.LocalPlayerState.Hp);

                if (!set.Equals(photon.LocalPlayerState.Hp, Constants.FloatComparisonTolerance))
                {
                    Logging.LogWarning("Attempted to set player {PlayerName} HP to {DesiredHp}, instead set to {SetHp}", photon.LocalPlayerState.NickName, photon.LocalPlayerState.Hp, set);
                    photon.CachePlayerProperty(nameof(PlayerState.Hp), set);
                }

                if (photon.LocalPlayerState.IsDead)
                {
                    var events = BUS_EventCollectionCS.Get(__instance.Owner);

                    if (events == null)
                    {
                        Logging.LogError("events are null");
                        return;
                    }

                    Logging.LogDebug("Applying unit dead for player {PlayerId}", photon.LocalPlayerState.PhotonId);

                    GameLoopPatch.QueueOnGameThread(() => { events.Evt_UnitDead!.Invoke(__instance.Owner, EDeadReason.SkillDamage); }, "Evt_UnitDead");
                }
            }
            else
            {
                var playerState = photon.GetByActor(__instance.Owner);

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

                        Logging.LogDebug("Applying unit dead for player {PlayerId}", playerState.PhotonId);
                        GameLoopPatch.QueueOnGameThread(() => { events.Evt_UnitDead!.Invoke(__instance.Owner, EDeadReason.SkillDamage); }, "Evt_UnitDead");
                    }
                }
                else
                {
                    var monster = photon.GetMonsterByCharacter(__instance.Owner as BGUCharacterCS);

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

                                // remove from collection
                                photon.RemoveMonster(monster.Guid);
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

            return AttrID != EBGUAttrFloat.Hp || WukongMP.Instance.Photon.IsMasterClient;
        }

        public static void Postfix(BUS_AttrComp __instance, EBGUAttrFloat AttrID)
        {
            if (!WukongMP.Instance.ShouldRunConnectedPatches())
                return;

            var photon = WukongMP.Instance.Photon;
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
                if (photon.IsMasterClient)
                {
                    // I was damaged, set my Hp
                    if (owner == photon.LocalPlayerState.Pawn)
                    {
                        if (!photon.LocalPlayerState.Hp.Equals(result, Constants.FloatComparisonTolerance))
                        {
                            photon.LocalPlayerState.Hp = result;
                            photon.CachePlayerProperty(nameof(PlayerState.Hp), result);
                        }

                        return;
                    }

                    // remote player was damaged, set his properties
                    var remotePlayer = WukongMP.Instance.Photon.GetByActor(owner);
                    if (remotePlayer != null)
                    {
                        if (!remotePlayer.Hp.Equals(result, Constants.FloatComparisonTolerance))
                        {
                            remotePlayer.Hp = result;
                            photon.SetRemotePlayerProperty(remotePlayer.PhotonId, nameof(PlayerState.Hp), result);
                        }

                        return;
                    }

                    // monster was damaged
                    var monster = photon.GetMonsterByCharacter(owner as BGUCharacterCS);
                    if (monster is { IsSynced: true })
                    {
                        if (!monster.Hp.Equals(result, Constants.FloatComparisonTolerance))
                        {
                            monster.Hp = result;
                            photon.CacheMonsterProperty(monster.Guid, AttrID.ToString(), result);

                            if (result <= 0)
                            {
                                // remove dead monster from sync
                                photon.RemoveMonster(monster.Guid);
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
            if (Constants.SyncedAttributes.Contains(AttrID) && owner == photon.LocalPlayerState.Pawn)
            {
                if (photon.LocalPlayerState.Attributes.TryGetValue(AttrID, out var existing)
                    && existing.Equals(result, Constants.FloatComparisonTolerance))
                {
                    return;
                }

                photon.LocalPlayerState.Attributes[AttrID] = result;
                photon.CachePlayerAttribute(AttrID, result);

                // some attributes may influence other attributes
                var calc = AttrMgr<EBGUAttrFloat, float>.getInstance().GetCalc(AttrID, out var valid);
                if (valid)
                {
                    Logging.LogTrace("Also updating {DependentAttr} because of {Attr}", calc.finalVal, AttrID);

                    var finalVal = Traverse.Create(__instance).Field<BUC_AttrContainer>("AttrContainer").Value.GetFloatValue(calc.finalVal);
                    photon.LocalPlayerState.Attributes[calc.finalVal] = finalVal;
                    photon.CachePlayerAttribute(calc.finalVal, finalVal);
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

            var photon = WukongMP.Instance.Photon;

            if (character == photon.LocalPlayerState.Pawn)
            {
                var localState = photon.LocalPlayerState;

                if (localState.IsFlying != __instance.IsFlying)
                {
                    photon.LocalPlayerState.IsFlying = __instance.IsFlying;
                    photon.CachePlayerProperty(nameof(PlayerState.IsFlying), photon.LocalPlayerState.IsFlying);
                }

                if (localState.IsFalling != __instance.IsFalling)
                {
                    photon.LocalPlayerState.IsFalling = __instance.IsFalling;
                    photon.CachePlayerProperty(nameof(PlayerState.IsFalling), photon.LocalPlayerState.IsFalling);
                }

                if (localState.IsLandingMove != __instance.IsLandingMove)
                {
                    photon.LocalPlayerState.IsLandingMove = __instance.IsLandingMove;
                    photon.CachePlayerProperty(nameof(PlayerState.IsLandingMove), photon.LocalPlayerState.IsLandingMove);
                }

                if (!localState.Velocity.Equals(__instance.Velocity, Constants.FloatComparisonTolerance))
                {
                    photon.LocalPlayerState.Velocity = __instance.Velocity;
                    photon.CachePlayerProperty(nameof(PlayerState.Velocity), photon.LocalPlayerState.Velocity);
                }

                if (!localState.MoveAcceleration.Equals(__instance.MoveAcceleration, Constants.FloatComparisonTolerance))
                {
                    photon.LocalPlayerState.MoveAcceleration = __instance.MoveAcceleration;
                    photon.CachePlayerProperty(nameof(PlayerState.MoveAcceleration), photon.LocalPlayerState.MoveAcceleration);
                }

                if (!localState.Location.Equals(__instance.ActorLocation, Constants.FloatComparisonTolerance))
                {
                    photon.LocalPlayerState.Location = __instance.ActorLocation;
                    photon.CachePlayerProperty(nameof(PlayerState.Location), photon.LocalPlayerState.Location);
                }

                if (!localState.Rotation.Equals(__instance.ActorRotation, Constants.FloatComparisonTolerance))
                {
                    photon.LocalPlayerState.Rotation = __instance.ActorRotation;
                    photon.CachePlayerProperty(nameof(PlayerState.Rotation), photon.LocalPlayerState.Rotation);
                }
            }
            else
            {
                var playerState = photon.GetByActor(character);

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

                    playerState.UpdateMarkerPosition();
                }
                else
                {
                    // maybe it's a monster
                    var monsterState = photon.GetMonsterByCharacter(character);

                    if (monsterState is { IsSynced: true })
                    {
                        if (photon.IsMasterClient)
                        {
                            if (!monsterState.Velocity.Equals(__instance.Velocity, Constants.FloatComparisonTolerance))
                            {
                                monsterState.Velocity = __instance.Velocity;
                                photon.CacheMonsterProperty(monsterState.Guid, nameof(MonsterState.Velocity), monsterState.Velocity);
                            }

                            if (!monsterState.MoveAcceleration.Equals(__instance.MoveAcceleration, Constants.FloatComparisonTolerance))
                            {
                                monsterState.MoveAcceleration = __instance.MoveAcceleration;
                                photon.CacheMonsterProperty(monsterState.Guid, nameof(MonsterState.MoveAcceleration), monsterState.MoveAcceleration);
                            }
                        }
                        else
                        {
                            __instance.Velocity = monsterState.Velocity;
                            __instance.MoveAcceleration = monsterState.MoveAcceleration;
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

            var photon = WukongMP.Instance.Photon;
            if (Actor is BGUCharacterCS character)
            {
                var monsterState = photon.GetMonsterByCharacter(character);
                if (monsterState != null)
                {
                    Logging.LogWarning("DestroyActor called for {Name}", Actor.GetFullName());
                    photon.RemoveMonster(monsterState.Guid);
                }
            }
        }
    }
}