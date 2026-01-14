using b1;
using BtlShare;
using Friflo.Engine.ECS.Systems;
using UnrealEngine.Runtime;
using WukongMp.Api.Chat;
using WukongMp.Api.Configuration;
using WukongMp.Api.State;

namespace WukongMp.Api.ECS.Systems.MainCharacters;

public class UpdateCooldownSystem(WukongPlayerState playerState, WukongEventBus eventBus, WukongAreaState areaState) : BaseSystem
{
    private float _vigorRegenAccumulator = 0f;

    protected override void OnUpdateGroup()
    {
        if (!eventBus.IsGameplayLevel)
            return;

        if (areaState.CurrentArea.HasValue && !areaState.CurrentArea.Value.Room.CheatsAllowed)
            return;

        var mainCharacterEntity = playerState.LocalMainCharacter;
        if (mainCharacterEntity == null)
            return;

        ref var localMainComp = ref mainCharacterEntity.Value.GetLocalState();

        var localPawn = localMainComp.Pawn;
        if (localPawn == null)
            return;

        var magicallyChangeData = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_MagicallyChangeData>(localPawn);

        if (magicallyChangeData.DurMagicallyChange)
        {
            _vigorRegenAccumulator = 0f;
            return;
        }

        var events = BUS_EventCollectionCS.Get(localPawn);
        if (localMainComp.SpiritCooldownTime.Equals(0, Constants.FloatComparisonTolerance))
        {
            events?.Evt_SetAttrFloat.Invoke(EBGUAttrFloat.VigorEnergy, BGUFunctionLibraryCS.BGUGetFloatAttr(localPawn, EBGUAttrFloat.VigorEnergyMax));
            return;
        }

        if (_vigorRegenAccumulator > localMainComp.SpiritCooldownTime)
            return;

        _vigorRegenAccumulator += Tick.deltaTime;
        var newVigorValue = FMath.Lerp(0, BGUFunctionLibraryCS.BGUGetFloatAttr(localPawn, EBGUAttrFloat.VigorEnergyMax), FMath.Clamp(_vigorRegenAccumulator / localMainComp.SpiritCooldownTime, 0f, 1f));
        localMainComp.ShouldSetSpiritCooldown = true;
        events?.Evt_SetAttrFloat.Invoke(EBGUAttrFloat.VigorEnergy, newVigorValue);
        localMainComp.ShouldSetSpiritCooldown = false;
    }
}
