using WukongApi.State;

namespace WukongApi.UI
{
    public class LobbyStatusWidget : GameWidgetBase
    {
        public LobbyStatusWidget() : base(Constants.LobbyStatusWidgetName) { }

        public void SetConnectedCount(int count)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetConnectedCount {count}", true);
        }

        public void SetMaxConnectedCount(int count)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetMaxConnectedCount {count}", true);
        }

        public void SetReadyCount(int count)
        {
            GameWidget?.CallFunctionByNameWithArguments($"SetReadyCount {count}", true);
        }

        public void UpdatePlayerTeam(PlayerState playerState, int teamId)
        {
            RemovePlayerFromTeams(playerState);
            if (teamId == Constants.AvailableTeamIds[0])
            {
                AddToTeam1(playerState.NickName);
            }
            else if (teamId == Constants.AvailableTeamIds[1])
            {
                AddToTeam2(playerState.NickName);
            }
        }

        public void RemovePlayerFromTeams(PlayerState playerState)
        {
            RemoveFromTeam1(playerState.NickName);
            RemoveFromTeam2(playerState.NickName);
        }

        private void AddToTeam1(string playerName)
        {
            GameWidget?.CallFunctionByNameWithArguments($"AddToTeam1 {playerName}", true);
        }

        private void RemoveFromTeam1(string playerName)
        {
            GameWidget?.CallFunctionByNameWithArguments($"RemoveFromTeam1 {playerName}", true);
        }

        private void AddToTeam2(string playerName)
        {
            GameWidget?.CallFunctionByNameWithArguments($"AddToTeam2 {playerName}", true);
        }

        private void RemoveFromTeam2(string playerName)
        {
            GameWidget?.CallFunctionByNameWithArguments($"RemoveFromTeam2 {playerName}", true);
        }

        protected override void PostInitialize() { }
    }
}
