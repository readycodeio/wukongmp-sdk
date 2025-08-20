using System.Collections.Generic;

namespace WukongMp.Api.Configuration
{
    internal static class DisabledCollidersData
    {
        private static readonly List<string> Guids =
        [
            "1716325759611-719bd9374ae623ee09ed3983024db13e-BP_DynamicObstcle_C_1",
        ];

        public static bool IsDisabled(string guid)
        {
            return Guids.Contains(guid);
        }
    }
}