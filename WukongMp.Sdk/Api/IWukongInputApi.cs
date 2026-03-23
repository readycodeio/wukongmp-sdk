using System;
using CSharpModBase.Input;

namespace WukongMp.Sdk.Api;

public interface IWukongInputApi
{
    void RegisterKeyBind(Key key, Action action);
    void RegisterKeyBind(ModifierKeys modifiers, Key key, Action action);
    void RegisterGamePadBind(GamePadButton button, Action action);

    /// <returns><c>true</c> if keyboard input is not blocked by active text fields and menus.</returns>
    bool CanApplyInput();
}