using System;
using CSharpModBase.Input;
using WukongMp.Api.Input;

namespace WukongMp.Sdk.Api;

public sealed class WukongInputApi : IInputManager
{
    private readonly IInputManager _manager;
    private readonly WukongInputManager _wukongInput;

    internal WukongInputApi(IInputManager manager, WukongInputManager wukongInput)
    {
        _manager = manager;
        _wukongInput = wukongInput;
    }

    public HotKeyItem RegisterKeyBind(Key key, Action action)
    {
        return _manager.RegisterKeyBind(key, action);
    }

    public HotKeyItem RegisterKeyBind(ModifierKeys modifiers, Key key, Action action)
    {
        return _manager.RegisterKeyBind(modifiers, key, action);
    }

    public HotKeyItem RegisterGamePadBind(GamePadButton button, Action action)
    {
        return _manager.RegisterGamePadBind(button, action);
    }
    
    /// <returns><c>true</c> if keyboard input is not blocked by active text fields and menus.</returns>
    public bool CanApplyInput()
    {
        return _wukongInput.CanApplyInput();
    }
}