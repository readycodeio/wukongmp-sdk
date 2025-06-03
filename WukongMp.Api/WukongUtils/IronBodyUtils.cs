using b1;
using WukongMp.Api.Configuration;
using WukongMp.Api.Old;
using WukongMp.Api.Patches;

namespace WukongMp.Api.WukongUtils
{
    public static class IronBodyUtils
    {
        public static void TriggerIronBody(BGUCharacterCS pawn)
        {
            GameLoopPatch.QueueOnGameThread(() =>
            {
                Logging.LogDebug("Received trigger iron body for character {Nickname}", pawn.GetName());
                var playerEvents = BUS_EventCollectionCS.Get(pawn);
                BGUFunctionLibraryCS.BGUTryCastSpell(pawn, Constants.IronBodySkillId, ECastSkillSourceType.Default);
            }, nameof(TriggerIronBody));
        }
    }
}
