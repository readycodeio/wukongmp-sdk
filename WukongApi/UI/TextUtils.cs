namespace WukongApi.UI
{
    public static class TextUtils
    {
        public static string GetReadyText(int connectedPlayersCount)
        {
            if (connectedPlayersCount == 0)
            {
                return Texts.PressToPlayWithBots;
            }
            return Texts.PressToBeReady;
        }
    }
}
