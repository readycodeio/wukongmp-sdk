using b1;
using B1UI;
using System;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api;
using WukongMp.Api.Command;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Coop.Command
{
    internal class CoopCommandConsole
    {
        private readonly WukongCommandConsole _wukongCommandConsole;

        public CoopCommandConsole(
        WukongCommandConsole wukongCommandConsole
    )
        {
            Logging.LogDebug("Initializing CoopCommandConsole");

            _wukongCommandConsole = wukongCommandConsole;

            SetupCommands();
        }

        private void SetupCommands()
        {
#if DEBUG
            _wukongCommandConsole.AddCommand("/play", new ConsoleCommand(PlayCutscene));
            _wukongCommandConsole.AddCommand("/teleport", new ConsoleCommand(Teleport));
            _wukongCommandConsole.AddCommand("/openlevel", new ConsoleCommand(OpenLevel));
#endif
        }

        private void PlayCutscene(ReadOnlyMemory<string> args)
        {
            if (args.Length == 1 && int.TryParse(args.Span[0], out var seqId))
            {
                GSG.GMSvc.GMTeleportToTargetSequence(seqId);
            }
        }

        private void Teleport(ReadOnlyMemory<string> args)
        {
            if (args.Length == 1 && int.TryParse(args.Span[0], out var birthpointId))
            {
                BPS_EventCollectionCS.Get(GameUtils.GetControlledPawn()?.PlayerState).Evt_BPS_TeleportTo.Invoke(ETeleportTypeV2.RebirthPointTeleportOnly, new TeleportParam_RebirthPoint
                {
                    RebirthPointId = birthpointId
                }, EPlayerTeleportReason.RebirthPoint);
            }
        }

        private void OpenLevel(ReadOnlyMemory<string> args)
        {
            if (args.Length == 1)
            {
                UGameplayStatics.OpenLevel(GameUtils.GetWorld(), new FName(args.Span[0]));
            }
        }
    }
}
