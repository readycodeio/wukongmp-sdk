using System.Collections.Generic;
using System.Linq;
using BtlShare;
using WukongMp.Api.Old;

namespace WukongMp.Api.GameApi.Configuration
{
    internal static class SkillsConfig
    {
        private static readonly Dictionary<SkillKind, SkillData> Configurations = new()
        {
            { SkillKind.None, new SkillData(EInputActionType.None, 0, -1, -1) },
            { SkillKind.RingOfFire, new SkillData(EInputActionType.UseSkillByType, 10520, 250, -1) },
            { SkillKind.RockSolid, new SkillData(EInputActionType.UseSkillByType, 10505, 230, -1) },
            { SkillKind.PluckOfMany, new SkillData(EInputActionType.UseSkillByType, 10516, 240, -1) },
            { SkillKind.SupremeGourd, new SkillData(EInputActionType.CastItemSkill, 10530, -1, -1) },
        };

        public static SkillData GetSkillData(SkillKind skillKind)
        {
            if (Configurations.TryGetValue(skillKind, out var value))
            {
                return value;
            }

            Logging.LogError("Skill data for '{Kind}' not found.", skillKind);
            return Configurations.First().Value;
        }
    }
}