using b1;
using B1UI;
using ReadyM.Api.Command;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api.WukongUtils;
using WukongMp.Sdk.Api;

namespace WukongMp.Coop.Commands;

public static class CoopCommandRegistrations
{
    public static void RegisterCommands(IWukongConsoleApi consoleApi)
    {
        consoleApi.AddCommand("cutscene", ConsoleCommand.Create(PlayCutscene, true));
        consoleApi.AddCommand("teleport", ConsoleCommand.Create(Teleport, true));
        consoleApi.AddCommand("openlevel", ConsoleCommand.Create(OpenLevel, true));
    }

    private static void PlayCutscene(int seqId)
    {
        GSG.GMSvc.GMTeleportToTargetSequence(seqId);
    }

    private static void Teleport(int birthPointId)
    {
        BPS_EventCollectionCS.Get(GameUtils.GetControlledPawn()?.PlayerState).Evt_BPS_TeleportTo.Invoke(
            ETeleportTypeV2.RebirthPointTeleportOnly,
            new TeleportParam_RebirthPoint { RebirthPointId = birthPointId },
            EPlayerTeleportReason.RebirthPoint);
    }

    private static void OpenLevel(string name)
    {
        UGameplayStatics.OpenLevel(GameUtils.GetWorld(), new FName(name));
    }
}