using System;
using Friflo.Engine.ECS.Systems;
using ReadyM.SDK.Client.Systems;

namespace WukongMp.Sdk;

/// Base class for plugin systems. Adds itself to the update loop on creation and removes itself on disposal.
[Obsolete("Use [System] attribute instead.")]
public abstract class ModSystemBase
{
    protected readonly ref struct UpdateTick(float deltaTime, float time)
    {
        /// The time in seconds since the last tick. 
        public readonly float deltaTime = deltaTime;

        /// The time at the beginning of the current frame since application start. 
        public readonly float time = time;
    }

    private class PluginSystemWrapper(ModSystemBase modSystem) : BaseSystem
    {
        public override string Name { get; } = modSystem.GetType().Name;

        protected override void OnUpdateGroup()
        {
            if (!ModSystems.Running)
                return;

            modSystem.OnUpdate(new UpdateTick(Tick.deltaTime, Tick.time));
        }
    }

    internal BaseSystem ToBaseSystem() => new PluginSystemWrapper(this);

    protected abstract void OnUpdate(UpdateTick tick);
}