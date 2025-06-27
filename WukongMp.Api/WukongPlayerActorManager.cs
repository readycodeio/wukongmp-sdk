 using System.Linq;
using b1;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Idents;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Relay.Common.Wukong.ECS.Components;
using WukongMp.Api.ECS.Jobs;
using WukongMp.Api.Old;
using WukongMp.Api.Old.State;
using WukongMp.Api.UI;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api;

// NOTE: only deals with local pawn spawning; shouldn't ideally send anything over the network or cause any network-related
// effects. However this is difficult to guarantee at the moment since there were a lot of dependencies in the original code.
public class WukongPlayerPawnManager(Store world, WukongPlayerRegistry playerRegistry, WukongPlayerModeManager modeManager)
{
    public void RemovePlayerPawn(PlayerState playerState)
    {
        if (playerState.MarkerActor != null)
        {
            BGU_UnrealWorldUtil.DestroyActor(playerState.MarkerActor);
        }

        if (playerState.Pawn != null)
        {
            BGU_UnrealWorldUtil.DestroyActor(playerState.Pawn);
        }

        LobbyStatusWidget.Instance.RemovePlayerFromTeams(playerState.NickName);
        UpdateConnectedCount();
        LobbyStatusWidget.Instance.SetReadyCount(playerRegistry.AllConnectedPlayers.Count(x => x.IsReadyForPvP));
        CoopStatusWidget.Instance.RemovePlayer(playerState.NickName);

        world.Query<TamerComponent>().Each(new ClearPlayerTamersJob(playerState.PlayerId));
    }

    public void UpdateConnectedCount()
    {
        LobbyStatusWidget.Instance.SetConnectedCount(playerRegistry.ConnectedPlayers.Count + 1);
        CoopStatusWidget.Instance.SetConnectedCount(playerRegistry.ConnectedPlayers.Count + 1);
        GameMessageWidget.Instance.SetSecondText(TextUtils.GetReadyText(playerRegistry.ConnectedPlayers.Count, playerRegistry.LocalPlayerState.IsReadyForPvP));
    }

    public PlayerState? AddPlayerPawn(PlayerId playerId)
    {
        var playerState = SpawningUtils.SpawnCloneForPlayer(playerId);

        if (playerState != null)
        {
            MarkerUtils.CreateMarkerForCharacter(playerState); // 3D marker above player
            playerRegistry.RegisterPlayer(playerState);
            UpdateConnectedCount();

            modeManager.UpdatePlayerTeamUi(playerState);
        }

        return playerState;
    }
}