using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;

namespace WukongMp.Sdk;

/// Base class for plugin systems. Adds itself to the update loop on creation and removes itself on disposal.
public abstract class ModSystemBase
{
    protected ILogger Logger { get; private set; } = null!;

    internal void Initialize(ILogger logger)
    {
        Logger = logger;
    }

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