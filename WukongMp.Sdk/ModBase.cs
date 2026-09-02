using System;
using System.Linq;
using CSharpModBase;
using DryIoc;
using Microsoft.Extensions.Logging;
using ReadyM.Api;
using ReadyM.Api.DI;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Loader;
using WukongMp.Api;
using WukongMp.Sdk.Api;

namespace WukongMp.Sdk;

/// <summary>
/// Base class for WukongMP SDK mods.
/// Each mod should have exactly one class extending from this, which will be instantiated by the mod loader.
/// </summary>
public abstract class ModBase : ICSharpModExV2
{
    protected ILogger Logger { get; private set; } = null!;

    /// <summary>
    /// This mod's own folder under <c>Mods</c>, which holds its assemblies, manifest and any config files.
    /// </summary>
    protected string ModDirectory { get; private set; } = null!;

    private PatcherBase _patcher = null!;
    private IDependencyContainer _services = null!;

    /// <summary>
    /// Mod name, used for logging and patching.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Indicates whether the mod is running in a debug build.
    /// Can be used to enable debug-only features or logging.
    /// </summary>
    public bool IsDebug
#if DEBUG
        => true;
#else
            => false;
#endif

    /// <summary>
    /// Called by the mod loader on game start.
    /// </summary>
    public void Init()
    {
        _services = WukongApi.Services;
        ScanForAndRegisterSystems();
        Initialize(_services);
    }

    /// <summary>
    /// Reads <paramref name="fileName"/> from this mod's folder and registers the result as a singleton.
    /// </summary>
    /// <remarks>
    /// A missing file yields defaults. A file that exists but does not parse, or that carries a key the
    /// config type does not declare, throws <see cref="ModConfigException" />.
    /// </remarks>
    protected void RegisterConfig<TConfig>(string fileName = ModConfigReader.DefaultFileName)
        where TConfig : class, new()
    {
        _services.RegisterSingleton(ModConfigReader.Read<TConfig>(ModDirectory, fileName, Logger));
    }
    
    private class FunctionalArchetypeRegistration(Action<IArchetypeRegistry> callback) : IArchetypeRegistration
    {
        public void Register(IArchetypeRegistry registry)
        {
            callback(registry);
        }
    }
    
    /// <summary>
    /// Register new archetypes or modify existing.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    protected void RegisterArchetypes(Action<IArchetypeRegistry> configure)
    {
        DI.Instance.RegisterSingleton<IArchetypeRegistration>(new FunctionalArchetypeRegistration(configure));
    }

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

    protected abstract void Initialize(IDependencyContainer services);

    /// <summary>
    /// Called by the mod loader after all <c>Init</c> calls.
    /// </summary>
    public virtual void LateInit()
    {
        Logger.LogInformation("LateInit: {ModName}", Name);
        _patcher = new WukongPatcher(GetType().Assembly, Name, DI.Instance.Prelude);
        if (!_patcher.IsPatched)
        {
            _patcher.Patch();
        }
    }

    /// <summary>
    /// Called by the mod loader on game closing.
    /// </summary>
    public virtual void DeInit()
    {
        Logger.LogInformation("DeInit: {ModName}", Name);
        if (_patcher.IsPatched)
        {
            _patcher.Unpatch();
        }
    }

    /// <summary>
    /// Called by the mod loader.
    /// </summary>
    public void SetLoggerFactory(ILoggerFactory loggerFactory)
    {
        DI.Instance.InitLogging(loggerFactory);
        Logger = DI.Instance.Logger;
    }

    /// <summary>
    /// Called by the mod loader, before <see cref="Init" />.
    /// </summary>
    public void SetModDirectory(string directory)
    {
        ModDirectory = directory;
    }

    /// <summary>
    /// Called by the mod loader.
    /// Used in hot reload.
    /// </summary>
    public virtual object? GetReloadContext()
    {
        return null;
    }

    /// <summary>
    /// Called by the mod loader.
    /// Used in hot reload.
    /// </summary>
    public virtual void Reload(object? context)
    {
        Logger.LogWarning("Mod {Name} does not support hot reload", Name);
    }
}