using BtlShare;
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
        public const string DefaultPhotonUserName = "ReadyM_noname";
        public const float MonsterSpawnDistance = 2000f;
        public const float MonsterSpawnTraceHeight = 10000f;
        public const float MonsterHalfHeight = 200f;
        public const float MonsterSpawnSpread = 100f;
        public const int MonsterSpawnDelayMs = 500;
        public const int BaseTeamId = -9999;
        public const string AttributePrefix = "attr_";
        public static readonly FVector PvpStartingLocation = new FVector(-11146.926, -3229.771, 6497.035);
        public const float PvpRadius = 4000;

        public const string ModActorPath = "/Game/Mods/CustomLuaMod/ModActor.ModActor_C";
        public const string ChatWidgetName = "WBP_MultiplayerChat_C";

        public static readonly EnumSet<EBGUAttrFloat> SyncedAttributes = new EnumSet<EBGUAttrFloat>(new[]
        {
            EBGUAttrFloat.HpMax,
            EBGUAttrFloat.HpMaxBase,
            EBGUAttrFloat.HpMaxMul,
            EBGUAttrFloat.AtkBase,
            EBGUAttrFloat.DefBase,
            EBGUAttrFloat.CritRateBase,
            EBGUAttrFloat.DmgDefBase,
            EBGUAttrFloat.BurnDefBase,
            EBGUAttrFloat.BurnAbnormalAccMaxMul,
            EBGUAttrFloat.PoisonDefBase,
            EBGUAttrFloat.PoisonAbnormalAccMaxMul,
            EBGUAttrFloat.FreezeDefBase,
            EBGUAttrFloat.FreezeAbnormalAccMaxMul,
            EBGUAttrFloat.PoisonAtkBase,
            EBGUAttrFloat.PevalueMaxBase,
            EBGUAttrFloat.FabaoEnergyMax,
            EBGUAttrFloat.CritMultiplierBase,
            EBGUAttrFloat.VigorEnergyMax,
            EBGUAttrFloat.ThunderAtkBase,
            EBGUAttrFloat.CommDropAddition,
            EBGUAttrFloat.ThunderDefBase,
            EBGUAttrFloat.ThunderAbnormalAccMaxMul,
            EBGUAttrFloat.DmgAdditionBase,
            EBGUAttrFloat.SpiritDropAddition,
            EBGUAttrFloat.ExpDropAddition,
            EBGUAttrFloat.FreezeAtkBase,
            EBGUAttrFloat.BurnAtkBase,
            EBGUAttrFloat.EnergyConsumeSpeedMul,
            EBGUAttrFloat.BloodBottomNumMaxBase,
        });
    }
}