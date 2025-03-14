using BtlShare;
using System.Collections.Generic;
using UnrealEngine.Runtime;
using WukongApi.Helpers;

namespace WukongApi
{
    public static class Constants
    {
        public const int ToleratedLatencyMs = 50;
        public const float FloatComparisonTolerance = 0.1f;
        public const string ConnectedPatches = "Connected";
        public const string GlobalPatches = "Global";
        public const float MonsterSpawnDistance = 2000f;
        public const float MonsterSpawnTraceHeight = 10000f;
        public const float MonsterHalfHeight = 200f;
        public const float MonsterSpawnSpread = 100f;
        public const int MonsterSpawnDelayMs = 500;
        public const string AttributePrefix = "attr_";
        public static readonly FVector PvpStartingLocation = new(-11146.926, -3229.771, 6497.035);
        public const float PvpRadius = 4000;
        public const float CameraArmLength = 720;
        public const int CharacterArchiveId = 10;
        public const int NewCharacterArchiveId = 9;
        public const int LevelArchiveId = 0;
        public const int MaxPlayers = 20;
        public static readonly List<int> AvailableTeamIds = new() { -9999, -9998 };
        public const int DrawTeamId = 9999;
        public static readonly List<int> SkillsWhitelist = new() { 10518 };

        public const int CountdownSeconds = 5;
        public const int MatchmakingSeconds = 45;
        public const int RoundSeconds = 30;
        public const int RoundMinutes = 1;

        public const string UiManagerActorPath = "/Game/Mods/CustomLuaMod/BP_UIManager.BP_UIManager_C";
        public const string PlayerMarkerPath = "/Game/Mods/CustomLuaMod/BP_PlayerMarker.BP_PlayerMarker_C";
        public const string ChatWidgetName = "WBP_MultiplayerChat_C";
        public const string TimerWidgetName = "WBP_Timer_C";
        public const string CountdownWidgetName = "WBP_Countdown_C";
        public const string GameMessageWidgetName = "WBP_GameMessage_C";
        public const string InfoMessageWidgetName = "WBP_InfoMessage_C";
        public const string LobbyStatusWidgetName = "WBP_LobbyStatus_C";
        public const string JsonCompactSerializationRegex = @"[A-Za-z0-9\-_=]+\.[A-Za-z0-9\-_=]+\.?[A-Za-z0-9\-_\.+\/=]*";

        public const string RealtimeAppId = "7aa130eb-9912-4845-b2de-8496a6f0fea7";
        public const string ChatAppId = "7fdefcca-ff84-4499-8f27-7d59bbd9c163";

