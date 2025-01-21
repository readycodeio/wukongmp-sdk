using b1;
using BtlShare;
using CSharpModBase;
using HarmonyLib;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongCSharpMod.State;

namespace WukongCSharpMod.Patches
{
    [HarmonyPatch(typeof(BUS_ABPHelperComp), "OnTickImpl")]
    [HarmonyPatchCategory(Constants.RoomPatches)]
    public class PatchTick
    {
        public static void Postfix(float DeltaTime, bool IsThreadTick)
        {
            if (IsThreadTick)
            {
                var photon = MyMod.Instance.Photon;
                photon.SendUpdatedPlayerProperties();

                if (photon.IsMasterClient)
                {
                    photon.SendUpdatedMonsterProperties();
                }
            }
        }
    }

    [HarmonyPatch(typeof(BUC_AttrContainer), nameof(BUC_AttrContainer.OnTick))]
    [HarmonyPatchCategory(Constants.RoomPatches)]
    public static class PatchAttrs
    {
        public static void Postfix(BUC_AttrContainer __instance)
        {
            var photon = MyMod.Instance.Photon;

            if (photon.IsMasterClient)
            {
                // master client always has the latest data
                return;
            }

            if (__instance.Owner == photon.LocalPlayerState.Pawn)
            {
                // local player (client)
                if (photon.LocalPlayerState.Hp <= -80000)
                {
                    Helpers.Log($"Would set hp to {photon.LocalPlayerState.Hp}  but will not");
                    return;
                }

                __instance.SetFloatValue(EBGUAttrFloat.Hp, photon.LocalPlayerState.Hp);
            }
            else
            {
                var playerState = photon.GetByActor(__instance.Owner as BGUCharacterCS);

                // remote player
                if (playerState != null)
                {
                    if (playerState.Hp <= -80000)
                    {
                        Helpers.Log($"Would set hp to {playerState.Hp} but will not");
                        return;
                    }

                    __instance.SetFloatValue(EBGUAttrFloat.Hp, playerState.Hp);
                }
                else
                {
                    var monster = photon.GetMonsterByCharacter(__instance.Owner as BGUCharacterCS);

                    // monster
                    if (monster?.Hp != null && monster.IsSynced)
                    {
                        __instance.SetFloatValue(EBGUAttrFloat.Hp, monster.Hp.Value);

                        if (monster.Hp.Value <= 0)
                        {
                            var events = BUS_EventCollectionCS.Get(__instance.Owner);
                            Helpers.Log("Will run on game thread: unit dead");
                            Utils.TryRunOnGameThread(() =>
                            {
                                Helpers.Log("Running on game thread: unit dead");
                                events.Evt_UnitDead.Invoke(__instance.Owner, EDeadReason.SkillDamage);
                            }); // TODO: Sync other dead reasons?
                        }
                    }
                }
            }
        }
    }


