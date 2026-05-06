using System;

namespace WukongMp.Api;

/// <summary>
/// Exposes events related to level transitions, such as loading, playing, and exiting levels.
/// This allows mods to hook into these events and perform actions at the appropriate times during the level lifecycle.
/// </summary>
internal sealed class WukongEventBus
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
    public event Action? OnLoadingScreenOpen;
    public event Action? OnLoadingScreenClose;
    public event Action? OnLevelLoaded;
    public event Action? OnExitLevel;

    private LevelTransitionPhase _phase;

    public bool IsGameplayLevel { get; private set; }
    
    // this is triggered when beginning to load the gameplay level
    internal bool TryInvokeBeginLoadGameplayLevel()
    {
        if (_phase == LevelTransitionPhase.Loading)
            return false;
        _phase = LevelTransitionPhase.Loading;
        OnBeginLoadGameplayLevel?.Invoke();
        return true;
    }
    
    internal bool TryInvokeBeginPlayGameplayLevel()
    {
        if (_phase == LevelTransitionPhase.Playing)
            return false;
        _phase = LevelTransitionPhase.Playing;
        IsGameplayLevel = true;
        OnBeginPlayGameplayLevel?.Invoke();
        return true;
    }
    
    internal bool TryInvokeEndPlayGameplayLevel()
    {
        if (_phase == LevelTransitionPhase.Ending)
            return false;
        _phase = LevelTransitionPhase.Ending;
        OnEndPlayGameplayLevel?.Invoke();
        IsGameplayLevel = false;
        return true;
    }
    
    internal void InvokeLoadingScreenOpen()
    {
        OnLoadingScreenOpen?.Invoke();
    }
    
    internal void InvokeLoadingScreenClose()
    {
        _phase = LevelTransitionPhase.None;
        OnLoadingScreenClose?.Invoke();
    }

    internal void InvokeOnLevelLoaded()
    {
        _phase = LevelTransitionPhase.None;
        OnLevelLoaded?.Invoke();
    }

    internal void InvokeOnExitLevel()
    {
        _phase = LevelTransitionPhase.None;
        OnExitLevel?.Invoke();
    }
}
