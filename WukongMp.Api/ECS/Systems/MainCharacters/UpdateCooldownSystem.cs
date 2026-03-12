using b1;
using BtlShare;
using Friflo.Engine.ECS.Systems;
using UnrealEngine.Runtime;
using WukongMp.Api.Configuration;
using WukongMp.Api.State;

namespace WukongMp.Api.ECS.Systems.MainCharacters;

internal class UpdateCooldownSystem(WukongPlayerState playerState, WukongEventBus eventBus, WukongAreaState areaState) : BaseSystem
{
    private float _vigorRegenAccumulator;

    protected override void OnUpdateGroup()
    {
        if (!eventBus.IsGameplayLevel)
            return;

        if (areaState.CurrentArea is { Room.CheatsAllowed: false })
            return;

        var mainCharacterEntity = playerState.LocalMainCharacter;
        if (mainCharacterEntity == null)
            return;

        ref var localMainComp = ref mainCharacterEntity.Value.GetLocalState();

        if (!localMainComp.SpiritCooldownEnabled)
            return;

        var localPawn = mainCharacterEntity.Value.Pawn;
        if (localPawn == null)
            return;

        var magicallyChangeData = BGU_DataUtil.GetUnPersistentReadOnlyData<BUC_MagicallyChangeData>(localPawn);

        if (magicallyChangeData.DurMagicallyChange)
        {
            _vigorRegenAccumulator = 0f;
            return;
        }

        var currentVigorValue = BGUFunctionLibraryCS.BGUGetFloatAttr(localPawn, EBGUAttrFloat.VigorEnergy);
        if (currentVigorValue.Equals(0, Constants.FloatComparisonTolerance))
        {
            _vigorRegenAccumulator = 0f;
        }

        var events = BUS_EventCollectionCS.Get(localPawn);
        if (localMainComp.SpiritCooldownTime.Equals(0, Constants.FloatComparisonTolerance))
        {
            localMainComp.ShouldSetSpiritCooldown = true;
            events?.Evt_SetAttrFloat.Invoke(EBGUAttrFloat.VigorEnergy, BGUFunctionLibraryCS.BGUGetFloatAttr(localPawn, EBGUAttrFloat.VigorEnergyMax));
            localMainComp.ShouldSetSpiritCooldown = false;
            return;
        }

        if (_vigorRegenAccumulator > localMainComp.SpiritCooldownTime)
            return;

        _vigorRegenAccumulator += Tick.deltaTime;
        var newVigorValue = FMath.Lerp(0, BGUFunctionLibraryCS.BGUGetFloatAttr(localPawn, EBGUAttrFloat.VigorEnergyMax), FMath.Clamp(_vigorRegenAccumulator / localMainComp.SpiritCooldownTime, 0f, 1f));
        if (newVigorValue > currentVigorValue)
        {
            localMainComp.ShouldSetSpiritCooldown = true;
            events?.Evt_SetAttrFloat.Invoke(EBGUAttrFloat.VigorEnergy, newVigorValue);
            localMainComp.ShouldSetSpiritCooldown = false;
        }
    }
}
