using System.Linq;
using CSharpModBase;
using DryIoc;
using Microsoft.Extensions.Logging;
using ReadyM.Api;
using ReadyM.Api.DI;
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
    private PatcherBase _patcher = null!;

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
        ScanForAndRegisterSystems();
        Initialize(WukongApi.Services);
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