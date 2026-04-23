using b1;
using BtlShare;
using ReadyM.Api.Command;
using ReadyM.Relay.Client;
using WukongMp.Api.Chat;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Command;

internal class GiveUpCommandRegistration(
    IClientEcsUpdateLoop ecsLoop,
    WukongPlayerState playerState,
    WukongChatter chatter) : IConsoleCommandRegistration
{
    private readonly WukongPlayerState _playerState = playerState;
    
    public void RegisterCommands(ConsoleCommandRegistry registry)
    {
        registry.AddCommand("giveup", ConsoleCommand.Create(RequestGiveUp, isDebugOnly: false));
    }

    private void RequestGiveUp()
    {
        chatter.SendLocalizedServerMessage("PlayerGaveUp", _playerState.Nickname);

        // no need to send an RPC event since in co-op all players are authoritative over their HP
        ecsLoop.Scheduler.Schedule(static (_, self) =>
        {
            if (self._playerState.LocalMainCharacter is not { } mainEntity)
                return;

            // FIXME(api): Replace with a game event
            DebugUtils.InvincibilityEnabled = false; // otherwise we get black screen

            var events = BUS_EventCollectionCS.Get(mainEntity.Pawn);
            events?.Evt_IncreaseAttrFloat.Invoke(EBGUAttrFloat.Hp, -2000f);
            events?.Evt_UnitDead.Invoke(mainEntity.Pawn, EDeadReason.Suicide);
        }, this);
    }
}