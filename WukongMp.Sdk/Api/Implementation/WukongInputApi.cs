using System;
using CSharpModBase.Input;
using WukongMp.Api.Input;

namespace WukongMp.Sdk.Api.Implementation;

internal sealed class WukongInputApi(
    IInputManager manager,
    WukongInputManager wukongInput
) : IWukongInputApi
{
    public HotKeyItem RegisterKeyBind(Key key, Action action)
    {
        return manager.RegisterKeyBind(key, action);
    }

    public HotKeyItem RegisterKeyBind(ModifierKeys modifiers, Key key, Action action)
    {
        return manager.RegisterKeyBind(modifiers, key, action);
    }

    public HotKeyItem RegisterGamePadBind(GamePadButton button, Action action)
    {
        return manager.RegisterGamePadBind(button, action);
    }

    /// <returns><c>true</c> if keyboard input is not blocked by active text fields and menus.</returns>
    public bool CanApplyInput()
    {
        return wukongInput.CanApplyInput();
    }
}