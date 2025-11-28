namespace WukongMp.PvP.Configuration;

public static class PvpConstants
{
    public const float PvpStartingRadius = 500;
    public const float PvpMonsterRadius = 1000;
    public const int CharacterArchiveId = 10;
    public const int WorldArchiveId = 0;

    public const int DrawTeamId = 9999;

    public const int CountdownSeconds = 5;
    public const int RedTeamId = -9999;
    public const int BlueTeamId = -9998;
    public const int SpectatorTeamId = -9997;
    public static readonly int[] CompetingTeamIds = [RedTeamId, BlueTeamId];
    public static readonly int[] AllTeamIds = [RedTeamId, BlueTeamId, SpectatorTeamId];
}