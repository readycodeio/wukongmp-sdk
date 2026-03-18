using WukongMp.Sdk;
using WukongMp.Sdk.Api;
using WukongMp.Sdk.Entities;

namespace WukongMp.Coop.Systems;

// ReSharper disable once UnusedType.Global
public class FixYellowbrowSystem : ModSystemBase
{
    protected override void OnUpdate(UpdateTick tick)
    {
        if (!WukongApi.Client.InRoom || !WukongApi.Client.LocalMainCharacter.HasValue)
            return;

        foreach (var tamer in WukongApi.Client.AllTamers)
        {
            // FIXME(api): Define Guid constants somewhere
            // FIXME(api): Rename `Guid` to something less confusing
            if (tamer is { IsMonsterActive: true, Hp: < 1f, Guid: "UGuid.LYS.HuangMei.Big" })
            {
                if (WukongApi.Client.LocalMainCharacter.Value.IsDead)
                {
                    // rebirth player
                    WukongApi.Client.LocalMainCharacter.Value.RebirthInPlace();
                }
            }
        }
    }
}