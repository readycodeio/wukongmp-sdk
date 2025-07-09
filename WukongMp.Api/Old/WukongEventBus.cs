using System;

namespace WukongMp.Api.Old;

public class WukongEventBus
{
    private enum LevelTransitionPhase
    {
        None,
        Loading,
        Playing,
        Ending
    }
    
    public event Action? OnBeginLoadGameplayLevel;
    public event Action? OnBeginPlayGameplayLevel;
    public event Action? OnEndPlayGameplayLevel;
    public event Action? OnLoadingScreenClose;

    private LevelTransitionPhase _phase;
    
    // this is triggered when beginning to load the gameplay level
    public bool TryInvokeBeginLoadGameplayLevel()
    {
        if (_phase == LevelTransitionPhase.Loading)
            return false;
        _phase = LevelTransitionPhase.Loading;
        OnBeginLoadGameplayLevel?.Invoke();
        return true;
    }
    
    // this is triggered for every player controller, but we want to apply the logic once
    public bool TryInvokeBeginPlayGameplayLevel()
    {
        if (_phase == LevelTransitionPhase.Playing)
            return false;
        _phase = LevelTransitionPhase.Playing;
        OnBeginPlayGameplayLevel?.Invoke();
        return true;
    }
    
    public bool TryInvokeEndPlayGameplayLevel()
    {
        if (_phase == LevelTransitionPhase.Ending)
            return false;
        _phase = LevelTransitionPhase.Ending;
        OnEndPlayGameplayLevel?.Invoke();
        return true;
    }
    
    public void InvokeLoadingScreenClose()
    {
        _phase = LevelTransitionPhase.None;
        OnLoadingScreenClose?.Invoke();
    }
}
