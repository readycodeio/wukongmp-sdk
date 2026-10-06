using System;
using CSharpModBase;
using Microsoft.Extensions.Logging;
using ReadyM.Api;
using ReadyM.Api.DI;
using ReadyM.Api.Loader;
using ReadyM.SDK.Archetypes;
using WukongMp.Api;
using WukongMp.Sdk.Api;

namespace WukongMp.Sdk;

public abstract class ModHostBase : ICSharpModExV2
{
    private PatcherBase _patcher = null!;

    private protected ModHostBase()
    {
    }

    protected ILogger Logger { get; private set; } = null!;

    /// This mod's own folder under <c>Mods</c>, which holds its assemblies, manifest and any config files.
    protected string ModDirectory { get; private set; } = null!;

    protected IDependencyContainer Services { get; private set; } = null!;

    /// Used for logging and patching.
    public abstract string Name { get; }

    /// Whether this is a debug build, which a mod can read to turn on what only makes sense in one.
    public bool IsDebug
#if DEBUG
        => true;
#else
        => false;
#endif

    /// Called by the mod loader on game start.
    public void Init()
    {
        RegisterGeneratedShapes();

        Services = WukongApi.Services;

        OnInit();
        Initialize(Services);
    }

    /// Called by the mod loader after all <c>Init</c> calls.
    public virtual void LateInit()
    {
        Logger.LogInformation("LateInit: {ModName}", Name);

        _patcher = new WukongPatcher(GetType().Assembly, Name, DI.Instance.Prelude);

        if (!_patcher.IsPatched)
            _patcher.Patch();
    }

    /// Called by the mod loader on game closing.
    public virtual void DeInit()
    {
        Logger.LogInformation("DeInit: {ModName}", Name);

        if (_patcher.IsPatched)
            _patcher.Unpatch();
    }

    public void SetLoggerFactory(ILoggerFactory loggerFactory)
    {
        DI.Instance.InitLogging(loggerFactory);
        Logger = DI.Instance.Logger;
    }

    /// Called by the mod loader, before <see cref="Init" />.
    public void SetModDirectory(string directory) => ModDirectory = directory;

    /// Called by the mod loader. Used in hot reload.
    public virtual object? GetReloadContext() => null;

    /// Called by the mod loader. Used in hot reload.
    public virtual void Reload(object? context)
        => Logger.LogWarning("Mod {Name} does not support hot reload", Name);

    protected abstract void Initialize(IDependencyContainer services);

    /// What a derived base does before the mod's own Initialize. Nothing, unless one says otherwise.
    private protected virtual void OnInit()
    {
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
        => Services.RegisterSingleton(ModConfigReader.Read<TConfig>(ModDirectory, fileName, Logger));

    private static bool _shapesRegistered;

    /// Registers what every loaded assembly's shapes add to an archetype, and which component holds an index.
    private void RegisterGeneratedShapes()
    {
        if (_shapesRegistered)
            return;

        _shapesRegistered = true;

        var applied = GeneratedShapes.ApplyAll(
            AppDomain.CurrentDomain.GetAssemblies(),
            assembly => Logger?.LogDebug("Registered shapes from {Assembly}", assembly.GetName().Name),
            (assembly, ex) => Logger?.LogError(ex, "Failed to register shapes from {Assembly}", assembly.GetName().Name));

        Logger?.LogInformation("Registered shapes from {Count} assemblies", applied);
    }
}
