using System.Numerics;
using ReadyM.Api.Command;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Api.WukongUtils;
using WukongMp.Sdk.Api;

namespace WukongMp.Sdk.Command;

public class DebugApiCommandRegistration(
    WukongClientApi clientApi,
    WukongLocalApi localApi
) : IConsoleCommandRegistration
{
    public void RegisterCommands(ConsoleCommandRegistry registry)
    {
        registry.AddCommand("create_main_character", ConsoleCommand.Create(CreateMainCharacter, isDebugOnly: true));
        registry.AddCommand("create_tamer", ConsoleCommand.Create(CreateTamer, isDebugOnly: true));
        registry.AddCommand("show_all_main_characters", ConsoleCommand.Create(ShowAllMainCharacters, isDebugOnly: true));
        registry.AddCommand("show_main_character", ConsoleCommand.Create(ShowMainCharacter, isDebugOnly: true));
    }

    private void CreateMainCharacter(Vector3? location = null, Vector3? rotation = null, int? teamId = null)
    {
        if (clientApi.LocalMainCharacter?.Pawn != null)
        {
            var pawn = clientApi.LocalMainCharacter.Value.Pawn;

            if (location == null)
            {
                location = SpawningUtils.CalculateSpawnLocation(pawn.GetActorLocation(), pawn.GetActorForwardVector()).ToVector3();
            }

            if (rotation == null)
            {
                rotation = Vector3.Zero;
            }

            if (teamId == null)
            {
                teamId = clientApi.LocalMainCharacter.Value.TeamId;
            }
        }

        if (location == null)
            return;
        if (rotation == null)
            return;
        if (teamId == null)
            return;

        clientApi.CreateMainCharacter(location.Value, rotation.Value, teamId.Value);
    }

    private void CreateTamer(Ident ident, Vector3? location = null, Vector3? rotation = null, int? teamId = null)
    {
        if (clientApi.LocalMainCharacter?.Pawn != null)
        {
            var pawn = clientApi.LocalMainCharacter.Value.Pawn;

            if (location == null)
            {
                location = SpawningUtils.CalculateSpawnLocation(pawn.GetActorLocation(), pawn.GetActorForwardVector()).ToVector3();
            }

            if (rotation == null)
            {
                rotation = Vector3.Zero;
            }

            if (teamId == null)
            {
                teamId = clientApi.LocalMainCharacter.Value.TeamId;
            }
        }

        if (location == null)
            return;
        if (rotation == null)
            return;
        if (teamId == null)
            return;

        var tamerKind = TamerConstants.GetTamerKind(ident.Name);
        clientApi.CreateTamer(location.Value, rotation.Value, tamerKind, teamId.Value);
    }

    private void ShowAllMainCharacters()
    {
        foreach (var mainCharacter in clientApi.AllMainCharacters)
        {
            ShowMainCharacter(mainCharacter);
        }
    }

    private void ShowMainCharacter(ReadyMainCharacter mainCharacter)
    {
        localApi.WriteConsoleMessage($"MainCharacter: location={mainCharacter.Location}, rotation={mainCharacter.Rotation}, teamId={mainCharacter.TeamId}");
    }
}