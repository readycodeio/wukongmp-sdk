using System.Collections.Generic;
using UnrealEngine.Runtime;
using WukongMp.Api;

namespace WukongMp.PvP.Configuration
{
    internal static class LevelSpawnConfig
    {
        private static readonly Dictionary<int, LevelSpawnData> Configurations = new()
        {
            { 0, new LevelSpawnData(61, 17, 6101, new FVector(-11146, -3229, 6507), 3000) }, // Heart of Birthstone
            { 1, new LevelSpawnData(98, 5, 9803, new FVector(78686, -22648, 14646)) }, // Rhino Watch Slope
            { 2, new LevelSpawnData(98, 7, 9802, new FVector(-48308, -92826, 5658)) }, // Deer Sight Forest
            { 3, new LevelSpawnData(20, 21, 2010, new FVector(-82034, 26036, -10158), 3000) }, // Windseal Gate
            { 4, new LevelSpawnData(30, 6, 3004, new FVector(399750, -346464, -17503)) }, // Mirrormere
            { 5, new LevelSpawnData(98, 11, 9801, new FVector(-128621, -36775, -4407)) }, // Cooling Slope
            { 6, new LevelSpawnData(50, 7, 5009, new FVector(51132, -5121, 26367), 3000) }, // Fallen Furnance Crater
            { 7, new LevelSpawnData(50, 2, 5008, new FVector(-28007, 93707, 39560), 3000) },

            { 8, new LevelSpawnData(10, 26, 1008, new FVector(-73476, 29887, 10001.03), 3000) }, // Bodhi Peak
            { 9, new LevelSpawnData(70, 7, 7004, new FVector(107291, -142160, 12900.79), 2700) }, // Corridor of Fire and Ice - lava damage
            { 10, new LevelSpawnData(70, 2, 7002, new FVector(200524, -45683, 31919.74), 3000) }, // Purge Pit
            { 11, new LevelSpawnData(12, 27, 1013, new FVector(-94705, -22403, -8419.67), 2700) }, // Loong Claw Grove - no shrine
            { 12, new LevelSpawnData(20, 35, 2016, new FVector(128532, -21342, 4466.41), 2600) }, // Bottom of the Well
            { 13, new LevelSpawnData(30, 33, 3020, new FVector(-153095, -271407, -45556.81), 2500) }, // Watermelon Field
            { 14, new LevelSpawnData(40, 96, 4013, new FVector(146478, -66773, -3319.89), 3000) }, // Bonevault
            { 15, new LevelSpawnData(30, 39, 3026, new FVector(-216424, -127145, -19491.41), 3500) }, // Mahavira Hall
            { 16, new LevelSpawnData(80, 12, 8005, new FVector(12302, 38156, 7803.24), 3800) }, // Cloudnest Peak
            { 17, new LevelSpawnData(40, 21, 4028, new FVector(75507, 143275, 51508.68), 4000) }, // Court of Illumination
            { 18, new LevelSpawnData(31, 0, 3102, new FVector(-10046, 91668, -1617.68), 1500) }, // Zodiac Village
        };

        public static LevelSpawnData GetLevelSpawnData(int levelId)
        {
            return Configurations[levelId];
        }

        public static LevelSpawnData GetCurrentLevelSpawnData()
        {
            return GetLevelSpawnData(LaunchParameters.Instance.LevelId!.Value);
        }
    }
}