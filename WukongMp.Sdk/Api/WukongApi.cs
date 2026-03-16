using System;
using CSharpModBase.Input;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Api.UI;

namespace WukongMp.Sdk.Api;

public static class WukongApi // TODO: Interfaces
{
    [Obsolete("TODO: Make a more centralized configuration system.")]
    public static GameplayConfiguration Configuration => DI.Instance.GameplayConfiguration;
    public static IInputManager Input => DI.Instance.InputManager;
    public static WukongConsoleApi Console { get; } = new(DI.Instance.CommandConsole, DI.Instance.CommandRegistry);
    public static WukongFileApi Files { get; } = new(DI.Instance.Logger);
    public static IWukongSaveRelay Saves => DI.Instance.SaveRelay;
    public static WukongEventApi Events { get; } = new(DI.Instance.State, DI.Instance.PlayerPawnState, DI.Instance.PlayerState, DI.Instance.EventBus, DI.Instance.GameplayEventRouter);
    public static WukongClientApi Client { get; } = new(DI.Instance.World, DI.Instance.State, DI.Instance.AreaState, DI.Instance.PlayerState, DI.Instance.MappingPolicyDir, DI.Instance.RelayClient);
    public static WukongWidgetManager Widgets  => DI.Instance.WidgetManager;

    public static WukongLocalApi Local { get; } = new(
        DI.Instance.EventBus,
        DI.Instance.WidgetManager,
        DI.Instance.EcsLoop
    );
}