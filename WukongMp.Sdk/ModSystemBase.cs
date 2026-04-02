using Friflo.Engine.ECS.Systems;

namespace WukongMp.Sdk;

/// Base class for plugin systems. Adds itself to the update loop on creation and removes itself on disposal.
public abstract class ModSystemBase
{
    protected readonly ref struct UpdateTick(float deltaTime, float time)
    {
        /// <summary> The time in seconds since the last tick. </summary>
        public readonly float deltaTime = deltaTime;

        /// <summary> The time at the beginning of the current frame since application start. </summary>
        public readonly float time = time;
    }

    private class PluginSystemWrapper(ModSystemBase modSystem) : BaseSystem
    {
        public override string Name { get; } = modSystem.GetType().Name;

        protected override void OnUpdateGroup()
        {
            modSystem.OnUpdate(new UpdateTick(Tick.deltaTime, Tick.time));
        }
    }

    internal BaseSystem ToBaseSystem() => new PluginSystemWrapper(this);

    protected abstract void OnUpdate(UpdateTick tick);
}