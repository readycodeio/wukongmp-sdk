using b1;
using BtlShare;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.Configuration;
using WukongMp.Api.ECS.Components;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Systems;

public class CreateLocalMainCharacterEntitySystem(WukongPlayerState playerState, ILogger logger) : BaseSystem
{
    protected override void OnUpdateGroup()
    {
        var playerEntity = playerState.LocalPlayer;
        if (playerEntity == null)
            return;
        
        var pawn = GameUtils.GetControlledPawn();
        var mainEntity = playerState.LocalMainCharacter;

        if (!pawn.IsNullOrDestroyed() && mainEntity == null)
        {
            // NOTE: Controlled pawn exists, but no corresponding ECS entity, need to be created
            CreateLocalMainEntity(pawn, playerEntity.Value);
        }
    }
    
    private void CreateLocalMainEntity(BGUPlayerCharacterCS pawn, PlayerEntity playerEntity)
    {
        logger.LogDebug("Setting initial player properties");

        ref var player = ref playerEntity.GetState();
        
        var mainEntity = playerState.CreateLocalMainCharacter();
        ref var mainComp = ref mainEntity.GetState();
        
        mainComp.Location = pawn.GetActorLocation();
        mainComp.Rotation = pawn.GetActorRotation();

        var attrContainer = BGU_DataUtil.GetReadOnlyData<IBUC_AttrContainer, BUC_AttrContainer>(pawn);
        mainComp.Hp = attrContainer.GetFloatValue(EBGUAttrFloat.Hp);
        mainComp.HpMaxBase = attrContainer.GetFloatValue(EBGUAttrFloat.HpMaxBase);

        foreach (var attr in Constants.SyncedAttributes)
        {
            var value = attrContainer.GetFloatValue(attr);
            mainComp.Attributes[attr] = value;
        }

        mainComp.CharacterNickName = player.NickName;

        var eq = EquipmentUtils.GetCurrentEquipmentStateForActor(pawn);
        mainComp.Equipment = eq;

        var pawnTeamId = pawn.GetTeamIDInCS();
        mainEntity.SetTeam(new TeamComponent()
        {
            TeamId = pawnTeamId,
        });
        
#if TESTING
        BUC_SpeedCtrlData? speedCtrlData = BGU_DataUtil.GetUnPersistentReadOnlyData<IBUC_SpeedCtrlData, BUC_SpeedCtrlData>(GameUtils.GetControlledPawn()) as BUC_SpeedCtrlData;
        speedCtrlData?.SetSpeedInfo(10000, 10000, 10000);
#endif

        Logging.LogDebug("Finished setting initial player properties");
    }
}
