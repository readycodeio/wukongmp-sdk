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

        var modSystems = DefineSystems().ToList();
        modSystems.ForEach(x => x.Initialize(Api.ReadyM.Local, Api.ReadyM.Client, Logger));
        
        _systems = [.. modSystems.Select(x => x.ToBaseSystem())];
        _systemGroup = new SystemGroup(Name);

        foreach (var system in _systems)
        {
            _systemGroup.Add(system);
        }

        _systemGroup.SetMonitorPerf(true);
        DI.Instance.World.SystemRoot.Add(_systemGroup);
    }

    protected virtual void Initialize() { }

    [Obsolete("Use attributes and declarative systems instead")]
    protected virtual IEnumerable<ModSystemBase> DefineSystems()
    {
        yield break;
    }

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
        _patcher = Api.ReadyM.GetPatcher(this);
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