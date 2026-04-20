using ReadyM.Api.DI;
using WukongMp.Api;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Entities;

namespace WukongMp.PvP.Chat;

public class PvpChatter : IHostedService
{
    public void OnScopeStart()
    {
        WukongApi.Events.OnPlayerDead += OnPlayerDead;
    }

    public void Dispose()
    {
        WukongApi.Events.OnPlayerDead -= OnPlayerDead;
    }

    private void OnPlayerDead(ReadyMainCharacter victim, ReadyCharacter? attacker)
    {
        if (!WukongApi.PvP.InPvP || !attacker.HasValue) 
            return;

        if (victim.PlayerId != WukongApi.Sync.LocalPlayerId)
            return;
        
        if (victim.Pawn == attacker.Value.Pawn) 
            return;
        
        if (!_clientOwnership.OwnsEntity(victimMainEntity.Value.Entity))
            return;

        ref var attackerMain = ref attackerMainEntity.Value.GetState();
        ref var killedMain = ref victimMainEntity.Value.GetState();

        WukongApi.Chat.SendServerMessage("PlayerKilledPlayer", attackerMain.CharacterNickname, killedMain.CharacterNickname);
    }
}