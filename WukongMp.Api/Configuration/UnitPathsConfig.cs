using System.Collections.Generic;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Configuration
{
    internal static class UnitPathsConfig
    {
        private static readonly Dictionary<string, string> Configurations = new()
        {
            // Monkey
            { CharacterKind.Monkey, "/Game/00Main/Design/Units/Player/TAMER_monkeysummon_fs.TAMER_monkeysummon_fs_C" },

            // Regular enemies
            { CharacterKind.AxeStalwart, "/Game/00Main/Design/Units/HYS/TAMER_hys_niu_02.TAMER_hys_niu_02_C" },
            { CharacterKind.Bandit, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_tufei_01.TAMER_gycy_tufei_01_C" },
            { CharacterKind.BladeMonk, "/Game/00Main/Design/Units/LYS/TAMER_LYS_JieDaoSeng.TAMER_LYS_JieDaoSeng_C" },
            { CharacterKind.BullSergeant, "/Game/00Main/Design/Units/HYS/TAMER_hys_techushi_01.TAMER_hys_techushi_01_C" },
            { CharacterKind.BullSoldier, "/Game/00Main/Design/Units/HYS/TAMER_hys_techushi_02.TAMER_hys_techushi_02_C" },
            { CharacterKind.BullStalwart, "/Game/00Main/Design/Units/HYS/TAMER_hys_techushi_03.TAMER_hys_techushi_03_C" },
            { CharacterKind.CrowDiviner, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_yaxiangke.TAMER_gycy_yaxiangke_C" },
            { CharacterKind.EagleSoldier, "/Game/00Main/Design/Units/MGD/Tamer_mgd_tianbing_02.TAMER_mgd_tianbing_02_C" },
            { CharacterKind.EarthRakshasa, "/Game/00Main/Design/Units/HYS/TAMER_hys_huijingrenou_03.TAMER_hys_huijingrenou_03_C" },
            { CharacterKind.RatCaptain, "/Game/00Main/Design/Units/HFM/TAMER_hfm_shuangtoushu_01a.TAMER_hfm_shuangtoushu_01a_C" },
            { CharacterKind.RatSoldier, "/Game/00Main/Design/Units/HFM/TAMER_hfm_shu_05a.TAMER_hfm_shu_05a_C" },
            { CharacterKind.SnakePatroller, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_she_03.TAMER_gycy_she_03_C" },
            { CharacterKind.TurtleTreasure, "/Game/00Main/Design/Units/HYS/TAMER_hys_niaozui.TAMER_hys_niaozui_C" },
            { CharacterKind.WolfArcher, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_08_NotMove.TAMER_gycy_lang_08_NotMove_C" },
            { CharacterKind.WolfArcherMove, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_08.TAMER_gycy_lang_08_C" },
            { CharacterKind.WolfAssassin, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_02.TAMER_gycy_lang_02_C" },
            { CharacterKind.WolfGuardian, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_huangpaolang.TAMER_gycy_huangpaolang_C" },
            { CharacterKind.WolfScout, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_03.TAMER_gycy_lang_03_C" },
            { CharacterKind.WolfSentinel, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_01.TAMER_gycy_lang_01_C" },
            { CharacterKind.WolfSoldier, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_07a.TAMER_gycy_lang_07a_C" },
            { CharacterKind.WolfStalwart, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_06.TAMER_gycy_lang_06_C" },
            { CharacterKind.WolfSwornsword, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_05.TAMER_gycy_lang_05_C" },
            { CharacterKind.YakshaArcher, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_guishiwei_01a.TAMER_gycy_guishiwei_01a_C" },
            { CharacterKind.YakshaPatroller, "/Game/00Main/Design/Units/HFM/TAMER_hfm_xunshangui_01a.TAMER_hfm_xunshangui_01a_C" },
            // { CharacterKind.BlazeBone, "/Game/00Main/Design/Units/HFM/TAMER_hfm_guyao_01_realstand.TAMER_hfm_guyao_01_realstand_C" }, // enemy performs no actions
            // { CharacterKind.JackalSoldier, "/Game/00Main/Design/Units/MGD/TAMER_mgd_tianbing_03.TAMER_mgd_tianbing_03_C" }, projectiles not synchronized

            // Enemies to test
            //{ CharacterKind.Lang03Huoba, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_03_huoba.TAMER_gycy_lang_03_huoba_C" }, // torch
            //{ CharacterKind.Lang03Summon, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_03_summon.TAMER_gycy_lang_03_summon_C" },
            //{ CharacterKind.Lang04, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_04.TAMER_gycy_lang_04_C" }, // boss
            //{ CharacterKind.Lingxuzi01, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lingxuzi_01.TAMER_gycy_lingxuzi_01_C" },
            //{ CharacterKind.Seng01, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_seng_01.TAMER_gycy_seng_01_C" },
            //{ CharacterKind.Seng02, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_seng_02.TAMER_gycy_seng_02_C" },
            //{ CharacterKind.Seng03, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_seng_03.TAMER_gycy_seng_03_C" },
            //{ CharacterKind.Seng04, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_seng_04.TAMER_gycy_seng_04_C" },
            //{ CharacterKind.She01, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_she_01.TAMER_gycy_she_01_C" },
            //{ CharacterKind.She02, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_she_02.TAMER_gycy_she_02_C" }

            // Working bosses
            { CharacterKind.Acolyte, "/Game/00Main/Design/Units/HFM/TAMER_hfm_shawuliang_01a.TAMER_hfm_shawuliang_01a_C" },
            { CharacterKind.ApramanaBat, "/Game/00Main/Design/Units/LYS/TAMER_lys_mo3.TAMER_lys_mo3_C" },
            { CharacterKind.BlackBear, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_xiong_02.TAMER_gycy_xiong_02_C" },
            { CharacterKind.BlackLoong, "/Game/00Main/Design/Units/LYS/TAMER_lys_chuilong_01a.TAMER_lys_chuilong_01a_C" },
            { CharacterKind.BlackWind, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_hfdw.TAMER_gycy_hfdw_C" },
            { CharacterKind.CyanLoong, "/Game/00Main/Design/Units/LYS/TAMER_lys_wudulong_03a.TAMER_lys_wudulong_03a_C" },
            { CharacterKind.Dear, "/Game/00Main/Design/Units/Online/SZLC/TAMER_szlc_yingzuilu_01.TAMER_szlc_yingzuilu_01_C" },
            { CharacterKind.EarthWolf, "/Game/00Main/Design/Units/HFM/TAMER_HFM_Suoyang_01a.TAMER_HFM_Suoyang_01a_C" },
            { CharacterKind.Erlang, "/Game/00Main/Design/Units/MGD/TAMER_mgd_yangjian_01.TAMER_mgd_yangjian_01_C" },
            { CharacterKind.ErlangShen, "/Game/00Main/Design/Units/MGD/TAMER_mgd_erlangshen_01.TAMER_mgd_erlangshen_01_C" },
            { CharacterKind.FatherOfStones, "/Game/00Main/Design/Units/HYS/TAMER_hys_hms.TAMER_hys_hms_C" },
            { CharacterKind.GoldRhino, "/Game/00Main/Design/Units/Online/SZLC/TAMER_szlc_xiniu_01.TAMER_szlc_xiniu_01_C" },
            { CharacterKind.GoreEye, "/Game/00Main/Design/Units/HFM/TAMER_hfm_hou_01a.TAMER_hfm_hou_01a_C" },
            { CharacterKind.KangLoong, "/Game/00Main/Design/Units/LYS/TAMER_lys_kjldragon.TAMER_lys_kjldragon_C" },
            { CharacterKind.KangStar, "/Game/00Main/Design/Units/LYS/TAMER_lys_kjlwoman.TAMER_lys_kjlwoman_C" },
            { CharacterKind.MadTiger, "/Game/00Main/Design/Units/HFM/TAMER_hfm_bashanhu_01.TAMER_hfm_bashanhu_01_C" },
            { CharacterKind.Mantis, "/Game/00Main/Design/Units/Online/SZLC/TAMER_szlc_tanglang01.TAMER_szlc_tanglang01_C" },
            { CharacterKind.NonPure, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_seng_04.TAMER_gycy_seng_04_C" },
            { CharacterKind.NonVoid, "/Game/00Main/Design/Units/LYS/TAMER_LYS_LaoSeng_01.TAMER_LYS_LaoSeng_01_C" }, // Projectiles not synced
            { CharacterKind.PoisonChief, "/Game/00Main/Design/Units/Online/SL/TAMER_sl_shitongling.TAMER_sl_shitongling_C" },
            { CharacterKind.RedBoy, "/Game/00Main/Design/Units/HYS/TAMER_hys_honghaier_01a.TAMER_hys_honghaier_01a_C" },
            { CharacterKind.RedLoong, "/Game/00Main/Design/Units/LYS/TAMER_lys_wudulong_02a.TAMER_lys_wudulong_02a_C" },
            { CharacterKind.StoneMonkey, "/Game/00Main/Design/Units/MGD/TAMER_mgd_yuan.TAMER_mgd_yuan_C" },
            { CharacterKind.TigerVanguard, "/Game/00Main/Design/Units/HFM/TAMER_hfm_hu_01.TAMER_hfm_hu_01_C" },
            { CharacterKind.WhitecladNoble, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_baiyi_03.TAMER_gycy_baiyi_03_C" },
            { CharacterKind.YellowLoong, "/Game/00Main/Design/Units/LYS/TAMER_lys_dage.TAMER_lys_dage_C" },
            { CharacterKind.YellowSquire, "/Game/00Main/Design/Units/HFM/TAMER_hfm_huangpaozhu.TAMER_hfm_huangpaozhu_C" },
            { CharacterKind.YellowWind, "/Game/00Main/Design/Units/HFM/TAMER_hfm_hfds_01a.TAMER_hfm_hfds_01a_C" },
            { CharacterKind.YinTiger, "/Game/00Main/Design/Units/HFM/TAMER_hfm_hu_wind_01.TAMER_hfm_hu_wind_01_C" }, // Cutscene not synchronized

            { CharacterKind.DaSheng, "/Game/00Main/Design/Units/MGD/TAMER_mgd_jsds.TAMER_mgd_jsds_C" },
            { CharacterKind.DaSheng2, "/Game/00Main/Design/Units/MGD/TAMER_mgd_jsds_p2.TAMER_mgd_jsds_p2_C" },

            // Not syncing bosses
            //{ CharacterKind.JiaoLoong, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_baiyi_04.TAMER_gycy_baiyi_04_C" },  // do not show - guid mismatch
            //{ CharacterKind.Martialist, "/Game/00Main/Design/Units/HFM/TAMER_HFM_HuanWuZhe_01a.TAMER_HFM_HuanWuZhe_01a_C" }, // do not show - guid mismatch
            //{ CharacterKind.MacaqueChief, "/Game/00Main/Design/Units/LYS/TAMER_lys_xuehou.TAMER_lys_xuehou_C" }, // summons are not synchronized
            //{ CharacterKind.LotusVision, "/Game/00Main/Design/Units/LYS/TAMER_lys_mo4.TAMER_lys_mo4_C" }, // do not show - guid mismatch
            //{ CharacterKind.BawLangLang, "/Game/00Main/Design/Units/HYS/TAMER_hys_wa_01.TAMER_hys_wa_01_C" }, // do not show - guid mismatch
            //{ CharacterKind.Spider, "/Game/00Main/Design/Units/Online/SL/TAMER_szlc_zizhuer_01.TAMER_szlc_zizhuer_01_C" }, // debug content
            //{ CharacterKind.Spider2, "/Game/00Main/Design/Units/Online/SL/TAMER_szlc_baiyanmojun_01.TAMER_szlc_baiyanmojun_01_C" }, // debug content
            //{ CharacterKind.BossB, "/Game/00Main/Design/Units/HYS/TAMER_hys_honghaier_01a.TAMER_hys_honghaier_01a_C" },
            //{ CharacterKind.BossC, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_yanjianxi_01a.TAMER_gycy_yanjianxi_01a_C" }
        };

        public static string GetUnitPath(string unitName)
        {
            return Configurations[TamerUtils.UnifyUnitName(unitName)];
        }

        public static bool IsValidMonsterName(string enemyName)
        {
            return Configurations.ContainsKey(TamerUtils.UnifyUnitName(enemyName));
        }
    }
}