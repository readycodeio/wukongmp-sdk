using Microsoft.Extensions.Logging;
using WukongMp.Api;
using WukongMp.PvP.Chat;
using WukongMp.PvP.Configuration;

namespace WukongMp.PvP
{
    internal class PvpDI
    {
        public static PvpDI Instance { get; } = new();

        public DI DI { get; private set; } = null!;

        public PvpChatter PvpChatter { get; private set; } = null!;
        public PvpGameplayConfiguration GameplayConfiguration { get; private set; } = null!;

        public void Init(DI wukongDI)
        {
            wukongDI.Logger.LogDebug("Initializing PvP DI...");

            DI = wukongDI;

            var chatter = PvpChatter = new PvpChatter(DI.Chatter, DI.PlayerState, DI.Rpc);
            var gameplayConfig = GameplayConfiguration = new PvpGameplayConfiguration(DI.GameplayConfiguration);
        }
    }
}
