using System;
using System.Collections.Generic;
using System.Linq;
using CSharpModBase;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using ReadyM.Api;
using WukongMp.Api;

namespace WukongMp.Sdk;

public abstract class ModBase : ICSharpModExV2
{
    protected ILogger Logger { get; private set; } = null!;
    private PatcherBase _patcher = null!;

    public abstract string Name { get; }
    public abstract string Version { get; }

    public bool IsDebug
#if DEBUG
        => true;
#else
            => false;
#endif

    private List<BaseSystem> _systems = [];

    private SystemGroup _systemGroup = null!;

    public void Init()
    {
        Initialize();

        _systems = [.. ScanForAndInitializeSystems().Select(x => x.ToBaseSystem())];
        _systemGroup = new SystemGroup(Name);

        foreach (var system in _systems)
        {
            _systemGroup.Add(system);
        }

        _systemGroup.SetMonitorPerf(true);
        DI.Instance.World.SystemRoot.Add(_systemGroup);
    }

    private IEnumerable<ModSystemBase> ScanForAndInitializeSystems()
    {
        var eligible = GetType().Assembly.GetTypes()
            .Where(t => typeof(ModSystemBase).IsAssignableFrom(t) && !t.IsAbstract);

        foreach (var type in eligible)
        {
            Logger.LogDebug("Found mod system: {SystemType}", type.FullName);
            var instance = (ModSystemBase)Activator.CreateInstance(type)!;
            instance.Initialize(Logger);
            yield return instance;
        }
    }

    protected virtual void Initialize() { }

    public virtual void DeInit()
    {
        // TODO: Replace with proper DI container
        _systems.ForEach(x =>
        {
            if (x is IDisposable disposable)
                disposable.Dispose();
        });

        if (_patcher.IsPatched)
        {
            _patcher.Unpatch();
        }
    }

    public virtual void LateInit()
    {
        _patcher = new WukongPatcher(this.GetType().Assembly, this.Name, DI.Instance.Prelude);
        if (!_patcher.IsPatched)
        {
            _patcher.Patch();
        }
    }

    public void SetLoggerFactory(ILoggerFactory loggerFactory)
    {
        DI.Instance.InitLogging(loggerFactory);
        Logger = DI.Instance.Logger;
    }

    public virtual object? GetReloadContext()
    {
        return null;
    }

    public virtual void Reload(object? context)
    {
        Logger.LogWarning("Mod {Name} does not support hot reload", Name);
    }
}