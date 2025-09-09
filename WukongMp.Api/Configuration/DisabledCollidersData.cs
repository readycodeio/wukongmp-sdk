using System.Collections.Generic;

namespace WukongMp.Api.Configuration
{
    internal static class DisabledCollidersData
    {
        private static readonly List<string> Guids =
        [
            "1716325759611-719bd9374ae623ee09ed3983024db13e-BP_DynamicObstcle_C_1",
            "1686949186349-6b25c5f04b1967cd5863e485d95e3cd9-BP_DynamicObstcle_C_3",
        ];

        public static bool IsDisabled(string guid)
        {
            return Guids.Contains(guid);
        }
    }
}