    [HarmonyPatch(typeof(BUS_AttrComp), "SetFloatValue")]
    [HarmonyPatchCategory(Constants.RoomPatches)]
    public static class PatchHp
    {
        public static bool Prefix(BUS_AttrComp __instance, EBGUAttrFloat AttrID, float NewValue)
        {
            var photon = MyMod.Instance.Photon;

            if (AttrID == EBGUAttrFloat.Hp)
            {
                var owner = __instance.GetOwner();

                // I am a server
                if (photon.IsMasterClient)
                {
                    // I was damaged, set my Hp
                    if (owner == photon.LocalPlayerState.Pawn)
                    {
                        //if (!photon.LocalPlayerState.Hp.Equals(NewValue, Constants.FloatComparisonTolerance))
                        //{
                        //    photon.LocalPlayerState.Hp = NewValue;
                        //    photon.SetPlayerProperty(AttrID.ToString(), NewValue);
                        //}

                        return false;
                    }

                    // remote player was damaged, set his properties
                    var remotePlayer = MyMod.Instance.Photon.GetByActor(owner);
                    if (remotePlayer != null)
                    {
                        //if (!remotePlayer.Hp.Equals(NewValue, Constants.FloatComparisonTolerance))
                        //{
                        //    remotePlayer.Hp = NewValue;
                        //    photon.SendRemotePlayerProperty(remotePlayer.PhotonId, AttrID.ToString(), NewValue);
                        //}

                        return false;
                    }

                    // monster was damaged
                    var monster = photon.GetMonsterByCharacter(owner as BGUCharacterCS);
                    if (monster != null && monster.IsSynced)
                    {
                        if (!monster.Hp.HasValue || !monster.Hp.Value.Equals(NewValue, Constants.FloatComparisonTolerance))
                        {
                            monster.Hp = NewValue;
                            photon.SetMonsterProperty(monster.Guid, AttrID.ToString(), NewValue);
                        }

                        return true;
                    }

                    // unsynced monster or sth else
                    return true;
                }

                // I am a client
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(BUC_ABPCharacterData), nameof(BUC_ABPCharacterData.Update_GameThread))]
    [HarmonyPatchCategory(Constants.RoomPatches)]
    public class PatchCharacterAnimation
    {
        public static void Postfix(BUC_ABPCharacterData __instance, AActor Owner, IBUC_ABPHelperData HelperData, float DeltaTime)
        {
            if (__instance == null)
            {
                Helpers.LogError("__instance is null in BUC_ABPCharacterData.Update_GameThread");
                return;
            }

            if (!(Owner is BGUCharacterCS character))
                return;

            var photon = MyMod.Instance.Photon;

            if (character == photon.LocalPlayerState.Pawn)
            {
                var localState = photon.LocalPlayerState;

                if (localState.IsFlying != __instance.IsFlying)
                {
                    photon.LocalPlayerState.IsFlying = __instance.IsFlying;
                    photon.SetPlayerProperty(nameof(PlayerState.IsFlying), photon.LocalPlayerState.IsFlying);
                }

                if (localState.IsFalling != __instance.IsFalling)
                {
                    photon.LocalPlayerState.IsFalling = __instance.IsFalling;
                    photon.SetPlayerProperty(nameof(PlayerState.IsFalling), photon.LocalPlayerState.IsFalling);
                }

                if (localState.IsLandingMove != __instance.IsLandingMove)
                {
                    photon.LocalPlayerState.IsLandingMove = __instance.IsLandingMove;
                    photon.SetPlayerProperty(nameof(PlayerState.IsLandingMove), photon.LocalPlayerState.IsLandingMove);
                }

                if (!localState.Velocity.Equals(__instance.Velocity, Constants.FloatComparisonTolerance))
                {
                    photon.LocalPlayerState.Velocity = __instance.Velocity;
                    photon.SetPlayerProperty(nameof(PlayerState.Velocity), photon.LocalPlayerState.Velocity);
                }

                if (!localState.MoveAcceleration.Equals(__instance.MoveAcceleration, Constants.FloatComparisonTolerance))
                {
                    photon.LocalPlayerState.MoveAcceleration = __instance.MoveAcceleration;
                    photon.SetPlayerProperty(nameof(PlayerState.MoveAcceleration), photon.LocalPlayerState.MoveAcceleration);
                }

                if (!localState.Location.Equals(__instance.ActorLocation, Constants.FloatComparisonTolerance))
                {
                    photon.LocalPlayerState.Location = __instance.ActorLocation;
                    photon.SetPlayerProperty(nameof(PlayerState.Location), photon.LocalPlayerState.Location);
                }

                if (!localState.Rotation.Equals(__instance.ActorRotation, Constants.FloatComparisonTolerance))
                {
                    photon.LocalPlayerState.Rotation = __instance.ActorRotation;
                    photon.SetPlayerProperty(nameof(PlayerState.Rotation), photon.LocalPlayerState.Rotation);
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
                        Helpers.Log("Will run on game thread: interpolation move (character)");
                        Utils.TryRunOnGameThread(() =>
                        {
                            Helpers.Log("Running on game thread: interpolation move (character)");
                            events.Evt_InterpolationMove.Invoke(playerState.Location, playerState.Rotation, Constants.ToleratedLatencyMs / 1000f, true, false, false, true);
                        });
                    }
                }
                else
                {
                    // maybe it's a monster
                    var monsterState = photon.GetMonsterByCharacter(character);

                    if (monsterState != null && monsterState.IsSynced)
                    {
                        if (photon.IsMasterClient)
                        {
                            if (!monsterState.Velocity.Equals(__instance.Velocity, Constants.FloatComparisonTolerance))
                            {
                                monsterState.Velocity = __instance.Velocity;
                                photon.SetMonsterProperty(monsterState.Guid, nameof(MonsterState.Velocity), monsterState.Velocity);
                            }

                            if (!monsterState.MoveAcceleration.Equals(__instance.MoveAcceleration, Constants.FloatComparisonTolerance))
                            {
                                monsterState.MoveAcceleration = __instance.MoveAcceleration;
                                photon.SetMonsterProperty(monsterState.Guid, nameof(MonsterState.MoveAcceleration), monsterState.MoveAcceleration);
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
}