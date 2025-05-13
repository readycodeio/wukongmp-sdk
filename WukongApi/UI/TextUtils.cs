using WukongApi.Resources;

namespace WukongApi.UI
{
    public static class TextUtils
    {
        public static string GetReadyText(int playersCount, bool isReady)
        {
            if (playersCount == 0)
            {
                return isReady ? Texts.PressToCancelMatch : Texts.PressToPlayWithBots;
            }
            return isReady ? Texts.PressToBeNotReady : Texts.PressToBeReady;
        }
    }
}
