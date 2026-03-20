using System;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Sdk.Api.Implementation;

namespace WukongMp.Sdk.Api;

/// <summary>
/// The main entry point for accessing Wukong's API.
/// Provides access to all of the various APIs and services that WukongMP offers.
/// </summary>
public static class WukongApi
{
    static WukongApi()
    {
        Services.RegisterSingleton<IWukongFileApi, WukongFileApi>();
        Services.RegisterSingleton<IWukongConsoleApi, WukongConsoleApi>();
        Services.RegisterSingleton<IWukongEventApi, WukongEventApi>();
        Services.RegisterSingleton<IWukongSynchronizationApi, WukongSynchronizationApi>();
        Services.RegisterSingleton<IWukongLocalApi, WukongLocalApi>();
        Services.RegisterSingleton<IWukongInputApi, WukongInputApi>();
        Services.RegisterSingleton<IWukongWidgetApi, WukongWidgetApi>();
    }

    public static IDependencyContainer Services => DI.Instance;

    [Obsolete("TODO: Make a more centralized configuration system.")]
    public static GameplayConfiguration Configuration => Services.Resolve<GameplayConfiguration>();
    public static IWukongInputApi Input => Services.Resolve<IWukongInputApi>();
    public static IWukongConsoleApi Console => Services.Resolve<IWukongConsoleApi>();
    public static IWukongFileApi Files => Services.Resolve<IWukongFileApi>();
    public static IWukongSaveApi Saves => Services.Resolve<IWukongSaveApi>();
    public static IWukongEventApi Events => Services.Resolve<IWukongEventApi>();
    public static IWukongSynchronizationApi Sync => Services.Resolve<IWukongSynchronizationApi>();
    public static IWukongWidgetApi Widgets => Services.Resolve<IWukongWidgetApi>();
    public static IWukongLocalApi Local => Services.Resolve<IWukongLocalApi>();
}