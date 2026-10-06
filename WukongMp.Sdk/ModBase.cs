using System.Linq;
using System;
using DryIoc;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.ECS.Worlds;
using WukongMp.Api;

namespace WukongMp.Sdk;

/// <summary>
/// Base class for WukongMP SDK mods.
/// Each mod should have exactly one class extending from this, which will be instantiated by the mod loader.
/// </summary>
[Obsolete("Use the [ModEntry] attribute on your mod's entry point class instead.")]
public abstract class ModBase : ModHostBase
{
    /// <summary>
    /// Register new archetypes or modify existing.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    protected void RegisterArchetypes(Action<IArchetypeRegistry> configure)
        => DI.Instance.RegisterSingleton<IArchetypeRegistration>(new FunctionalArchetypeRegistration(configure));

    private protected override void OnInit() => ScanForAndRegisterSystems();

    private void ScanForAndRegisterSystems()
    {
        var eligible = GetType().Assembly.GetTypes()
            .Where(t => typeof(ModSystemBase).IsAssignableFrom(t) && !t.IsAbstract);

        foreach (var type in eligible)
        {
            Logger.LogDebug("Found mod system: {SystemType}", type.FullName);
            DI.Instance.Container.RegisterMany([typeof(ModSystemBase), type], type);
        }
    }

    private class FunctionalArchetypeRegistration(Action<IArchetypeRegistry> callback) : IArchetypeRegistration
    {
        public void Register(IArchetypeRegistry registry) => callback(registry);
    }
}
