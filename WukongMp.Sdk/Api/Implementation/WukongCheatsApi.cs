using WukongMp.Api.Chat;
using WukongMp.Api.Command;
using WukongMp.Api.Resources;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;

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
        chatter.SendLocalizedServerMessage(localStateComp.HasInfiniteMana ? "InfManaEnabled" : "InfManaDisabled", playerState.Nickname);
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
}