        public static readonly EnumSet<EBGUAttrFloat> SyncedAttributes = new([
            EBGUAttrFloat.HpMax,
            EBGUAttrFloat.MpMax,
            EBGUAttrFloat.B1StunMax,
            EBGUAttrFloat.StaminaDepletedLimit,
            EBGUAttrFloat.StaminaMax,
            EBGUAttrFloat.SkillSuperArmorMax,
            EBGUAttrFloat.TransEnergyMax,
            EBGUAttrFloat.EnergyMinConsume,
            EBGUAttrFloat.EnergyConsumeSpeed,
            EBGUAttrFloat.EnergyIncreaseSpeed,
            EBGUAttrFloat.SpecialEnergyMax,
            EBGUAttrFloat.FabaoEnergyMax,
            EBGUAttrFloat.VigorEnergyMax,
            EBGUAttrFloat.BlockCollapseArmorMax,
            EBGUAttrFloat.FreezeAbnormalAccMax,
            EBGUAttrFloat.BurnAbnormalAccMax,
            EBGUAttrFloat.PoisonAbnormalAccMax,
            EBGUAttrFloat.ThunderAbnormalAccMax,
            EBGUAttrFloat.BlindSlotMax,
            EBGUAttrFloat.BloodBottomNumMax,
            EBGUAttrFloat.PelevelMax,
            EBGUAttrFloat.ShieldMax,
            EBGUAttrFloat.PevalueMax,
            EBGUAttrFloat.YinAbnormalAccMax,
            EBGUAttrFloat.YangAbnormalAccMax,
            EBGUAttrFloat.HpMaxMul,
            EBGUAttrFloat.MpMaxMul,
            EBGUAttrFloat.AtkMul,
            EBGUAttrFloat.DefMul,
            EBGUAttrFloat.B1StunMaxMul,
            EBGUAttrFloat.StaminaDepletedLimitMul,
            EBGUAttrFloat.StaminaMaxMul,
            EBGUAttrFloat.StaminaRecoverMul,
            EBGUAttrFloat.KptturnSpeedMul,
            EBGUAttrFloat.FreezeAbnormalAccMaxMul,
            EBGUAttrFloat.BurnAbnormalAccMaxMul,
            EBGUAttrFloat.PoisonAbnormalAccMaxMul,
            EBGUAttrFloat.ThunderAbnormalAccMaxMul,
            EBGUAttrFloat.YinAbnormalAccMaxMul,
            EBGUAttrFloat.YangAbnormalAccMaxMul,
            EBGUAttrFloat.StaminaCostMultiperMul,
            EBGUAttrFloat.TransEnergyMaxMul,
            EBGUAttrFloat.EnergyMinConsumeMul,
            EBGUAttrFloat.EnergyConsumeSpeedMul,
            EBGUAttrFloat.EnergyIncreaseSpeedMul,
            EBGUAttrFloat.HpMaxBase,
            EBGUAttrFloat.MpMaxBase,
            EBGUAttrFloat.AtkBase,
            EBGUAttrFloat.DefBase,
            EBGUAttrFloat.B1StunMaxBase,
            EBGUAttrFloat.StaminaDepletedLimitBase,
            EBGUAttrFloat.StaminaMaxBase,
            EBGUAttrFloat.StaminaRecoverBase,
            EBGUAttrFloat.SkillSuperArmorMaxBase,
            EBGUAttrFloat.CritRateBase,
            EBGUAttrFloat.CritMultiplierBase,
            EBGUAttrFloat.TenacityBase,
            EBGUAttrFloat.KptturnSpeedBase,
            EBGUAttrFloat.EarPlugBase,
            EBGUAttrFloat.CritRateDefBase,
            EBGUAttrFloat.CritDmgMulDefBase,
            EBGUAttrFloat.DmgAdditionBase,
            EBGUAttrFloat.DmgDefBase,
            EBGUAttrFloat.BlockCollapseArmorMaxBase,
            EBGUAttrFloat.DingshenDefAdditionBase,
            EBGUAttrFloat.FreezeAbnormalAccMaxBase,
            EBGUAttrFloat.BurnAbnormalAccMaxBase,
            EBGUAttrFloat.PoisonAbnormalAccMaxBase,
            EBGUAttrFloat.ThunderAbnormalAccMaxBase,
            EBGUAttrFloat.FreezeAtkBase,
            EBGUAttrFloat.BurnAtkBase,
            EBGUAttrFloat.PoisonAtkBase,
            EBGUAttrFloat.ThunderAtkBase,
            EBGUAttrFloat.FreezeDefBase,
            EBGUAttrFloat.BurnDefBase,
            EBGUAttrFloat.PoisonDefBase,
            EBGUAttrFloat.ThunderDefBase,
            EBGUAttrFloat.BloodBottomNumMaxBase,
            EBGUAttrFloat.PelevelMaxBase,
            EBGUAttrFloat.ShieldMaxBase,
            EBGUAttrFloat.PevalueMaxBase,
            EBGUAttrFloat.YinAbnormalAccMaxBase,
            EBGUAttrFloat.YangAbnormalAccMaxBase,
            EBGUAttrFloat.YinAtkBase,
            EBGUAttrFloat.YangAtkBase,
            EBGUAttrFloat.YinDefBase,
            EBGUAttrFloat.YangDefBase,
            EBGUAttrFloat.StaminaCostMultiperBase,
            EBGUAttrFloat.TransEnergyMaxBase,
            EBGUAttrFloat.EnergyMinConsumeBase,
            EBGUAttrFloat.EnergyConsumeSpeedBase,
            EBGUAttrFloat.EnergyIncreaseSpeedBase,
            // EBGUAttrFloat.Hp,
            // EBGUAttrFloat.Mp,
            EBGUAttrFloat.Atk,
            EBGUAttrFloat.Def,
            EBGUAttrFloat.B1Stun,
            // EBGUAttrFloat.Stamina,
            EBGUAttrFloat.StaminaRecover,
            EBGUAttrFloat.SkillSuperArmor,
            EBGUAttrFloat.CritRate,
            EBGUAttrFloat.CritMultiplier,
            EBGUAttrFloat.Tenacity,
            EBGUAttrFloat.KptturnSpeed,
            EBGUAttrFloat.EarPlug,
            EBGUAttrFloat.CritRateDef,
            EBGUAttrFloat.CritDmgMulDef,
            EBGUAttrFloat.DmgAddition,
            EBGUAttrFloat.DmgDef,
            EBGUAttrFloat.BlockCollapseArmor,
            EBGUAttrFloat.DingshenDefAddition,
            EBGUAttrFloat.FreezeAbnormalAcc,
            EBGUAttrFloat.BurnAbnormalAcc,
            EBGUAttrFloat.PoisonAbnormalAcc,
            EBGUAttrFloat.ThunderAbnormalAcc,
            EBGUAttrFloat.BlindSlot,
            EBGUAttrFloat.FreezeAtk,
            EBGUAttrFloat.BurnAtk,
            EBGUAttrFloat.PoisonAtk,
            EBGUAttrFloat.ThunderAtk,
            EBGUAttrFloat.FreezeDef,
            EBGUAttrFloat.BurnDef,
            EBGUAttrFloat.PoisonDef,
            EBGUAttrFloat.ThunderDef,
            EBGUAttrFloat.BloodBottomNum,
            // EBGUAttrFloat.Pelevel,
            // EBGUAttrFloat.CurEnergy,
            // EBGUAttrFloat.SpecialEnergy,
            // EBGUAttrFloat.Shield,
            // EBGUAttrFloat.Pevalue,
            EBGUAttrFloat.YinAbnormalAcc,
            EBGUAttrFloat.YangAbnormalAcc,
            EBGUAttrFloat.YinAtk,
            EBGUAttrFloat.YangAtk,
            EBGUAttrFloat.YinDef,
            EBGUAttrFloat.YangDef,
            EBGUAttrFloat.ExpDropAddition,
            EBGUAttrFloat.SpiritDropAddition,
            EBGUAttrFloat.StaminaCostMultiper,
            // EBGUAttrFloat.FabaoEnergy,
            // EBGUAttrFloat.VigorEnergy,
            EBGUAttrFloat.CommDropAddition
            // EBGUAttrFloat.AttrFloatMax,
            // EBGUAttrFloat.EnumMax,
        ]);
    }
}