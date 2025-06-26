using WukongMp.Api.Configuration;

namespace WukongMp.Api;

public class WukongPatcher() : HarmonyPatcherBase("ReadyM.WukongMp")
{
    protected override void OnPatch()
    {
        base.OnPatch();

        Harmony.PatchCategory(Constants.GlobalPatches);
        Logging.LogInformation("Patched Harmony category: {Category}", Constants.GlobalPatches);
        
        Harmony.PatchCategory(Constants.ConnectedPatches);
        Logging.LogInformation("Patched Harmony category: {Category}", Constants.ConnectedPatches);

        const string category = Constants.IsCoop ? Constants.CoopPatches : Constants.PvpPatches;
        Harmony.PatchCategory(category);
        Logging.LogInformation("Patched Harmony WukongMpMod {Patch}", category);
    }
    
    protected override void OnUnpatch()
    {
        const string category = Constants.IsCoop ? Constants.CoopPatches : Constants.PvpPatches;
        Harmony.UnpatchCategory(category);
        Logging.LogInformation("Unpatched Harmony WukongMpMod {Patch}", category);
        
        Harmony.UnpatchCategory(Constants.ConnectedPatches);
        Logging.LogInformation("Unpatched Harmony category: {Category}", Constants.ConnectedPatches);

        Harmony.UnpatchCategory(Constants.GlobalPatches);
        Logging.LogInformation("Unpatched Harmony category: {Category}", Constants.GlobalPatches);
        
        base.OnUnpatch();
    }
}