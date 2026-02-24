using b1;
using B1UI;
using ReadyM.Api.Command;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Coop.Command
{
    public class CoopCommandRegistration : IConsoleCommandRegistration
    {
        public void RegisterCommands(ConsoleCommandRegistry registry)
        {
            registry.AddCommand("play", ConsoleCommand.Create(PlayCutscene, true));
            registry.AddCommand("teleport", ConsoleCommand.Create(Teleport, true));
            registry.AddCommand("openlevel", ConsoleCommand.Create(OpenLevel, true));
        }
        
        private void PlayCutscene(int seqId)
        {
            GSG.GMSvc.GMTeleportToTargetSequence(seqId);
        }

        private void Teleport(int birthPointId)
        {
            BPS_EventCollectionCS.Get(GameUtils.GetControlledPawn()?.PlayerState).Evt_BPS_TeleportTo.Invoke(ETeleportTypeV2.RebirthPointTeleportOnly, new TeleportParam_RebirthPoint
            {
                RebirthPointId = birthPointId
            }, EPlayerTeleportReason.RebirthPoint);
        }

        private void OpenLevel(string name)
        {
            UGameplayStatics.OpenLevel(GameUtils.GetWorld(), new FName(name));
        }
    }
}
