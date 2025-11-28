using b1;
using B1UI;
using System;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api;
using WukongMp.Api.Chat;
using WukongMp.Api.WukongUtils;

namespace WukongMp.PvP.Chat
{
    internal class CoopChatter
    {
        private readonly WukongChatter _wukongChatter;

        public CoopChatter(
        WukongChatter wukongChatter
    )
        {
            Logging.LogDebug("Initializing WukongChatter");

            _wukongChatter = wukongChatter;

            SetupCommands();
        }

        private void SetupCommands()
        {
#if DEBUG
            _wukongChatter.AddCommand("/play", new WukongChatterCommand(PlayCutscene));
            _wukongChatter.AddCommand("/teleport", new WukongChatterCommand(Teleport));
            _wukongChatter.AddCommand("/openlevel", new WukongChatterCommand(OpenLevel));
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
