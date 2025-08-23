using b1;
using WukongMp.Api.Configuration;

namespace WukongMp.Api.WukongUtils
{
    public static class IronBodyUtils
    {
        public static void TriggerIronBody(BGUCharacterCS pawn)
        {
            Logging.LogDebug("Received trigger iron body for character {Nickname}", pawn.GetName());
            var playerEvents = BUS_EventCollectionCS.Get(pawn);
            BGUFunctionLibraryCS.BGUTryCastSpell(pawn, Constants.IronBodySkillId, ECastSkillSourceType.Default);
        }
    }
}
