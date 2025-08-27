using System.Collections.Generic;
using UnrealEngine.Runtime;

namespace WukongMp.Api.Configuration
{
    internal static class LevelSpawnConfig
    {
        private static readonly Dictionary<int, LevelSpawnData> Configurations = new()
        {
            { 0, new LevelSpawnData(61, 17, 6101, new FVector(-11146, -3229, 6507), 3000) },
            { 1, new LevelSpawnData(98, 5, 9803, new FVector(78686, -22648, 14646)) },
            { 2, new LevelSpawnData(98, 7, 9802, new FVector(-48308, -92826, 5658)) },
            { 3, new LevelSpawnData(20, 21, 2010, new FVector(-82034, 26036, -10158), 3000) },
            { 4, new LevelSpawnData(30, 6, 3004, new FVector(399750, -346464, -17503)) },
            { 5, new LevelSpawnData(98, 11, 9801, new FVector(-128621, -36775, -4407)) },
            { 6, new LevelSpawnData(50, 7, 5009, new FVector(50232, -5521, 26267), 3000) },
            { 7, new LevelSpawnData(50, 2, 5008, new FVector(-28007, 93707, 39560), 3000) },
        };

        public static LevelSpawnData GetLevelSpawnData(int levelId)
        {
            return Configurations[levelId];
        }

        public static LevelSpawnData GetCurrentLevelSpawnData()
        {
            return GetLevelSpawnData(LaunchParameters.Instance.LevelId);
        }
    }
}