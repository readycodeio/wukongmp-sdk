using b1;
using BtlShare;
using HarmonyLib;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS;
using WukongMp.Api.Old;
using WukongMp.Api.Old.State;

namespace WukongMp.Api.Patches
{
    [HarmonyPatch(typeof(BUC_AttrContainer), nameof(BUC_AttrContainer.OnTick))]
    [HarmonyPatchCategory(Constants.PvpPatches)]
    public static class PvpPatchAttrs
    {
        public static void Postfix(BUC_AttrContainer __instance)
        {
            if (!DI.Instance.RoomState.InRoom)
                return;

            var players = DI.Instance.Players;

            if (__instance.Owner.IsNullOrDestroyed())
            {
                Logging.LogError("Owner is null or destroyed");
                return;
            }

            if (DI.Instance.RoomState.IsMasterClient)
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
                    players.CachePlayerProperty(nameof(PlayerState.Hp), set);
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
    [HarmonyPatchCategory(Constants.PvpPatches)]
    public static class PvpPatchHp
    {
        public static bool Prefix(EBGUAttrFloat AttrID)
        {
            if (!DI.Instance.RoomState.InRoom)
                return true;

            return AttrID != EBGUAttrFloat.Hp || DI.Instance.RoomState.IsMasterClient;
        }

        public static void Postfix(BUS_AttrComp __instance, EBGUAttrFloat AttrID)
        {
            if (!DI.Instance.RoomState.InRoom)
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
                if (DI.Instance.RoomState.IsMasterClient)
                {
                    // I was damaged, set my Hp
                    if (owner == players.LocalPlayerState.Pawn)
                    {
                        if (!players.LocalPlayerState.Hp.Equals(result, Constants.FloatComparisonTolerance))
                        {
                            players.LocalPlayerState.Hp = result;
                            players.CachePlayerProperty(nameof(PlayerState.Hp), result);
                        }

                        return;
                    }

                    // remote player was damaged, set his properties
                    var remotePlayer = DI.Instance.Players.GetPlayerByActor(owner);
                    if (remotePlayer != null)
                    {
                        if (!remotePlayer.Hp.Equals(result, Constants.FloatComparisonTolerance))
                        {
                            remotePlayer.Hp = result;
                            players.SetRemotePlayerProperty(remotePlayer.PlayerId, nameof(PlayerState.Hp), result);
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
                players.CachePlayerAttribute(AttrID, result);

                // some attributes may influence other attributes
                var calc = AttrMgr<EBGUAttrFloat, float>.getInstance().GetCalc(AttrID, out var valid);
                if (valid)
                {
                    Logging.LogTrace("Also updating {DependentAttr} because of {Attr}", calc.finalVal, AttrID);

                    var finalVal = Traverse.Create(__instance).Field<BUC_AttrContainer>("AttrContainer").Value.GetFloatValue(calc.finalVal);
                    players.LocalPlayerState.Attributes[calc.finalVal] = finalVal;
                    players.CachePlayerAttribute(calc.finalVal, finalVal);
                }
            }
        }
    }
}