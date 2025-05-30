using WukongMp.Api.Configuration;

namespace WukongMp.Api.WukongUtils
{
    public static class SkillsUtils
    {
        public static bool IsSkillWhitelisted(int skillId)
        {
            return Constants.IsCoop || Constants.SkillsWhitelist.Contains(skillId);
        }
    }
}
