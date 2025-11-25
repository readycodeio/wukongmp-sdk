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
    public static readonly int[] AvailableTeamIds = [RedTeamId, BlueTeamId];

    public const string FeetCameraLockNode = "CAMERA_LOCK_Root";
}