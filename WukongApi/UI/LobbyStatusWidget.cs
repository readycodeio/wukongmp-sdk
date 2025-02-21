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

        public void AddToTeam1(string playerName)
        {
            _gameWidget?.CallFunctionByNameWithArguments($"AddToTeam1 {playerName}", true);
        }

        public void RemoveFromTeam1(string playerName)
        {
            _gameWidget?.CallFunctionByNameWithArguments($"RemoveFromTeam1 {playerName}", true);
        }

        public void AddToTeam2(string playerName)
        {
            _gameWidget?.CallFunctionByNameWithArguments($"AddToTeam2 {playerName}", true);
        }

        public void RemoveFromTeam2(string playerName)
        {
            _gameWidget?.CallFunctionByNameWithArguments($"RemoveFromTeam2 {playerName}", true);
        }
    }
}
