using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using WukongMp.Sdk;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Entities;

namespace WukongMp.Coop.Systems;

public class FixYellowbrowSystem : ModSystemBase
{
    protected override void OnUpdate(UpdateTick tick)
    {
        if (!ClientApi.InRoom || !ClientApi.LocalMainCharacter.HasValue)
            return;

        foreach (var tamer in ClientApi.AllTamers)
        {
            // FIXME(api): Define Guid constants somewhere
            // FIXME(api): Rename `Guid` to something less confusing
            if (tamer is { IsMonsterActive: true, Hp: < 1f, Guid: "UGuid.LYS.HuangMei.Big" })
            {
                if (ClientApi.LocalMainCharacter.Value.IsDead)
                {
                    // rebirth player
                    ClientApi.LocalMainCharacter.Value.RebirthInPlace();
                }
            }
        }
    }
}