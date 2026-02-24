using System;
using Microsoft.Extensions.Logging;
using WukongMp.Sdk.Api;

namespace WukongMp.Sdk;

/// Base class for plugin systems. Adds itself to the update loop on creation and removes itself on disposal.
public abstract class PluginSystemBase : IDisposable
{
    protected readonly WukongLocalApi LocalApi;
    protected readonly WukongClientApi ClientApi;
    protected readonly ILogger Logger;
    
    protected PluginSystemBase(WukongLocalApi localApi, WukongClientApi clientApi, ILogger logger)
    {
        LocalApi = localApi;
        ClientApi = clientApi;
        Logger = logger;
        
        clientApi.OnUpdate += OnUpdate;
    }

    public virtual void Dispose()
    {
        ClientApi.OnUpdate -= OnUpdate;
    }
    
    protected abstract void OnUpdate(PluginTick tick);
}