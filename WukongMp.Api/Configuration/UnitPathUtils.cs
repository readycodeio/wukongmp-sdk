using System;
using System.Collections.Generic;
using System.Linq;

namespace WukongMp.Api.Configuration;

public static class UnitPathUtils
{
    private static readonly Dictionary<TamerKind, string> CharacterPathNames = new()
    {
        // Monkey
        { TamerConstants.Monkey, "/Game/00Main/Design/Units/Player/TAMER_monkeysummon_fs.TAMER_monkeysummon_fs_C" },

        // Regular enemies
        { TamerConstants.AxeStalwart, "/Game/00Main/Design/Units/HYS/TAMER_hys_niu_02.TAMER_hys_niu_02_C" },
        { TamerConstants.Bandit, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_tufei_01.TAMER_gycy_tufei_01_C" },
        { TamerConstants.BladeMonk, "/Game/00Main/Design/Units/LYS/TAMER_LYS_JieDaoSeng.TAMER_LYS_JieDaoSeng_C" },
        { TamerConstants.BullSergeant, "/Game/00Main/Design/Units/HYS/TAMER_hys_techushi_01.TAMER_hys_techushi_01_C" },
        { TamerConstants.BullSoldier, "/Game/00Main/Design/Units/HYS/TAMER_hys_techushi_02.TAMER_hys_techushi_02_C" },
        { TamerConstants.BullStalwart, "/Game/00Main/Design/Units/HYS/TAMER_hys_techushi_03.TAMER_hys_techushi_03_C" },
        { TamerConstants.CrowDiviner, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_yaxiangke.TAMER_gycy_yaxiangke_C" },
        { TamerConstants.EagleSoldier, "/Game/00Main/Design/Units/MGD/Tamer_mgd_tianbing_02.TAMER_mgd_tianbing_02_C" },
        { TamerConstants.EarthRakshasa, "/Game/00Main/Design/Units/HYS/TAMER_hys_huijingrenou_03.TAMER_hys_huijingrenou_03_C" },
        { TamerConstants.RatCaptain, "/Game/00Main/Design/Units/HFM/TAMER_hfm_shuangtoushu_01a.TAMER_hfm_shuangtoushu_01a_C" },
        { TamerConstants.RatSoldier, "/Game/00Main/Design/Units/HFM/TAMER_hfm_shu_05a.TAMER_hfm_shu_05a_C" },
        { TamerConstants.SnakePatroller, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_she_03.TAMER_gycy_she_03_C" },
        { TamerConstants.TurtleTreasure, "/Game/00Main/Design/Units/HYS/TAMER_hys_niaozui.TAMER_hys_niaozui_C" },
        { TamerConstants.WolfArcher, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_08_NotMove.TAMER_gycy_lang_08_NotMove_C" },
        { TamerConstants.WolfArcherMove, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_08.TAMER_gycy_lang_08_C" },
        { TamerConstants.WolfAssassin, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_02.TAMER_gycy_lang_02_C" },
        { TamerConstants.WolfGuardian, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_huangpaolang.TAMER_gycy_huangpaolang_C" },
        { TamerConstants.WolfScout, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_03.TAMER_gycy_lang_03_C" },
        { TamerConstants.WolfSentinel, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_01.TAMER_gycy_lang_01_C" },
        { TamerConstants.WolfSoldier, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_07a.TAMER_gycy_lang_07a_C" },
        { TamerConstants.WolfStalwart, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_06.TAMER_gycy_lang_06_C" },
        { TamerConstants.WolfSwornsword, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_05.TAMER_gycy_lang_05_C" },
        { TamerConstants.YakshaArcher, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_guishiwei_01a.TAMER_gycy_guishiwei_01a_C" },
        { TamerConstants.YakshaPatroller, "/Game/00Main/Design/Units/HFM/TAMER_hfm_xunshangui_01a.TAMER_hfm_xunshangui_01a_C" },
        // { CharacterKind.BlazeBone, "/Game/00Main/Design/Units/HFM/TAMER_hfm_guyao_01_realstand.TAMER_hfm_guyao_01_realstand_C" }, // enemy performs no actions
        // { CharacterKind.JackalSoldier, "/Game/00Main/Design/Units/MGD/TAMER_mgd_tianbing_03.TAMER_mgd_tianbing_03_C" }, projectiles not synchronized

        // Enemies to test
        // { CharacterKind.Lang03Huoba, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_03_huoba.TAMER_gycy_lang_03_huoba_C" }, // torch
        // { CharacterKind.Lang03Summon, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_03_summon.TAMER_gycy_lang_03_summon_C" },
        // { CharacterKind.Lang04, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lang_04.TAMER_gycy_lang_04_C" }, // boss
        // { CharacterKind.Lingxuzi01, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_lingxuzi_01.TAMER_gycy_lingxuzi_01_C" },
        // { CharacterKind.Seng01, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_seng_01.TAMER_gycy_seng_01_C" },
        // { CharacterKind.Seng02, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_seng_02.TAMER_gycy_seng_02_C" },
        // { CharacterKind.Seng03, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_seng_03.TAMER_gycy_seng_03_C" },
        // { CharacterKind.Seng04, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_seng_04.TAMER_gycy_seng_04_C" },
        // { CharacterKind.She01, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_she_01.TAMER_gycy_she_01_C" },
        // { CharacterKind.She02, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_she_02.TAMER_gycy_she_02_C" }

        // Working bosses
        { TamerConstants.Acolyte, "/Game/00Main/Design/Units/HFM/TAMER_hfm_shawuliang_01a.TAMER_hfm_shawuliang_01a_C" },
        { TamerConstants.ApramanaBat, "/Game/00Main/Design/Units/LYS/TAMER_lys_mo3.TAMER_lys_mo3_C" },
        { TamerConstants.BlackBear, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_xiong_02.TAMER_gycy_xiong_02_C" },
        { TamerConstants.BlackLoong, "/Game/00Main/Design/Units/LYS/TAMER_lys_chuilong_01a.TAMER_lys_chuilong_01a_C" },
        { TamerConstants.BlackWind, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_hfdw.TAMER_gycy_hfdw_C" },
        { TamerConstants.CyanLoong, "/Game/00Main/Design/Units/LYS/TAMER_lys_wudulong_03a.TAMER_lys_wudulong_03a_C" },
        { TamerConstants.Dear, "/Game/00Main/Design/Units/Online/SZLC/TAMER_szlc_yingzuilu_01.TAMER_szlc_yingzuilu_01_C" },
        { TamerConstants.EarthWolf, "/Game/00Main/Design/Units/HFM/TAMER_HFM_Suoyang_01a.TAMER_HFM_Suoyang_01a_C" },
        { TamerConstants.Erlang, "/Game/00Main/Design/Units/MGD/TAMER_mgd_yangjian_01.TAMER_mgd_yangjian_01_C" },
        { TamerConstants.ErlangShen, "/Game/00Main/Design/Units/MGD/TAMER_mgd_erlangshen_01.TAMER_mgd_erlangshen_01_C" },
        { TamerConstants.FatherOfStones, "/Game/00Main/Design/Units/HYS/TAMER_hys_hms.TAMER_hys_hms_C" },
        { TamerConstants.GoldRhino, "/Game/00Main/Design/Units/Online/SZLC/TAMER_szlc_xiniu_01.TAMER_szlc_xiniu_01_C" },
        { TamerConstants.GoreEye, "/Game/00Main/Design/Units/HFM/TAMER_hfm_hou_01a.TAMER_hfm_hou_01a_C" },
        { TamerConstants.KangLoong, "/Game/00Main/Design/Units/LYS/TAMER_lys_kjldragon.TAMER_lys_kjldragon_C" },
        { TamerConstants.KangStar, "/Game/00Main/Design/Units/LYS/TAMER_lys_kjlwoman.TAMER_lys_kjlwoman_C" },
        { TamerConstants.MadTiger, "/Game/00Main/Design/Units/HFM/TAMER_hfm_bashanhu_01.TAMER_hfm_bashanhu_01_C" },
        { TamerConstants.Mantis, "/Game/00Main/Design/Units/Online/SZLC/TAMER_szlc_tanglang01.TAMER_szlc_tanglang01_C" },
        { TamerConstants.NonPure, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_seng_04.TAMER_gycy_seng_04_C" },
        { TamerConstants.NonVoid, "/Game/00Main/Design/Units/LYS/TAMER_LYS_LaoSeng_01.TAMER_LYS_LaoSeng_01_C" }, // Projectiles not synced
        { TamerConstants.PoisonChief, "/Game/00Main/Design/Units/Online/SL/TAMER_sl_shitongling.TAMER_sl_shitongling_C" },
        { TamerConstants.RedBoy, "/Game/00Main/Design/Units/HYS/TAMER_hys_honghaier_01a.TAMER_hys_honghaier_01a_C" },
        { TamerConstants.RedLoong, "/Game/00Main/Design/Units/LYS/TAMER_lys_wudulong_02a.TAMER_lys_wudulong_02a_C" },
        { TamerConstants.StoneMonkey, "/Game/00Main/Design/Units/MGD/TAMER_mgd_yuan.TAMER_mgd_yuan_C" },
        { TamerConstants.TigerVanguard, "/Game/00Main/Design/Units/HFM/TAMER_hfm_hu_01.TAMER_hfm_hu_01_C" },
        { TamerConstants.WhitecladNoble, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_baiyi_03.TAMER_gycy_baiyi_03_C" },
        { TamerConstants.YellowSquire, "/Game/00Main/Design/Units/HFM/TAMER_hfm_huangpaozhu.TAMER_hfm_huangpaozhu_C" },
        { TamerConstants.YellowWind, "/Game/00Main/Design/Units/HFM/TAMER_hfm_hfds_01a.TAMER_hfm_hfds_01a_C" },
        { TamerConstants.YinTiger, "/Game/00Main/Design/Units/HFM/TAMER_hfm_hu_wind_01.TAMER_hfm_hu_wind_01_C" }, // Cutscene not synchronized

        { TamerConstants.DaSheng, "/Game/00Main/Design/Units/MGD/TAMER_mgd_jsds.TAMER_mgd_jsds_C" },
        { TamerConstants.DaSheng2, "/Game/00Main/Design/Units/MGD/TAMER_mgd_jsds_p2.TAMER_mgd_jsds_p2_C" },

        // Not syncing bosses
        // { CharacterKind.JiaoLoong, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_baiyi_04.TAMER_gycy_baiyi_04_C" },  // do not show - guid mismatch
        // { CharacterKind.Martialist, "/Game/00Main/Design/Units/HFM/TAMER_HFM_HuanWuZhe_01a.TAMER_HFM_HuanWuZhe_01a_C" }, // do not show - guid mismatch
        // { CharacterKind.MacaqueChief, "/Game/00Main/Design/Units/LYS/TAMER_lys_xuehou.TAMER_lys_xuehou_C" }, // summons are not synchronized
        // { CharacterKind.LotusVision, "/Game/00Main/Design/Units/LYS/TAMER_lys_mo4.TAMER_lys_mo4_C" }, // do not show - guid mismatch
        // { CharacterKind.BawLangLang, "/Game/00Main/Design/Units/HYS/TAMER_hys_wa_01.TAMER_hys_wa_01_C" }, // do not show - guid mismatch
        // { CharacterKind.Spider, "/Game/00Main/Design/Units/Online/SL/TAMER_szlc_zizhuer_01.TAMER_szlc_zizhuer_01_C" }, // debug content
        // { CharacterKind.Spider2, "/Game/00Main/Design/Units/Online/SL/TAMER_szlc_baiyanmojun_01.TAMER_szlc_baiyanmojun_01_C" }, // debug content
        // { CharacterKind.BossB, "/Game/00Main/Design/Units/HYS/TAMER_hys_honghaier_01a.TAMER_hys_honghaier_01a_C" },
        // { CharacterKind.BossC, "/Game/00Main/Design/Units/GYCY/TAMER_gycy_yanjianxi_01a.TAMER_gycy_yanjianxi_01a_C" }
    };

    public static string GetUnitPathName(TamerKind tamerKind)
    {
        if (tamerKind == default)
            throw new ArgumentException($"Invalid tamer kind: {tamerKind}");
        // NOTE: should always work since we don't ever create incorrect TamerKinds
        return CharacterPathNames[tamerKind];
    }

    public static bool IsValidUnitName(TamerKind tamerKind)
        => CharacterPathNames.ContainsKey(tamerKind);
    
    public static IEnumerable<TamerKind> GetAllValidUnits()
        => CharacterPathNames.Keys;

    public static IEnumerable<string> GetAllValidUnitNames()
        => CharacterPathNames.Keys.Select(x => x.Name!);
}