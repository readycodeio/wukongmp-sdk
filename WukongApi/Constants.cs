using System.Collections.Generic;
using BtlShare;
using WukongApi.Helpers;

namespace WukongApi
{
    public static class Constants
    {
        public const int PlayerTtlMs = 3000;
        public const int ToleratedLatencyMs = 50;
        public const float FloatComparisonTolerance = 0.1f;
        public const string ConnectedPatches = "Connected";
        public const string GlobalPatches = "Global";
        public const float MonsterSpawnDistance = 2000f;
        public const float MonsterSpawnTraceHeight = 10000f;
        public const float MonsterHalfHeight = 200f;
        public const float MonsterSpawnSpread = 200f;
        public const int MonsterSpawnDelayMs = 500;
        public const string AttributePrefix = "a_";
        public const float PvpStartingRadius = 500;
        public const float PvpMonsterRadius = 1000;
        public const float CameraArmLength = 720;
        public const int CharacterArchiveId = 10;
        public const int NewCharacterArchiveId = 9;
        public const int WorldArchiveId = 0;
        public const int MaxPlayers = 20;
        public static readonly List<int> AvailableTeamIds = [-9999, -9998];
        public const int DrawTeamId = 9999;
        public const int ReconnectDelayMs = 1000;

        public static readonly List<int> SkillsWhitelist = [10518];
        public const int GourdSkillId = 10530;
        public const int ImmobilizeSkillId = 10518;
        public const int ConsumableBuffSkillId = 10913;

        public const int CountdownSeconds = 5;
        public const int MatchmakingSeconds = 45;
        public const int RoundSeconds = 0;
        public const int RoundMinutes = 5;

        public const int BotCount = 2;

        public const bool IsCoop = true;

        public const string UiManagerActorPath = "/Game/Mods/CustomLuaMod/BP_UIManager.BP_UIManager_C";
        public const string PlayerMarkerPath = "/Game/Mods/CustomLuaMod/BP_PlayerMarker.BP_PlayerMarker_C";
        public const string ChatWidgetName = "WBP_MultiplayerChat_C";
        public const string TimerWidgetName = "WBP_Timer_C";
        public const string PingWidgetName = "WBP_PingIndicator_C";
        public const string FreeCameraWidgetName = "WBP_FreeCameraControls_C";
        public const string CountdownWidgetName = "WBP_Countdown_C";
        public const string GameMessageWidgetName = "WBP_GameMessage_C";
        public const string InfoMessageWidgetName = "WBP_InfoMessage_C";
        public const string LobbyStatusWidgetName = "WBP_LobbyStatus_C";

