namespace WukongApi.UI
{
    public static class TextUtils
    {
        public static string GetReadyText(int playersCount, bool isReady)
        {
            if (playersCount == 0)
            {
                return isReady ? Resources.Texts.PressToCancelMatch : Resources.Texts.PressToPlayWithBots;
            }
            return isReady ? Resources.Texts.PressToBeNotReady : Resources.Texts.PressToBeReady;
        }
    }
}
