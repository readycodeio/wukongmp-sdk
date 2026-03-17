using System;
using CSharpModBase.Input;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Api.UI;

namespace WukongMp.Sdk.Api;

public static class WukongApi // TODO: Interfaces
{
    static WukongApi()
    {
        Services.RegisterSingleton<WukongFileApi>();
        Services.RegisterSingleton<WukongConsoleApi>();
        Services.RegisterSingleton<WukongEventApi>();
        Services.RegisterSingleton<WukongClientApi>();
        Services.RegisterSingleton<WukongLocalApi>();
        Services.RegisterSingleton<WukongInputApi>();
    }

    public static IDependencyContainer Services => DI.Instance;

    [Obsolete("TODO: Make a more centralized configuration system.")]
    public static GameplayConfiguration Configuration => Services.Resolve<GameplayConfiguration>();

    public static WukongInputApi Input => Services.Resolve<WukongInputApi>();
    public static WukongConsoleApi Console => Services.Resolve<WukongConsoleApi>();
    public static WukongFileApi Files => Services.Resolve<WukongFileApi>();
    public static IWukongSaveRelay Saves => Services.Resolve<IWukongSaveRelay>();
    public static WukongEventApi Events => Services.Resolve<WukongEventApi>();
    public static WukongClientApi Client => Services.Resolve<WukongClientApi>();
    public static WukongWidgetManager Widgets => Services.Resolve<WukongWidgetManager>();
    public static WukongLocalApi Local => Services.Resolve<WukongLocalApi>();
}