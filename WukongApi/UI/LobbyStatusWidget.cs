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

        public void UpdatePlayerTeam(string nickName, int teamId, bool isSpectator)
        {
            RemovePlayerFromTeams(nickName);
            if (isSpectator)
            {
                AddSpectator(nickName);
            }
            else if (teamId == Constants.AvailableTeamIds[0])
            {
                AddToTeam1(nickName);
            }
            else if (teamId == Constants.AvailableTeamIds[1])
            {
                AddToTeam2(nickName);
            }
        }

        public void RemovePlayerFromTeams(string nickName)
        {
            RemoveFromTeam1(nickName);
            RemoveFromTeam2(nickName);
            RemoveSpectator(nickName);
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

        private void AddSpectator(string playerName)
        {
            GameWidget?.CallFunctionByNameWithArguments($"AddSpectator {playerName}", true);
        }

        private void RemoveSpectator(string playerName)
        {
            GameWidget?.CallFunctionByNameWithArguments($"RemoveSpectator {playerName}", true);
        }

        protected override void PostInitialize() { }
    }
}
