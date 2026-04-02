using System;
using System.Collections.Generic;
using WukongMp.Api.WukongUtils;

namespace WukongMp.Api.Configuration;

public static class TamerConstants
{
    private static TamerKind CreateTamerKind(string name, bool disabled = false)
    {
        // NOTE: For disabled tamers, we return an invalid tamer constant
        if (disabled)
            return new TamerKind();
        
        var kind = new TamerKind(TamerUtils.UnifyUnitName(name));
        _validTamerKinds.Add(kind);
        return kind;
    }

    private static readonly HashSet<TamerKind> _validTamerKinds = new();
    
    public static readonly TamerKind Monkey = CreateTamerKind("monkey");
    
    // regular enemies
    public static readonly TamerKind AxeStalwart = CreateTamerKind("axe_stalwart");
    public static readonly TamerKind Bandit = CreateTamerKind("bandit");
    public static readonly TamerKind BladeMonk = CreateTamerKind("blade_monk");
    public static readonly TamerKind BullSergeant = CreateTamerKind("bull_sergeant");
    public static readonly TamerKind BullSoldier = CreateTamerKind("bull_soldier");
    public static readonly TamerKind BullStalwart = CreateTamerKind("bull_stalwart");
    public static readonly TamerKind CrowDiviner = CreateTamerKind("crow_diviner");
    public static readonly TamerKind EagleSoldier = CreateTamerKind("eagle_soldier");
    public static readonly TamerKind EarthRakshasa = CreateTamerKind("earth_rakshasa");
    public static readonly TamerKind RatCaptain = CreateTamerKind("rat_captain");
    public static readonly TamerKind RatSoldier = CreateTamerKind("rat_soldier");
    public static readonly TamerKind SnakePatroller = CreateTamerKind("snake_patroller");
    public static readonly TamerKind TurtleTreasure = CreateTamerKind("turtle_treasure");
    public static readonly TamerKind WolfArcher = CreateTamerKind("wolf_archer");
    public static readonly TamerKind WolfArcherMove = CreateTamerKind("wolf_archer_move");
    public static readonly TamerKind WolfAssassin = CreateTamerKind("wolf_assassin");
    public static readonly TamerKind WolfGuardian = CreateTamerKind("wolf_guardian");
    public static readonly TamerKind WolfScout = CreateTamerKind("wolf_scout");
    public static readonly TamerKind WolfSentinel = CreateTamerKind("wolf_sentinel");
    public static readonly TamerKind WolfSoldier = CreateTamerKind("wolf_soldier");
    public static readonly TamerKind WolfStalwart = CreateTamerKind("wolf_stalwart");
    public static readonly TamerKind WolfSwornsword = CreateTamerKind("wolf_swornsword");
    public static readonly TamerKind YakshaArcher = CreateTamerKind("yaksha_archer");
    public static readonly TamerKind YakshaPatroller = CreateTamerKind("yaksha_patroller");
    
    // bosses
    public static readonly TamerKind Acolyte = CreateTamerKind("acolyte");
    public static readonly TamerKind ApramanaBat = CreateTamerKind("apramana_bat");
    public static readonly TamerKind BlackBear = CreateTamerKind("black_bear");
    public static readonly TamerKind BlackLoong = CreateTamerKind("black_loong");
    public static readonly TamerKind BlackWind = CreateTamerKind("black_wind");
    public static readonly TamerKind CyanLoong = CreateTamerKind("cyan_loong");
    public static readonly TamerKind Dear = CreateTamerKind("dear");
    public static readonly TamerKind EarthWolf = CreateTamerKind("earth_wolf");
    public static readonly TamerKind Erlang = CreateTamerKind("erlang");
    public static readonly TamerKind ErlangShen = CreateTamerKind("erlang_shen");
    public static readonly TamerKind FatherOfStones = CreateTamerKind("father_of_stones");
    public static readonly TamerKind GoldRhino = CreateTamerKind("gold_rhino");
    public static readonly TamerKind GoreEye = CreateTamerKind("gore_eye");
    public static readonly TamerKind KangLoong = CreateTamerKind("kang_loong");
    public static readonly TamerKind KangStar = CreateTamerKind("kang_star");
    public static readonly TamerKind MadTiger = CreateTamerKind("mad_tiger");
    public static readonly TamerKind Mantis = CreateTamerKind("mantis");
    public static readonly TamerKind NonPure = CreateTamerKind("non_pure");
    public static readonly TamerKind NonVoid = CreateTamerKind("non_void");
    public static readonly TamerKind PoisonChief = CreateTamerKind("poison_chief");
    public static readonly TamerKind RedBoy = CreateTamerKind("red_boy");
    public static readonly TamerKind RedLoong = CreateTamerKind("red_loong");
    public static readonly TamerKind StoneMonkey = CreateTamerKind("stone_monkey");
    public static readonly TamerKind TigerVanguard = CreateTamerKind("tiger_vanguard");
    public static readonly TamerKind WhitecladNoble = CreateTamerKind("whiteclad_noble");
    public static readonly TamerKind YellowLoong = CreateTamerKind("yellow_loong");
    public static readonly TamerKind YellowSquire = CreateTamerKind("yellow_squire");
    public static readonly TamerKind YellowWind = CreateTamerKind("yellow_wind");
    public static readonly TamerKind YinTiger = CreateTamerKind("yin_tiger");
    
    public static readonly TamerKind DaSheng = CreateTamerKind("da_sheng");
    public static readonly TamerKind DaSheng2 = CreateTamerKind("da_sheng_2");

    // not working yet
    public static readonly TamerKind BawLangLang = CreateTamerKind("baw_lang_lang", true);
    public static readonly TamerKind BlazeBone = CreateTamerKind("blaze_bone", true);
    public static readonly TamerKind BossB = CreateTamerKind("boss_b", true);
    public static readonly TamerKind BossC = CreateTamerKind("boss_c", true);
    public static readonly TamerKind JackalSoldier = CreateTamerKind("jackal_soldier", true);
    public static readonly TamerKind JiaoLoong = CreateTamerKind("jiao_loong", true);
    public static readonly TamerKind LotusVision = CreateTamerKind("lotus_vision", true);
    public static readonly TamerKind MacaqueChief = CreateTamerKind("macaque_chief", true);
    public static readonly TamerKind Martialist = CreateTamerKind("martialist", true);
    public static readonly TamerKind Spider = CreateTamerKind("spider", true);
    public static readonly TamerKind Spider2 = CreateTamerKind("spider2", true);
    
    public static bool IsValidTamerName(string tamerName)
    {
        var unifiedName = TamerUtils.UnifyUnitName(tamerName);
        return _validTamerKinds.Contains(new TamerKind(unifiedName));
    }

    public static TamerKind GetTamerKind(string? tamerName)
    {
        if (tamerName == null)
            return new TamerKind();
        
        if (!IsValidTamerName(tamerName))
            throw new ArgumentException($"Invalid tamer name: {tamerName}");
        var unifiedName = TamerUtils.UnifyUnitName(tamerName);
        return new TamerKind(unifiedName);
    }
}
