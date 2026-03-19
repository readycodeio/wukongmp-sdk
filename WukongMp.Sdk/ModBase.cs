using System.Linq;
using CSharpModBase;
using DryIoc;
using Microsoft.Extensions.Logging;
using ReadyM.Api;
using WukongMp.Api;
using WukongMp.Sdk.Api;

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

    protected virtual void Initialize(IDependencyContainer services) { }

    public virtual void LateInit()
    {
        _patcher = new WukongPatcher(GetType().Assembly, Name, DI.Instance.Prelude);
        if (!_patcher.IsPatched)
        {
            _patcher.Patch();
        }
    }

    public virtual void DeInit()
    {
        if (_patcher.IsPatched)
        {
            _patcher.Unpatch();
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