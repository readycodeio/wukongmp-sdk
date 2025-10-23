using b1;
using BtlShare;
using HarmonyLib;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using UnrealEngine.Engine;
using WukongMp.Api.Compat;
using WukongMp.Api.Configuration;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Patches;

[HarmonyPatch(typeof(BUC_AttrContainer), nameof(BUC_AttrContainer.OnTick))]
[HarmonyPatchCategory(Constants.DisabledPatches)]
public static class PvpPatchAttrs
{
    public static void Postfix(BUC_AttrContainer __instance)
    {
        if (!DI.Instance.AreaState.InRoom)
            return;

        var playerState = DI.Instance.PlayerState;

        if (__instance.Owner.IsNullOrDestroyed())
        {
            Logging.LogError("Owner is null or destroyed");
            return;
        }

        if (DI.Instance.AreaState.IsMasterClient)
        {
            // master client always has the latest data for himself, but may need to apply it for others
            if (__instance.Owner == playerState.LocalMainCharacter?.GetLocalState().Pawn)
                return;

            var mainEntity = DI.Instance.PawnState.GetEntityByPlayerPawn(__instance.Owner);
            if (mainEntity != null)
            {
                ref var mainComp = ref mainEntity.Value.GetState();
                foreach (var (attr, value) in mainComp.Attributes)
                {
                    __instance.SetFloatValue((EBGUAttrFloat)attr, value);
                }
            }

            return;
        }

        // for clients, their own attributes are already set by them, and they do not care about attributes of other clients / monsters
        // because it's the master client that ultimately calculates damage in combat

        if (__instance.Owner == playerState.LocalMainCharacter?.GetLocalState().Pawn)
        {
            var mainEntity = playerState.LocalMainCharacter;
            ref var mainComp = ref mainEntity.Value.GetState();

            // local player (client)
            SyncMainCharacterHp(__instance, ref mainComp);
        }
        else
        {
            var mainEntity = DI.Instance.PawnState.GetEntityByPlayerPawn(__instance.Owner);

            // remote player
            if (mainEntity != null)
            {
                ref var mainComp = ref mainEntity.Value.GetState();

                // set their attributes
                foreach (var (attr, value) in mainComp.Attributes)
                {
                    __instance.SetFloatValue((EBGUAttrFloat)attr, value);
                }

                SyncMainCharacterHp(__instance, ref mainComp);
            }
            else
            {
                var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(__instance.Owner as BGUCharacterCS);
                if (!tamerEntity.HasValue)
                    return;

                ref var localTamer = ref tamerEntity.Value.GetLocalTamer();
                if (!localTamer.IsTamerSynced)
                {
                    Logging.LogDebug("Monster {Name} is not synced, skipping HP update", __instance.Owner.GetName());
                    return;
                }

                var hpComp = tamerEntity.Value.GetHp();

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

    private static void SyncMainCharacterHp(BUC_AttrContainer attrContainer, ref MainCharacterComponent mainCharacter)
    {
        if (mainCharacter.Hp <= -80000)
        {
            Logging.LogError("Would set HP to {HP}, but will not (OOB fall damage)", mainCharacter.Hp);
            return;
        }

        var currentHp = attrContainer.GetFloatValue(EBGUAttrFloat.Hp);

        if (mainCharacter.Hp.Equals(currentHp, Constants.FloatComparisonTolerance))
            return; // do not reapply the same value

        var setHp = attrContainer.SetFloatValue(EBGUAttrFloat.Hp, mainCharacter.Hp);

        if (!setHp.Equals(mainCharacter.Hp, Constants.FloatComparisonTolerance))
        {
            Logging.LogDebug("Attempted to set player {PlayerName} HP to {DesiredHp}, instead set to {SetHp}", mainCharacter.CharacterNickName, mainCharacter.Hp, setHp);
        }

        if (mainCharacter.IsDead)
        {
            Logging.LogDebug("Applying unit dead for player {PlayerId}", mainCharacter.PlayerId);
            var events = BUS_EventCollectionCS.Get(attrContainer.Owner);
            events?.Evt_UnitDead!.Invoke(attrContainer.Owner, EDeadReason.SkillDamage);
        }
    }
}

[HarmonyPatch(typeof(BUS_AttrComp), "SetFloatValue")]
[HarmonyPatchCategory(Constants.DisabledPatches)]
public static class PvpPatchHp
{
    public static bool Prefix(EBGUAttrFloat AttrID)
    {
        if (!DI.Instance.AreaState.InRoom)
            return true;

        return AttrID != EBGUAttrFloat.Hp || DI.Instance.AreaState.IsMasterClient;
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

        if (AttrID == EBGUAttrFloat.Hp)
        {
            // I am a server
            if (DI.Instance.AreaState.IsMasterClient)
            {
                var mainEntity = playerState.LocalMainCharacter;
                if (!mainEntity.HasValue)
                    return;

                ref var mainComp = ref mainEntity.Value.GetState();
                ref var localMainComp = ref mainEntity.Value.GetLocalState();

                // I was damaged, set my Hp
                if (owner == localMainComp.Pawn)
                {
                    if (!mainComp.Hp.Equals(result, Constants.FloatComparisonTolerance))
                    {
                        mainComp.Hp = result;
                    }

                    return;
                }

                // remote player was damaged, set his properties
                var remoteMainEntity = DI.Instance.PawnState.GetEntityByPlayerPawn(owner);
                if (remoteMainEntity != null)
                {
                    ref var remoteMain = ref remoteMainEntity.Value.GetState();

                    if (!remoteMain.Hp.Equals(result, Constants.FloatComparisonTolerance))
                    {
                        remoteMain.Hp = result;
                    }

                    return;
                }

                // monster was damaged
                var tamerEntity = DI.Instance.PawnState.GetEntityByTamerMonster(owner as BGUCharacterCS);
                if (!tamerEntity.HasValue || !tamerEntity.Value.GetLocalTamer().IsTamerSynced)
                {
                    Logging.LogDebug("Monster {Name} is not synced, skipping HP update", owner.GetName());
                    return;
                }

                ref var hpComp = ref tamerEntity.Value.GetHp();

                hpComp.HpMaxBase = ___AttrContainer.GetFloatValue(EBGUAttrFloat.HpMaxBase);
                hpComp.Hp = result;
            }

            // I am a client
            return;
        }

        // only sync attributes that influence combat and are client-authoritative
        if (Constants.SyncedAttributes.Contains(AttrID) && owner == playerState.LocalMainCharacter?.GetLocalState().Pawn)
        {
            var mainEntity = playerState.LocalMainCharacter;
            ref var main = ref mainEntity.Value.GetState();

            if (main.Attributes.TryGetAttribute((byte)AttrID, out var existing)
                && existing.Equals(result, Constants.FloatComparisonTolerance))
            {
                return;
            }

            main.Attributes.SetAttribute((byte)AttrID, result);

            // some attributes may influence other attributes
            var calc = AttrMgr<EBGUAttrFloat, float>.getInstance().GetCalc(AttrID, out var valid);
            if (valid)
            {
                var finalVal = ___AttrContainer.GetFloatValue(calc.finalVal);
                main.Attributes.SetAttribute((byte)calc.finalVal, finalVal);
            }
        }
    }
}
