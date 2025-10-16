using System.Collections.Generic;

namespace WukongMp.Api.Configuration
{
    internal static class DisabledCollidersData
    {
        private static readonly List<string> Guids =
        [
            "1716325759611-719bd9374ae623ee09ed3983024db13e-BP_DynamicObstcle_C_1", // act 2 Tiger's Acolyte bridge
            "1686949186349-6b25c5f04b1967cd5863e485d95e3cd9-BP_DynamicObstcle_C_3", // Black Loong arena entrance
            "UGuid.HFM.HuStone.DOin01", // act 2 Tiger Vanguard arena entrance
            "1700513547161-6b25c5f04b1967cd5863e485d95e3cd9-BP_DynamicObstcle_C_1", // act 2 Tiger Vanguard
            "1654114656537-719bd9374ae623ee09ed3983024db13e-BP_DynamicObstcle_C_5", // act 2 Yellow-Robed Squire
            "UGuid.HFS.DBL.DOin" // act 1 Lingxuzi
        ];

        public static bool IsDisabled(string guid)
        {
            return Guids.Contains(guid);
        }
    }
}