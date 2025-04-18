using System.Collections.Generic;
using UnrealEngine.Runtime;

namespace WukongApi
{
    internal static class LevelSpawnConfig
    {
        private static readonly Dictionary<int, LevelSpawnData> Configurations = new()
        {
            { 0, new LevelSpawnData(new FVector(-11146, -3229, 6507)) },
            { 1, new LevelSpawnData(new FVector(78686, -22648, 14646)) },
            { 2, new LevelSpawnData(new FVector(-48308, -92826, 5658)) },
            { 3, new LevelSpawnData(new FVector(-81346, 26192, -10167)) },
            { 4, new LevelSpawnData(new FVector(399750, -346464, -17503)) },
            { 5, new LevelSpawnData(new FVector(-128621, -36775, -4407)) },
            { 6, new LevelSpawnData(new FVector(50232, -5521, 26267), 3000) },
            { 7, new LevelSpawnData(new FVector(-28007, -93707, 39560), 3000) },
        };

        public static LevelSpawnData GetLevelSpawnData(int levelId)
        {
            return Configurations[levelId];
        }

        public static LevelSpawnData GetCurrentLevelSpawnData()
        {
            return GetLevelSpawnData(CmdLineParams.Instance.LevelId);
        }
    }
}