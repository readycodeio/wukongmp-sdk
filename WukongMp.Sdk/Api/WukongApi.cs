using CSharpModBase.Input;
using WukongMp.Api;
using WukongMp.Api.Configuration;

namespace WukongMp.Sdk.Api;

public static class WukongApi // TODO: Interfaces
{
    internal static GameplayConfiguration Configuration => DI.Instance.GameplayConfiguration;
    public static IInputManager Input => DI.Instance.InputManager;
    public static WukongConsoleApi Console { get; } = new(DI.Instance.CommandConsole, DI.Instance.CommandRegistry);
    public static WukongFileApi Files { get; } = new(DI.Instance.Logger);
    public static IWukongSaveRelay Saves => DI.Instance.SaveRelay;
    public static WukongEventApi Events { get; } = new(DI.Instance.State, DI.Instance.PlayerPawnState, DI.Instance.PlayerState);
    public static WukongClientApi Client { get; } = new(DI.Instance.World, DI.Instance.State, DI.Instance.AreaState, DI.Instance.PlayerState, DI.Instance.RelayClient);

    public static WukongLocalApi Local { get; } = new(
        DI.Instance.EventBus,
        DI.Instance.WidgetManager,
        DI.Instance.EcsLoop
    );
}