using System.Globalization;
using b1;
using BtlShare;
using WukongMp.Api.Chat;
using WukongMp.Api.Command;
using WukongMp.Api.Resources;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;
using WukongMp.Sdk.Entities;

namespace WukongMp.Sdk.Api.Implementation;

internal sealed class WukongCheatsApi(WukongPlayerState playerState, WukongAreaState areaState, WukongChatter chatter) : IWukongCheatsApi
{
    public bool CheatsAllowed => areaState.CurrentArea?.Room.CheatsAllowed ?? false;

    public void ToggleInfiniteMana()
    {
        if (!CheatsAllowed)
        {
            chatter.AddLocalServerMessage(BuiltinTexts.CheatsAreDisabled);
            return;
        }

        if (playerState.LocalMainCharacter is not { } mainEntity)
            return;

        ref var localStateComp = ref mainEntity.GetLocalState();
        if (mainEntity.Pawn != null)
        {
            PlayerUtils.ResetMana(mainEntity.Pawn);
        }

        localStateComp.HasInfiniteMana = !localStateComp.HasInfiniteMana;
        chatter.SendLocalizedServerMessage(localStateComp.HasInfiniteMana ? nameof(BuiltinTexts.InfManaEnabled) : nameof(BuiltinTexts.InfManaDisabled), playerState.Nickname);
    }

    public void ResetMana()
    {
        if (!CheatsAllowed)
        {
            chatter.AddLocalServerMessage(BuiltinTexts.CheatsAreDisabled);
            return;
        }

        var player = GameUtils.GetControlledPawn();
        if (player != null)
            PlayerUtils.ResetMana(player);
    }

    public void ResetCooldowns()
    {
        if (!CheatsAllowed)
        {
            chatter.AddLocalServerMessage(BuiltinTexts.CheatsAreDisabled);
            return;
        }

        var player = GameUtils.GetControlledPawn();
        if (player != null)
            PlayerUtils.ResetCooldowns(player);
    }

    public void SetSpritCooldownTime(float spiritCooldownTime)
    {
        if (WukongApi.Sync.LocalMainCharacter is not { } mainEntity)
            return;

        if (!WukongApi.Cheats.CheatsAllowed)
        {
            WukongApi.Console.LogMessage(BuiltinTexts.CheatsAreDisabled);
            return;
        }
        
        if (spiritCooldownTime < 0)
        {
            WukongApi.Console.LogMessage(BuiltinTexts.InvalidCooldown);
            return;
        }

        ref var localStateComp = ref mainEntity.Entity.GetLocalState();
        if (mainEntity.Pawn != null)
        {
            var events = BUS_EventCollectionCS.Get(mainEntity.Pawn);
            localStateComp.ShouldSetSpiritCooldown = true;
            events?.Evt_SetAttrFloat.Invoke(EBGUAttrFloat.VigorEnergy, BGUFunctionLibraryCS.BGUGetFloatAttr(mainEntity.Pawn, EBGUAttrFloat.VigorEnergyMax));
            localStateComp.ShouldSetSpiritCooldown = false;
        }

        localStateComp.SpiritCooldownEnabled = true;
        localStateComp.SpiritCooldownTime = spiritCooldownTime;
        chatter.SendLocalizedServerMessage(nameof(BuiltinTexts.CustomSpiritCooldown), playerState.Nickname, spiritCooldownTime.ToString(CultureInfo.InvariantCulture));
    }

    public void ToggleInfiniteVessel()
    {
        if (WukongApi.Sync.LocalMainCharacter is not { } mainEntity)
            return;

        if (!WukongApi.Cheats.CheatsAllowed)
        {
            WukongApi.Console.LogMessage(BuiltinTexts.CheatsAreDisabled);
            return;
        }

        if (mainEntity.Pawn != null)
        {
            var events = BUS_EventCollectionCS.Get(mainEntity.Pawn);
            events?.Evt_SetAttrFloat.Invoke(EBGUAttrFloat.FabaoEnergy, BGUFunctionLibraryCS.BGUGetFloatAttr(mainEntity.Pawn, EBGUAttrFloat.FabaoEnergyMax));
        }

        ref var state = ref mainEntity.Entity.GetLocalState();

        state.HasInfiniteVessel = !state.HasInfiniteVessel;
        chatter.SendLocalizedServerMessage(state.HasInfiniteVessel ? nameof(BuiltinTexts.InfVesselEnabled) : nameof(BuiltinTexts.InfVesselDisabled), playerState.Nickname);
    }

    public void ToggleInfiniteTransform()
    {
        if (WukongApi.Sync.LocalMainCharacter is not { } mainEntity)
            return;

        if (!WukongApi.Cheats.CheatsAllowed)
        {
            WukongApi.Console.LogMessage(BuiltinTexts.CheatsAreDisabled);
            return;
        }

        if (mainEntity.Pawn != null)
        {
            var events = BUS_EventCollectionCS.Get(mainEntity.Pawn);
            events?.Evt_SetAttrFloat.Invoke(EBGUAttrFloat.CurEnergy, BGUFunctionLibraryCS.BGUGetFloatAttr(mainEntity.Pawn, EBGUAttrFloat.TransEnergyMax));
        }

        ref var state = ref mainEntity.Entity.GetLocalState();
        state.HasInfiniteTransform = !state.HasInfiniteTransform;

        chatter.SendLocalizedServerMessage(state.HasInfiniteTransform ? nameof(BuiltinTexts.InfTransformEnabled) : nameof(BuiltinTexts.InfTransformDisabled), mainEntity.Nickname);
    }

    public void ToggleNoSkillsCooldown()
    {
        if (WukongApi.Sync.LocalMainCharacter is not { } mainEntity)
            return;

        if (!WukongApi.Cheats.CheatsAllowed)
        {
            WukongApi.Console.LogMessage(BuiltinTexts.CheatsAreDisabled);
            return;
        }

        var events = BUS_EventCollectionCS.Get(mainEntity.Pawn);
        events?.Evt_ResetSkillCD.Invoke();

        ref var state = ref mainEntity.Entity.GetLocalState();
        state.InstantSkillCooldown = !state.InstantSkillCooldown;

        chatter.SendLocalizedServerMessage(state.InstantSkillCooldown ? nameof(BuiltinTexts.InstantCooldownEnabled) : nameof(BuiltinTexts.InstantCooldownDisabled), playerState.Nickname);
    }
}