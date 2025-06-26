using System;

namespace WukongMp.Api.Old;

public class WukongEventBus
{
    public event Action? OnBeginPlayGameplayLevel;
    public event Action? OnEndPlayGameplayLevel;
    public event Action? OnLoadingScreenClose;

    private bool _gameplayScene;
    
    // this is triggered for every player controller, but we want to apply the logic once
    public bool TryInvokeBeginGameplayLevel()
    {
        if (_gameplayScene)
            return false;
        _gameplayScene = true;
        OnBeginPlayGameplayLevel?.Invoke();
        return true;
    }
    
    public bool TryInvokeEndPlayGameplayLevel()
    {
        if (_gameplayScene)
            return false;
        _gameplayScene = false;
        OnEndPlayGameplayLevel?.Invoke();
        return true;
    }
    
    public void InvokeLoadingScreenClose()
    {
        OnLoadingScreenClose?.Invoke();
    }
}
