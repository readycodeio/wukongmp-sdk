using Friflo.Engine.ECS;
using ReadyM.Relay.Client.State;
using System;
using WukongMp.Api;
using WukongMp.Api.Chat;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.State;

namespace WukongMp.PvP.Chat;

internal class PvpChatter : IDisposable
{
    private readonly WukongChatter _wukongChatter;
    private readonly GameplayEventRouter _eventRouter;
    private readonly WukongAreaState _areaState;
    private readonly ClientOwnershipManager _clientOwnership;

    public PvpChatter(
        WukongChatter wukongChatter,
        GameplayEventRouter eventRouter,
        WukongAreaState areaState,
        ClientOwnershipManager clientOwnership
    )
    {
        Logging.LogDebug("Initializing PvpChatter");

        _wukongChatter = wukongChatter;
        _eventRouter = eventRouter;
        _areaState = areaState;
        _clientOwnership = clientOwnership;

        _eventRouter.OnUnitDead += OnUnitDead;
    }

    public void Dispose()
    {
        Logging.LogDebug("Disposing PvpChatter");

        _eventRouter.OnUnitDead -= OnUnitDead;
    }

    private void OnUnitDead(Entity victim, Entity attacker)
    {
        if (_areaState is { PvpState.InPvP: true })
        {
            if (victim != attacker)
            {
                if (MainCharacterEntity.TryGetMainCharacter(victim, out var victimMainEntity) &&
                    MainCharacterEntity.TryGetMainCharacter(attacker, out var attackerMainEntity))
                {
                    if (!_clientOwnership.OwnsEntity(victimMainEntity.Value.Entity))
                        return;

                    ref var attackerMain = ref attackerMainEntity.Value.GetState();
                    ref var killedMain = ref victimMainEntity.Value.GetState();

                    _wukongChatter.SendServerMessage("PlayerKilledPlayer", attackerMain.CharacterNickName, killedMain.CharacterNickName);
                }
            }
        }
    }
}