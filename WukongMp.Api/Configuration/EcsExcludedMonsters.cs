using System.Collections.Generic;

namespace WukongMp.Api.Configuration
{
    internal static class EcsExcludedMonsters
    {
        public static List<string> MonsterNames =
        [
            // Env
            "szlc_rabbit",
            "SZLC_Bullfrog",
            "SZLC_Mouse",
            // Wukong summons (pluck of many and phantom rush)
            "monkeysummon_pr",
            "monkeysummon_fs"
        ];
    }
}
