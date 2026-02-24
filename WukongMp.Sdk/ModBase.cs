using CSharpModBase;
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

    public virtual void Init() { }

    public virtual void DeInit()
    {
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