using System;
using CSharpModBase.Input;

namespace WukongMp.Sdk.Api;

/// <summary>
/// Provides methods related to input, such as registering key binds.
/// </summary>
public interface IWukongInputApi
{
    /// <summary>
    /// Registers a key bind with the specified key and action.
    /// </summary>
    /// <param name="key">The key to bind.</param>
    /// <param name="action">The action to execute when the key is pressed.</param>
    void RegisterKeyBind(Key key, Action action);

    /// <summary>
    /// Registers a key bind with the specified modifier keys, key, and action.
    /// </summary>
    /// <param name="modifiers">The modifier keys (e.g., Ctrl, Alt, Shift).</param>
    /// <param name="key">The key to bind.</param>
    /// <param name="action">The action to execute when the key combination is pressed.</param>
    void RegisterKeyBind(ModifierKeys modifiers, Key key, Action action);

    /// <summary>
    /// Registers a gamepad button bind with the specified button and action.
    /// </summary>
    /// <param name="button">The gamepad button to bind.</param>
    /// <param name="action">The action to execute when the button is pressed.</param>
    void RegisterGamePadBind(GamePadButton button, Action action);

    /// <summary>
    /// Determines whether keyboard input can currently be applied (i.e., not blocked by active text fields or menus).
    /// </summary>
    /// <returns><c>true</c> if keyboard input is not blocked by active text fields and menus; otherwise, <c>false</c>.</returns>
    bool CanApplyInput();
}