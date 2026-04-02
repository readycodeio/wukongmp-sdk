namespace WukongMp.Sdk.Api;

public interface IWukongConfigurationApi
{
    public bool IsSupportMultiLockEnabled { get; set; }
    public bool IsStrongDamageImmueEnabled { get; set; }
    public bool EnableCustomCameraArmLength { get; set; }
    public bool DeleteDestroyedTamersFromEcs { get; set; }
    public bool SyncTamerTeamFromGameToEcs { get; set; }

    string GetLaunchParameter(string key, string defaultValue);
}