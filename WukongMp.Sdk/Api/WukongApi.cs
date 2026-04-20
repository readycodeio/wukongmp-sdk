using ReadyM.Api.DI;
using WukongMp.Api;
using WukongMp.Sdk.Api.Implementation;

namespace WukongMp.Sdk.Api;

/// <summary>
/// The main entry point for accessing Wukong's API.
/// Provides access to all of the various APIs and services that WukongMP offers.
/// </summary>
public static class WukongApi
{
    internal static void RegisterApis()
    {
        Services.RegisterSingleton<IWukongSaveApi, WukongSelfHostedSaveApi>();
        Services.RegisterSingleton<IWukongFileApi, WukongFileApi>();
        Services.RegisterSingleton<IWukongConsoleApi, WukongConsoleApi>();
        Services.RegisterSingleton<IWukongChatApi, WukongChatApi>();
        Services.RegisterSingleton<IWukongPvpApi, WukongPvpApi>();
        Services.RegisterSingleton<IWukongCheatsApi, WukongCheatsApi>();
        Services.RegisterSingleton<IWukongEventApi, WukongEventApi>();
        Services.RegisterSingleton<IWukongSynchronizationApi, WukongSynchronizationApi>();
        Services.RegisterSingleton<IWukongLocalApi, WukongLocalApi>();
        Services.RegisterSingleton<IWukongInputApi, WukongInputApi>();
        Services.RegisterSingleton<IWukongWidgetApi, WukongWidgetApi>();
        Services.RegisterSingleton<IWukongConfigurationApi, WukongConfigurationApi>();
    }

    public static IDependencyContainer Services => DI.Instance;

    public static IWukongInputApi Input => Services.Resolve<IWukongInputApi>();
    public static IWukongConsoleApi Console => Services.Resolve<IWukongConsoleApi>();
    public static IWukongChatApi Chat => Services.Resolve<IWukongChatApi>();
    public static IWukongPvpApi PvP => Services.Resolve<IWukongPvpApi>();
    public static IWukongCheatsApi Cheats => Services.Resolve<IWukongCheatsApi>();
    public static IWukongFileApi Files => Services.Resolve<IWukongFileApi>();
    public static IWukongSaveApi Saves => Services.Resolve<IWukongSaveApi>();
    public static IWukongEventApi Events => Services.Resolve<IWukongEventApi>();
    public static IWukongSynchronizationApi Sync => Services.Resolve<IWukongSynchronizationApi>();
    public static IWukongWidgetApi Widgets => Services.Resolve<IWukongWidgetApi>();
    public static IWukongLocalApi Local => Services.Resolve<IWukongLocalApi>();
    public static IWukongConfigurationApi Configuration => Services.Resolve<IWukongConfigurationApi>();
}