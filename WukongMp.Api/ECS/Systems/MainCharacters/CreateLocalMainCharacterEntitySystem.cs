using b1;
using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;
using ReadyM.Relay.Client.State;
using ReadyM.Wukong.Common.ECS.Components;
using WukongMp.Api.ECS.Entities;
using WukongMp.Api.Mapping;
using WukongMp.Api.State;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.ECS.Systems.MainCharacters;

/// <summary>
/// Creates the MainCharacterEntity corresponding to the locally controlled pawn
/// </summary>
public class CreateLocalMainCharacterEntitySystem(ClientState clientState, WukongPlayerState playerState, WukongEventBus eventBus, ILogger logger) : BaseSystem
{
    protected override void OnUpdateGroup()
    {
        if (!eventBus.IsGameplayLevel)
            return;

        var playerEntity = playerState.LocalPlayerEntity;
        if (playerEntity == null)
            return;
        var areaId = clientState.CurrentAreaId;
        if (areaId == null)
            return;

        var pawn = GameUtils.GetControlledPawn();
        var mainEntity = playerState.LocalMainCharacter;

        if (!pawn.IsNullOrDestroyed() && mainEntity == null)
        {
            logger.LogDebug("CREATING LOCAL MAIN CHARACTER ENTITY");
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
        ref var localMainComp = ref mainEntity.GetLocalState();

        logger.LogDebug("Local main character pawn: {Pawn}", pawn.PathName);

        mainEntity.SetPawn(pawn, false);

        mainComp.Location = pawn.GetActorLocation().ToVector3();
        mainComp.Rotation = pawn.GetActorRotation().ToVector3();

        var attrContainer = BGU_DataUtil.GetReadOnlyData<BUC_AttrContainer>(pawn);

        if (DI.Instance.MappedField.CanLoadFromGame<MainCharacterComponent>(mainEntity, out var load))
        {
            load.LoadFromGame(MainCharacterComponent.Fields.HpMaxBase.In<BUC_AttrContainer>(), attrContainer);
            load.LoadFromGame(MainCharacterComponent.Fields.Hp.In<BUC_AttrContainer>(), attrContainer);
            load.LoadFromGame(MainCharacterComponent.Fields.Attributes.In<BUC_AttrContainer>(), attrContainer);
            load.LoadFromGame(MainCharacterComponent.Fields.Equipment.In<BGUCharacterCS>(), pawn);
        }

        mainComp.CharacterNickName = player.NickName;

        var pawnTeamId = pawn.GetTeamIDInCS();
        mainEntity.SetTeam(new TeamComponent
        {
            TeamId = pawnTeamId,
        });

        localMainComp.IsPlayerSynced = true;
        playerState.InvokeMainCharacterEntityInitialized(mainEntity);

        Logging.LogDebug("Finished setting initial player properties");
    }
}