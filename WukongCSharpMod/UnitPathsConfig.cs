using System;
using System.Collections.Generic;
using System.Linq;

namespace WukongCSharpMod
{
    internal static class UnitPathsConfig
    {
        private static readonly Dictionary<string, string> Configurations = new Dictionary<string, string>
        {
            { "wolf_sentinel", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_01.TAMER_gycy_lang_01_C" },
            { "wolf_soldier", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_07a.TAMER_gycy_lang_07a_C" },
            { "wolf_archer", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_08_NotMove.TAMER_gycy_lang_08_NotMove_C" },
            // Enemies to test
            { "wolf_archer_move", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_08.TAMER_gycy_lang_08_C" },
            { "wolf_assassin", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_02.TAMER_gycy_lang_02_C" },
            { "wolf_scout", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_03.TAMER_gycy_lang_03_C" },
            { "wolf_swornsword", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_05.TAMER_gycy_lang_05_C" },
            { "wolf_stalwart", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_06.TAMER_gycy_lang_06_C" },
            { "wolf_guardian", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_huangpaolang.TAMER_gycy_huangpaolang_C" },
            { "yaksha_archer", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_guishiwei_01a.TAMER_gycy_guishiwei_01a_C" },
            { "yaksha_patroller", "/Game/00Main/Design/Units/HFM/TAMER_hfm_xunshangui_01a.TAMER_hfm_xunshangui_01a_C" },
            { "snake_partoller", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_she_03.TAMER_gycy_she_03_C" },
            { "bandit", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_tufei_01.TAMER_gycy_tufei_01_C" },
            { "crow_diviner", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_yaxiangke.TAMER_gycy_yaxiangke_C" },
            { "blaze_bone", "/Game/00Main/Design/Units/HFM/TAMER_hfm_guyao_01_realstand.TAMER_hfm_guyao_01_realstand_C" },
            { "rat_captain", "/Game/00Main/Design/Units/HFM/TAMER_hfm_shuangtoushu_01a.TAMER_hfm_shuangtoushu_01a_C" },
            { "rat_soldier", "/Game/00Main/Design/Units/HFM/TAMER_hfm_shu_05a.TAMER_hfm_shu_05a_C" },
            { "earth_rakshasa", "/Game/00Main/Design/Units/HYS/TAMER_hys_huijingrenou_03.TAMER_hys_huijingrenou_03_C" },
            { "turtle_treasure", "/Game/00Main/Design/Units/HYS/TAMER_hys_niaozui.TAMER_hys_niaozui_C" },
            { "bull_sergeant", "/Game/00Main/Design/Units/HYS/TAMER_hys_techushi_01.TAMER_hys_techushi_01_C" },
            { "bull_soldier", "/Game/00Main/Design/Units/HYS/TAMER_hys_techushi_02.TAMER_hys_techushi_02_C" },
            { "bull_stalwart", "/Game/00Main/Design/Units/HYS/TAMER_hys_techushi_03.TAMER_hys_techushi_03_C" },
            { "blade_monk", "/Game/00Main/Design/Units/LYS/TAMER_LYS_JieDaoSeng.TAMER_LYS_JieDaoSeng_C" },
            { "eagle_soldier", "/Game/00Main/Design/Units/MGD/Tamer_mgd_tianbing_02.TAMER_mgd_tianbing_02_C" },
            { "jackal_soldier", "/Game/00Main/Design/Units/MGD/TAMER_mgd_tianbing_03.TAMER_mgd_tianbing_03_C" },

            //{ "lang_03_huoba", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_03_huoba.TAMER_gycy_lang_03_huoba_C" }, // torch
            //{ "lang_03_summon", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_03_summon.TAMER_gycy_lang_03_summon_C" },
            //{ "lang_04", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_04.TAMER_gycy_lang_04_C" }, // boss
            //{ "lingxuzi_01", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lingxuzi_01.TAMER_gycy_lingxuzi_01_C" },
            //{ "seng_01", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_seng_01.TAMER_gycy_seng_01_C" },
            //{ "seng_02", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_seng_02.TAMER_gycy_seng_02_C" },
            //{ "seng_03", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_seng_03.TAMER_gycy_seng_03_C" },
            //{ "seng_04", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_seng_04.TAMER_gycy_seng_04_C" },
            //{ "she_01", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_she_01.TAMER_gycy_she_01_C" },
            //{ "she_02", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_she_02.TAMER_gycy_she_02_C" }
            //working bosses
            { "whiteclad_noble", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_baiyi_03.TAMER_gycy_baiyi_03_C" },
            { "black_wind", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_hfdw.TAMER_gycy_hfdw_C" },
            { "mantis", "/Game/00Main/Design/Units/Online/SZLC/TAMER_szlc_tanglang01.TAMER_szlc_tanglang01_C" },
            //bosses to test
            { "non_pure", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_seng_04.TAMER_gycy_seng_04_C" },
            { "black_bear", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_xiong_02.TAMER_gycy_xiong_02_C" },
            { "yellow_wind", "/Game/00Main/Design/Units/HFM/TAMER_hfm_hfds_01a.TAMER_hfm_hfds_01a_C" },
            { "gore_eye", "/Game/00Main/Design/Units/HFM/TAMER_hfm_hou_01a.TAMER_hfm_hou_01a_C" },
            { "yellow_squire", "/Game/00Main/Design/Units/HFM/TAMER_hfm_huangpaozhu.TAMER_hfm_huangpaozhu_C" },
            { "tiger_vanguard", "/Game/00Main/Design/Units/HFM/TAMER_hfm_hu_01.TAMER_hfm_hu_01_C" },
            { "yin_tiger", "/Game/00Main/Design/Units/HFM/TAMER_hfm_hu_wind_01.TAMER_hfm_hu_wind_01_C" },
            { "acolyte", "/Game/00Main/Design/Units/HFM/TAMER_hfm_shawuliang_01a.TAMER_hfm_shawuliang_01a_C" },
            { "red_boy", "/Game/00Main/Design/Units/HYS/TAMER_hys_honghaier_01a.TAMER_hys_honghaier_01a_C" },
            { "father_of_stones", "/Game/00Main/Design/Units/HYS/TAMER_hys_hms.TAMER_hys_hms_C" },
            { "axe_stalwart", "/Game/00Main/Design/Units/HYS/TAMER_hys_niu_02.TAMER_hys_niu_02_C" },
            { "baw_lang_lang", "/Game/00Main/Design/Units/HYS/TAMER_hys_wa_01.TAMER_hys_wa_01_C" },
            { "black_loong", "/Game/00Main/Design/Units/LYS/TAMER_lys_chuilong_01a.TAMER_lys_chuilong_01a_C" },
            { "yellow_loong", "/Game/00Main/Design/Units/LYS/TAMER_lys_dage.TAMER_lys_dage_C" },
            { "kang_loong", "/Game/00Main/Design/Units/LYS/TAMER_lys_kjldragon.TAMER_lys_kjldragon_C" },
            { "kang_star", "/Game/00Main/Design/Units/LYS/TAMER_lys_kjlwoman.TAMER_lys_kjlwoman_C" },
            { "non_void", "/Game/00Main/Design/Units/LYS/TAMER_LYS_LaoSeng_01.TAMER_LYS_LaoSeng_01_C" },
            { "apramana_bat", "/Game/00Main/Design/Units/LYS/TAMER_lys_mo3.TAMER_lys_mo3_C" },
            { "lotus_vision", "/Game/00Main/Design/Units/LYS/TAMER_lys_mo4.TAMER_lys_mo4_C" },
            { "red_loong", "/Game/00Main/Design/Units/LYS/TAMER_lys_wudulong_02a.TAMER_lys_wudulong_02a_C" },
            { "cyan_loong", "/Game/00Main/Design/Units/LYS/TAMER_lys_wudulong_03a.TAMER_lys_wudulong_03a_C" },
            { "macaque_chief", "/Game/00Main/Design/Units/LYS/TAMER_lys_xuehou.TAMER_lys_xuehou_C" },
            { "erlang_shen", "/Game/00Main/Design/Units/MGD/TAMER_mgd_erlangshen_01.TAMER_mgd_erlangshen_01_C" },
            { "erlang", "/Game/00Main/Design/Units/MGD/TAMER_mgd_yangjian_01.TAMER_mgd_yangjian_01_C" },
            { "poison_chief", "/Game/00Main/Design/Units/Online/SL/TAMER_sl_shitongling.TAMER_sl_shitongling_C" },
            { "gold_rhino", "/Game/00Main/Design/Units/Online/SZLC/TAMER_szlc_xiniu_01.TAMER_szlc_xiniu_01_C" },
            { "dear", "/Game/00Main/Design/Units/Online/SZLC/TAMER_szlc_yingzuilu_01.TAMER_szlc_yingzuilu_01_C" },
            //not syncing bosses
            { "earth_wolf", "/Game/00Main/Design/Units/HFM/TAMER_HFM_Suoyang_01a.TAMER_HFM_Suoyang_01a_C" }, // not synchronized
            { "spider", "/Game/00Main/Design/Units/Online/SL/TAMER_szlc_zizhuer_01.TAMER_szlc_zizhuer_01_C" }, // debug content
            { "spider2", "/Game/00Main/Design/Units/Online/SL/TAMER_szlc_baiyanmojun_01.TAMER_szlc_baiyanmojun_01_C" }, // debug content
            { "mad_tiger", "/Game/00Main/Design/Units/HFM/TAMER_hfm_bashanhu_01.TAMER_hfm_bashanhu_01_C" },
            { "stone_moneky", "/Game/00Main/Design/Units/MGD/TAMER_mgd_yuan.TAMER_mgd_yuan_C" }, // desynchronize whole game 
            { "martialist", "/Game/00Main/Design/Units/HFM/TAMER_HFM_HuanWuZhe_01a.TAMER_HFM_HuanWuZhe_01a_C" }, // not syncing completetly
            { "jiao_loong", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_baiyi_04.TAMER_gycy_baiyi_04_C" }, // falls under ground
            { "boss_b", "/Game/00Main/Design/Units/HYS/TAMER_hys_honghaier_01a.TAMER_hys_honghaier_01a_C" },
            { "boss_c", "/Game/00Main/Design/Units/GYCY/TAMER_gycy_yanjianxi_01a.TAMER_gycy_yanjianxi_01a_C" }
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