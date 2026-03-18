using System;
using CSharpModBase.Input;

namespace WukongMp.Sdk.Api;

public interface IWukongInputApi
{
    HotKeyItem RegisterKeyBind(Key key, Action action);
    HotKeyItem RegisterKeyBind(ModifierKeys modifiers, Key key, Action action);
    HotKeyItem RegisterGamePadBind(GamePadButton button, Action action);

    /// <returns><c>true</c> if keyboard input is not blocked by active text fields and menus.</returns>
    bool CanApplyInput();
}