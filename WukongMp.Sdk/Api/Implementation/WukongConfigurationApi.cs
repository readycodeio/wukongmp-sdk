using WukongMp.Api;
using WukongMp.Api.Configuration;

namespace WukongMp.Sdk.Api.Implementation;

internal sealed class WukongConfigurationApi(GameplayConfiguration configuration, LaunchParameters launchParameters) : IWukongConfigurationApi
{
    public bool IsSupportMultiLockEnabled
    {
        get => configuration.IsSupportMultiLockEnabled;
        set => configuration.IsSupportMultiLockEnabled = value;
    }

    public bool IsStrongDamageImmueEnabled
    {
        get => configuration.IsStrongDamageImmueEnabled;
        set => configuration.IsStrongDamageImmueEnabled = value;
    }

    public bool EnableCustomCameraArmLength
    {
        get => configuration.EnableCustomCameraArmLength;
        set => configuration.EnableCustomCameraArmLength = value;
    }

    public bool DeleteDestroyedTamersFromEcs
    {
        get => configuration.DeleteDestroyedTamersFromEcs;
        set => configuration.DeleteDestroyedTamersFromEcs = value;
    }

    public bool SyncTamerTeamFromGameToEcs
    {
        get => configuration.SyncTamerTeamFromGameToEcs;
        set => configuration.SyncTamerTeamFromGameToEcs = value;
    }

    public string GetLaunchParameter(string key, string defaultValue)
    {
        return launchParameters.GetParameterOrDefault(key, defaultValue);
    }
}