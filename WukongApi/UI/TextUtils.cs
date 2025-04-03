namespace WukongApi.UI
{
    public static class TextUtils
    {
        public static string GetReadyText(WukongClient Photon)
        {
            if (Photon.ConnectedPlayers.Count == 0)
            {
                return Photon.LocalPlayerState.IsReadyForPvP ? Texts.PressToCancelMatch : Texts.PressToPlayWithBots;
            }
            return Photon.LocalPlayerState.IsReadyForPvP ? Texts.PressToBeNotReady : Texts.PressToBeReady;
        }
    }
}
