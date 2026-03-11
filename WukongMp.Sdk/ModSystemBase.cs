using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using WukongMp.Sdk.Api;

namespace WukongMp.Sdk;

/// Base class for plugin systems. Adds itself to the update loop on creation and removes itself on disposal.
public abstract class ModSystemBase(WukongLocalApi localApi, WukongClientApi clientApi, ILogger logger)
{
    protected readonly WukongLocalApi LocalApi = localApi;
    protected readonly WukongClientApi ClientApi = clientApi;
    protected readonly ILogger Logger = logger;

    private class PluginSystemWrapper(ModSystemBase modSystem) : BaseSystem
    {
        public override string Name { get; } = modSystem.GetType().Name;

        protected override void OnUpdateGroup()
        {
            modSystem.OnUpdate(Tick);
        }
    }

    internal BaseSystem ToBaseSystem() => new PluginSystemWrapper(this);

    protected abstract void OnUpdate(UpdateTick tick);
}