using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using ReadyM.Relay.Server.Sdk;

namespace WukongMp.Sdk.Serverside;

[UsedImplicitly]
internal class Mod : ServerModBase
{
    protected override void Init()
    {
        Services.RegisterSingleton<RpcHandlers>();

        var logger = Services.Resolve<ILogger>();
        logger.LogInformation("Serverside SDK mod initialized");
    }
}