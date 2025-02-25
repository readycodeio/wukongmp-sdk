namespace WukongApi.UI
{
    public class LobbyStatusWidget : GameWidgetBase
    {
        public LobbyStatusWidget() : base(Constants.LobbyStatusWidgetName) { }

        public void SetConnectedCount(int count)
        {
            _gameWidget?.CallFunctionByNameWithArguments($"SetConnectedCount {count}", true);
        }

        public void SetMaxConnectedCount(int count)
        {
            _gameWidget?.CallFunctionByNameWithArguments($"SetMaxConnectedCount {count}", true);
        }

        public void SetReadyCount(int count)
        {
            _gameWidget?.CallFunctionByNameWithArguments($"SetReadyCount {count}", true);
        }

        public void UpdatePlayerTeam(string nickName, int teamId)
        {
            Logging.LogWarning($"Updating player {nickName} to team {teamId}");
            if (teamId == Constants.AvailableTeamIds[0])
            {
                RemoveFromTeam1(nickName);
                RemoveFromTeam2(nickName);
                AddToTeam1(nickName);
            }
            else if (teamId == Constants.AvailableTeamIds[1])
            {
                RemoveFromTeam1(nickName);
                RemoveFromTeam2(nickName);
                AddToTeam2(nickName);
            }
        }

        private void AddToTeam1(string playerName)
        {
            _gameWidget?.CallFunctionByNameWithArguments($"AddToTeam1 {playerName}", true);
        }

        private void RemoveFromTeam1(string playerName)
        {
            _gameWidget?.CallFunctionByNameWithArguments($"RemoveFromTeam1 {playerName}", true);
        }

        private void AddToTeam2(string playerName)
        {
            _gameWidget?.CallFunctionByNameWithArguments($"AddToTeam2 {playerName}", true);
        }

        private void RemoveFromTeam2(string playerName)
        {
            _gameWidget?.CallFunctionByNameWithArguments($"RemoveFromTeam2 {playerName}", true);
        }
    }
}
