using Microsoft.Extensions.Logging;
using WukongMp.Sdk;
using WukongMp.Sdk.Api;

namespace WukongMp.Coop.Systems;

public class FixYellowbrowSystem(WukongLocalApi localApi, WukongClientApi clientApi, ILogger logger)
    : PluginSystemBase(localApi, clientApi, logger)
{
    protected override void OnUpdate(PluginTick tick)
    {
        if (!ClientApi.InRoom || !ClientApi.LocalMainCharacter.HasValue)
            return;

        foreach (var tamer in ClientApi.AllTamers)
        {
            // FIXME(api): Define Guid constants somewhere
            // FIXME(api): Rename `Guid` to something less confusing
            if (tamer.IsMonsterActive && tamer.Hp < 1f && tamer.Guid == "UGuid.LYS.HuangMei.Big")
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