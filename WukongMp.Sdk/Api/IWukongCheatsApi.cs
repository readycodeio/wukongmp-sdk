using WukongMp.Sdk.Entities;

namespace WukongMp.Sdk.Api;

public interface IWukongCheatsApi
{
    bool CheatsAllowed { get; }
    void ToggleInfiniteMana();
    void ResetCooldowns();
    void ResetMana();
    void SetSpritCooldownTime(ReadyMainCharacter mainEntity, float spiritCooldownTime);
    void ToggleInfiniteVessel(ReadyMainCharacter mainEntity);
    void ToggleInfiniteTransform(ReadyMainCharacter mainEntity);
    void ToggleNoSkillsCooldown(ReadyMainCharacter mainEntity);
}