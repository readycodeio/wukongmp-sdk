using System.Collections.Generic;
using BtlShare;
using UnrealEngine.Runtime;
using WukongMp.Api.Helpers;

namespace WukongMp.Api.Configuration
{
    public static class Constants
    {
        public const int ToleratedLatencyMs = 50;
        public const float FloatComparisonTolerance = 0.1f;
        public const string ConnectedPatches = "Connected";
        public const string GlobalPatches = "Global";
        public const string DisabledPatches = "Disabled";
        public const float MonsterSpawnDistance = 2000f;
        public const float MonsterSpawnTraceHeight = 2000f;
        public const float MonsterHalfHeight = 200f;
        public const float MonsterSpawnSpread = 200f;
        public const float CameraArmLength = 720;
        public const float TransformedCameraArmLength = 1100;
        public const int NewCharacterArchiveId = 1;
        public const int MaxPlayers = 10;
        public const int DefaultMonsterTeamId = 2;
        public const int ReconnectDelayMs = 1000;
        public const float RestrictedMovementRadius = 500f;
        public const float RestrictedMovementRadiusSquare = RestrictedMovementRadius * RestrictedMovementRadius;
        public const float MonsterUpdateTargetTime = 7; // seconds
        public const float SpawnOwnershipRadius = 7500f; // 75m
        public const float BaseMarkerHeightCoefficient = 0.12f;
        public const float MaxMarkerHeightDistance = 10000f;
        public const float ColliderDisableTime = 3f; // seconds

        public const string SupremeInspectorFirewallName = "BP_szlc_wanglingguan_mf_hq";
        public static readonly FVector SupremeInspectorFirewallLocation = new(107491.700, 92122.520, 15129.590);

        public static readonly FLinearColor ServerMessageColor = new(0.3f, 0.3f, 0.3f, 1f);
        public static readonly FLinearColor PlayerMessageColor = new(0.9f, 0.9f, 0.9f, 1f);
        public static readonly FLinearColor EnemyPlayerMessageColor = new(1f, 0.3f, 0.3f, 1f);

        public static readonly HashSet<int> InstantTriggerSequences =
        [
            30105200, // act 3 boss transition to 2nd phase
            40104151, // phase 2 of Hundred-Eyed Daoist Master, sword-swallowing cutscene
            62103371, 62103351, 62103321, 62103301 // 4 heavenly kings, lute guy
        ];

        public static readonly HashSet<int> SoloPlaySequences =
        [
            1102021, 1102031, 1103011, // Erlang in the Prologue
            90005015, // Feng-Tail General
            90005016, // Feng-Tail General
            90005017, // Feng-Tail General
            90005018, // Feng-Tail General
        ];

        public const int GourdSkillId = 10530;
        public const int ImmobilizeSkillId = 10518;
        public const int IncenseTrailTalismanSkillId = 10909;
        public const int RuyiScrollSkillId = 10912;
        public const int ConsumableBuffSkillId = 10913;
        public const int IronBodySkillId = 10505;
        public const string ChestCameraLockNode = "CAMERA_LOCK";
        public const string FeetCameraLockNode = "CAMERA_LOCK_Root";
        public const string SpringArmEndSocket = "SpringEndpoint";

        public const string WukongClassPath = "/Game/00Main/Design/Units/Player/Unit_Player_Wukong.Unit_Player_Wukong_C";
        public const string WukongDashengClassPath = "/Game/00Main/Design/Units/Player/Unit_player_dasheng.Unit_player_dasheng_C";

        public const string PlayerMarkerPath = "/Game/Mods/WukongMod/BP_PlayerMarker.BP_PlayerMarker_C";

        public const string DebugCubeActorPath = "/Game/Mods/DebugMod/BP_DebugCube.BP_DebugCube_C";
        public const string DebugSphereActorPath = "/Game/Mods/DebugMod/BP_DebugShpere.BP_DebugShpere_C";
        public const string CoopWorldArchiveName = "world.sav";

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

        public const string ShimFolder = "CSharpLoader/Shims";
    }
}