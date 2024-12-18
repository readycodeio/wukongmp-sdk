using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WukongCSharpMod
{
    internal static class UnitPathsConfig
    {

        private static readonly Dictionary<string, string> Configurations = new Dictionary<string, string>
        {
            { "lang_01", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_01.TAMER_gycy_lang_01_C" },
            { "lang_02", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_02.TAMER_gycy_lang_02_C" },
            { "lang_03", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_03.TAMER_gycy_lang_03_C" },
            { "lang_03_huoba", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_03_huoba.TAMER_gycy_lang_03_huoba_C" },
            { "lang_03_summon", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_03_summon.TAMER_gycy_lang_03_summon_C" },
            { "lang_04", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_04.TAMER_gycy_lang_04_C" },
            { "lang_05", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_05.TAMER_gycy_lang_05_C" },
            { "lang_06", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_06.TAMER_gycy_lang_06_C" },
            { "lang_07a", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_07a.TAMER_gycy_lang_07a_C" },
            { "lang_08", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_08.TAMER_gycy_lang_08_C" },
            { "lang_08_NotMove", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_08_NotMove.TAMER_gycy_lang_08_NotMove_C" },
            { "lingxuzi_01", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lingxuzi_01.TAMER_gycy_lingxuzi_01_C" },
            { "seng_01", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_seng_01.TAMER_gycy_seng_01_C" },
            { "seng_02", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_seng_02.TAMER_gycy_seng_02_C" },
            { "seng_03", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_seng_03.TAMER_gycy_seng_03_C" },
            { "seng_04", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_seng_04.TAMER_gycy_seng_04_C" },
            { "she_01", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_she_01.TAMER_gycy_she_01_C" },
            { "she_02", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_she_02.TAMER_gycy_she_02_C" },
            { "she_02_passive", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_she_02_passive.TAMER_gycy_she_02_passive_C" }
        };

        public static string GetUnitPath(string unitName)
        {
            if (Configurations.TryGetValue(unitName, out string value))
            {
                return value;
            }
            Console.WriteLine($"Unit path for '{unitName}' not found. Spawning {Configurations.First().Key} instead");
            return Configurations.First().Value;
        }
    }
}