        public static readonly EnumSet<EBGUAttrFloat> SyncedAttributes = new([
            #region Calculated Attributes

            // EBGUAttrFloat.Atk,
            // EBGUAttrFloat.BloodBottomNumMax,
            // EBGUAttrFloat.BurnAbnormalAccMax,
            // EBGUAttrFloat.BurnAtk,
            // EBGUAttrFloat.BurnDef,
            // EBGUAttrFloat.CritDmgMulDef,
            // EBGUAttrFloat.CritMultiplier,
            // EBGUAttrFloat.CritRate,
            // EBGUAttrFloat.CritRateDef,
            // EBGUAttrFloat.Def,
            // EBGUAttrFloat.DingshenDefAddition,
            // EBGUAttrFloat.DmgAddition,
            // EBGUAttrFloat.DmgDef,
            // EBGUAttrFloat.EarPlug,
            // EBGUAttrFloat.EnergyConsumeSpeed,
            // EBGUAttrFloat.EnergyIncreaseSpeed,
            // EBGUAttrFloat.EnergyMinConsume,
            // EBGUAttrFloat.FreezeAbnormalAccMax,
            // EBGUAttrFloat.FreezeAtk,
            // EBGUAttrFloat.FreezeDef,
            // EBGUAttrFloat.HpMax,
            // EBGUAttrFloat.KptturnSpeed,
            // EBGUAttrFloat.MpMax,
            // EBGUAttrFloat.PelevelMax,
            // EBGUAttrFloat.PevalueMax,
            // EBGUAttrFloat.PoisonAbnormalAccMax,
            // EBGUAttrFloat.PoisonAtk,
            // EBGUAttrFloat.PoisonDef,
            // EBGUAttrFloat.ShieldMax,
            // EBGUAttrFloat.StaminaCostMultiper,
            // EBGUAttrFloat.StaminaDepletedLimit,
            // EBGUAttrFloat.StaminaMax,
            // EBGUAttrFloat.StaminaRecover,
            // EBGUAttrFloat.Tenacity,
            // EBGUAttrFloat.ThunderAbnormalAccMax,
            // EBGUAttrFloat.ThunderAtk,
            // EBGUAttrFloat.ThunderDef,
            // EBGUAttrFloat.TransEnergyMax,
            // EBGUAttrFloat.YangAbnormalAccMax,
            // EBGUAttrFloat.YangAtk,
            // EBGUAttrFloat.YangDef,
            // EBGUAttrFloat.YinAbnormalAccMax,
            // EBGUAttrFloat.YinAtk,
            // EBGUAttrFloat.YinDef,

            #endregion

            EBGUAttrFloat.AtkBase,
            EBGUAttrFloat.AtkMul,
            // EBGUAttrFloat.AttrFloatMax,
            EBGUAttrFloat.B1Stun,
            EBGUAttrFloat.B1StunMax,
            EBGUAttrFloat.B1StunMaxBase,
            EBGUAttrFloat.B1StunMaxMul,
            EBGUAttrFloat.BlindSlot,
            EBGUAttrFloat.BlindSlotMax,
            EBGUAttrFloat.BlockCollapseArmor,
            EBGUAttrFloat.BlockCollapseArmorMax,
            EBGUAttrFloat.BlockCollapseArmorMaxBase,
            EBGUAttrFloat.BloodBottomNum,
            EBGUAttrFloat.BloodBottomNumMaxBase,
            EBGUAttrFloat.BurnAbnormalAcc,
            EBGUAttrFloat.BurnAbnormalAccMaxBase,
            EBGUAttrFloat.BurnAbnormalAccMaxMul,
            EBGUAttrFloat.BurnAtkBase,
            EBGUAttrFloat.BurnDefBase,
            EBGUAttrFloat.CommDropAddition,
            EBGUAttrFloat.CritDmgMulDefBase,
            EBGUAttrFloat.CritMultiplierBase,
            EBGUAttrFloat.CritRateBase,
            EBGUAttrFloat.CritRateDefBase,
            // EBGUAttrFloat.CurEnergy,
            EBGUAttrFloat.DefBase,
            EBGUAttrFloat.DefMul,
            EBGUAttrFloat.DingshenDefAdditionBase,
            EBGUAttrFloat.DmgAdditionBase,
            EBGUAttrFloat.DmgDefBase,
            EBGUAttrFloat.EarPlugBase,
            EBGUAttrFloat.EnergyConsumeSpeedBase,
            EBGUAttrFloat.EnergyConsumeSpeedMul,
            EBGUAttrFloat.EnergyIncreaseSpeedBase,
            EBGUAttrFloat.EnergyIncreaseSpeedMul,
            EBGUAttrFloat.EnergyMinConsumeBase,
            EBGUAttrFloat.EnergyMinConsumeMul,
            // EBGUAttrFloat.EnumMax,
            EBGUAttrFloat.ExpDropAddition,
            // EBGUAttrFloat.FabaoEnergy,
            // EBGUAttrFloat.FabaoEnergyMax,
            EBGUAttrFloat.FreezeAbnormalAcc,
            EBGUAttrFloat.FreezeAbnormalAccMaxBase,
            EBGUAttrFloat.FreezeAbnormalAccMaxMul,
            EBGUAttrFloat.FreezeAtkBase,
            EBGUAttrFloat.FreezeDefBase,
            // EBGUAttrFloat.Hp,
            EBGUAttrFloat.HpMaxBase,
            EBGUAttrFloat.HpMaxMul,
            EBGUAttrFloat.KptturnSpeedBase,
            EBGUAttrFloat.KptturnSpeedMul,
            // EBGUAttrFloat.Mp,
            EBGUAttrFloat.MpMaxBase,
            EBGUAttrFloat.MpMaxMul,
            EBGUAttrFloat.Pelevel,
            EBGUAttrFloat.PelevelMaxBase,
            EBGUAttrFloat.Pevalue,
            EBGUAttrFloat.PevalueMaxBase,
            EBGUAttrFloat.PoisonAbnormalAcc,
            EBGUAttrFloat.PoisonAbnormalAccMaxBase,
            EBGUAttrFloat.PoisonAbnormalAccMaxMul,
            EBGUAttrFloat.PoisonAtkBase,
            EBGUAttrFloat.PoisonDefBase,
            EBGUAttrFloat.Shield,
            EBGUAttrFloat.ShieldMaxBase,
            EBGUAttrFloat.SkillSuperArmor,
            EBGUAttrFloat.SkillSuperArmorMax,
            EBGUAttrFloat.SkillSuperArmorMaxBase,
            // EBGUAttrFloat.SpecialEnergy,
            // EBGUAttrFloat.SpecialEnergyMax,
            EBGUAttrFloat.SpiritDropAddition,
            // EBGUAttrFloat.Stamina,
            EBGUAttrFloat.StaminaCostMultiperBase,
            EBGUAttrFloat.StaminaCostMultiperMul,
            EBGUAttrFloat.StaminaDepletedLimitBase,
            EBGUAttrFloat.StaminaDepletedLimitMul,
            EBGUAttrFloat.StaminaMaxBase,
            EBGUAttrFloat.StaminaMaxMul,
            EBGUAttrFloat.StaminaRecoverBase,
            EBGUAttrFloat.StaminaRecoverMul,
            EBGUAttrFloat.TenacityBase,
            EBGUAttrFloat.ThunderAbnormalAcc,
            EBGUAttrFloat.ThunderAbnormalAccMaxBase,
            EBGUAttrFloat.ThunderAbnormalAccMaxMul,
            EBGUAttrFloat.ThunderAtkBase,
            EBGUAttrFloat.ThunderDefBase,
            EBGUAttrFloat.TransEnergyMaxBase,
            EBGUAttrFloat.TransEnergyMaxMul,
            // EBGUAttrFloat.VigorEnergy,
            // EBGUAttrFloat.VigorEnergyMax,
            EBGUAttrFloat.YangAbnormalAcc,
            EBGUAttrFloat.YangAbnormalAccMaxBase,
            EBGUAttrFloat.YangAbnormalAccMaxMul,
            EBGUAttrFloat.YangAtkBase,
            EBGUAttrFloat.YangDefBase,
            EBGUAttrFloat.YinAbnormalAcc,
            EBGUAttrFloat.YinAbnormalAccMaxBase,
            EBGUAttrFloat.YinAbnormalAccMaxMul,
            EBGUAttrFloat.YinAtkBase,
            EBGUAttrFloat.YinDefBase,
        ]);
    }
}