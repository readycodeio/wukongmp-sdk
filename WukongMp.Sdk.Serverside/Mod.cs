using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using ReadyM.Relay.Server.Sdk;
using ReadyM.Relay.Server.Sdk.Ecs;
using ReadyM.Relay.Server.Sdk.Ecs.Components;

namespace WukongMp.Sdk.Serverside;

[UsedImplicitly]
public class Mod : ServerModBase
{
    protected override void RegisterComponents(IComponentRegistry registry)
    {
        // do nothing
    }

    protected override void Init()
    {
        Services.RegisterSingleton<RpcHandlers>();

        var logger = Services.Resolve<ILogger>();
        logger.LogInformation("Serverside SDK mod initialized");
    }
}