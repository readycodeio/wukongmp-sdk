using b1;
using WukongMp.Api.Configuration;

namespace WukongMp.Api.WukongUtils
{
    public static class SkillsUtils
    {
        public static bool IsSkillWhitelisted(int skillId)
        {
            return Constants.IsCoop || Constants.SkillsWhitelist.Contains(skillId);
        }

        public static void DisableVigorSkill(BGUCharacterCS character)
        {
            var events = BUS_EventCollectionCS.Get(character);
            if (events != null)
            {
                events.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.CantInVigorSkill);
            }
        }

        public static void DisableFaBaoSkill(BGUCharacterCS character)
        {
            var events = BUS_EventCollectionCS.Get(character);
            if (events != null)
            {
                events.Evt_UnitSetSimpleState.Invoke(EBGUSimpleState.CantCastFaBao);
            }
        }
    }
}
