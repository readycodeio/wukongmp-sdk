using b1;
using BtlShare;
using HarmonyLib;
using UnrealEngine.Engine;
using WukongMp.Api.Compat;
using WukongMp.Api.Configuration;

namespace WukongMp.Api.Patches;

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
[HarmonyPatchCategory(Constants.ConnectedPatches)]
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
