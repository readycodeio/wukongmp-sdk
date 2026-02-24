using WukongMp.Api.State;

namespace WukongMp.Api.Command;

public static class ConsoleCommandUtils
{
    extension(WukongPlayerState playerState)
    {
        public string NickName
            => playerState.LocalPlayerEntity?.GetState().NickName ?? "";
    }
}