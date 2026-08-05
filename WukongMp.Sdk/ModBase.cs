using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
        // NOTE: LogInformation, not LogDebug, because a release build sets the minimum level to Information and
        // would drop these. The mod loader force-flushes every line for the duration of Init, so a step that
        // takes the process down is the last line in the log.
        Logger.LogInformation("Init step begin: ScanForAndRegisterSystems");
        ScanForAndRegisterSystems();
        Logger.LogInformation("Init step end: ScanForAndRegisterSystems");

        // Compiling Initialize is separated from running it on purpose. JIT-ing it makes Mono resolve every
        // type its body mentions, which reaches into the game assemblies we rewrote in memory, and a fault in
        // there happens before any statement in the method runs. Without this probe a crash during compilation
        // and a crash on the first line look identical from the log.
        Logger.LogInformation("Init step begin: JIT Initialize");
        try
        {
            var initializeMethod = GetType().GetMethod(
                nameof(Initialize),
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
            );

            if (initializeMethod != null)
                RuntimeHelpers.PrepareMethod(initializeMethod.MethodHandle);
            else
                Logger.LogWarning("Could not reflect Initialize, skipping the JIT probe");
        }
        catch (Exception ex)
        {
            // Not fatal: the probe is diagnostic only, and Initialize is compiled on call anyway.
            Logger.LogError(ex, "JIT probe for Initialize failed");
        }

        Logger.LogInformation("Init step end: JIT Initialize");

        Logger.LogInformation("Init step begin: Initialize");
        Initialize(WukongApi.Services);
        Logger.LogInformation("Init step end: Initialize");
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