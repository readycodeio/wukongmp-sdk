namespace WukongMp.Sdk.Api;

public interface IWukongCheatsApi
{
    bool CheatsAllowed { get; }
    void ToggleInfiniteMana();
    void ResetCooldowns();
    void ResetMana();
}