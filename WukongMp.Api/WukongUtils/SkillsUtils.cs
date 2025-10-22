using WukongMp.Api.Configuration;

namespace WukongMp.Api.WukongUtils;

public static class SkillsUtils
{
    public static bool IsSkillBlacklisted(int skillId)
    {
        return Constants.IsPvP && Constants.SkillsBlacklist.Contains(skillId);
    }
}