using System;
using CSharpModBase.Input;
using WukongMp.Api.Input;

namespace WukongMp.Sdk.Api.Implementation;

internal sealed class WukongInputApi(
    IInputManager manager,
    WukongInputManager wukongInput
) : IWukongInputApi
{
    public void RegisterKeyBind(Key key, Action action)
    {
        manager.RegisterKeyBind(key, action);
    }

    public void RegisterKeyBind(ModifierKeys modifiers, Key key, Action action)
    {
        manager.RegisterKeyBind(modifiers, key, action);
    }

    public void RegisterGamePadBind(GamePadButton button, Action action)
    {
        manager.RegisterGamePadBind(button, action);
    }

    /// <returns><c>true</c> if keyboard input is not blocked by active text fields and menus.</returns>
    public bool CanApplyInput()
    {
        return wukongInput.CanApplyInput();
    }
}