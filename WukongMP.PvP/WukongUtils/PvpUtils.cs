using b1;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;
using WukongMp.Api;
using WukongMp.Api.Configuration;
using WukongMp.Api.Resources;
using WukongMp.Api.WukongUtils;
using WukongMp.PvP.Configuration;

namespace WukongMp.PvP.WukongUtils;

public static class PvpUtils
{
    private const string RedTeamColor = "(R=1,G=0.3,B=0.3)";
    private const string BlueTeamColor = "(R=0.3,G=0.3,B=1)";

    public static void ShowPvPCountDown()
    {
        var areaState = DI.Instance.AreaState;
        var areaEntity = areaState.CurrentArea;
        if (areaEntity == null || !areaState.PvpState.HasValue)
            return;

        ref var room = ref areaEntity.Value.GetRoom();
        var current = areaState.PvpState.Value.CurrentRound;
        var total = room.TournamentRounds;
        UiUtils.ShowTip(string.Format(Texts.RoundCount, current, total), true);
    }

    public static string GetTeamColorString(int teamId)
    {
        if (teamId == Constants.RedTeamId)
            return RedTeamColor;
        if (teamId == Constants.BlueTeamId)
            return BlueTeamColor;
        return "";
    }

    public static string GetLocalizedTeamName(int teamId)
    {
        if (teamId == Constants.RedTeamId)
            return Texts.RedTeam;
        if (teamId == Constants.BlueTeamId)
            return Texts.BlueTeam;
        return "";
    }

    public static int GetOppositeTeam(int teamId)
    {
        if (teamId == Constants.DrawTeamId)
            return teamId;
        return teamId == Constants.RedTeamId ? Constants.BlueTeamId : Constants.RedTeamId;
    }

    public static void CreatePvpStateEntity()
    {
        DI.Instance.AreaState.PvpStateEntity = DI.Instance.ClientNetEntity.CreateNetworkedAreaEntity(DI.Instance.ArchetypeRegistration.PvPStateSingletonArchetype).Entity;
    }

    public static FVector GetSpawnPosition(BGUCharacterCS? pawn, int playerId, int maxPlayersCount)
    {
        float angle = playerId / (float)maxPlayersCount * 2f * FMath.PI;
        float x = FMath.Cos(angle) * Constants.PvpStartingRadius;
        float y = FMath.Sin(angle) * Constants.PvpStartingRadius;

        var levelData = LevelSpawnConfig.GetCurrentLevelSpawnData();
        var baseLocation = levelData.PvpStartingLocation + new FVector(x, y, 0f);

        return AdjustSpawnLocation(pawn, baseLocation);
    }

    public static FVector AdjustSpawnLocation(ABGUCharacter? CharacterCS, FVector InTargetLocation)
    {
        // TODO: For Heart of Birthstone map adjustment resulted in falling - invisible collision. So it is disabled for now.
        if (LaunchParameters.Instance.LevelId == 0)
        {
            return InTargetLocation;
        }

        FVector result = InTargetLocation;
        if (CharacterCS == null)
        {
            return result;
        }

        UCapsuleComponent? uCapsuleComponent = CharacterCS.GetRootComponent() as UCapsuleComponent;
        if (uCapsuleComponent == null)
        {
            return result;
        }

        float scaledCapsuleHalfHeight = uCapsuleComponent.GetScaledCapsuleHalfHeight();
        float scaledCapsuleHalfHeight2 = uCapsuleComponent.GetScaledCapsuleHalfHeight();
        float num = 2.4f;
        FVector start = InTargetLocation + FVector.UpVector * scaledCapsuleHalfHeight * 2.0;
        FVector end = InTargetLocation - FVector.UpVector * scaledCapsuleHalfHeight * 2.0;
        if (UGSE_TraceFuncLib.CharacterCapsuleTraceSingleByProfile(GameUtils.GetWorld(), start, end, scaledCapsuleHalfHeight2, scaledCapsuleHalfHeight, B1GlobalFNames.Pawn, bTraceComplex: false, CharacterCS, out var OutHitLocation))
        {
            result = OutHitLocation + num + FVector.UpVector * scaledCapsuleHalfHeight;
        }

        return result;
    }
}