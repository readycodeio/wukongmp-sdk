using PreludeLib.Common;
using PreludeLib.Runtime.Public;
using WukongMp.Api.Configuration;

namespace WukongMp.Api;

public class WukongPatcher(RuntimePrelude prelude) : PreludePatcherBase("ReadyM.WukongMp", prelude)
{
    protected override void OnPatch()
    {
        base.OnPatch();

        Prelude.ScanAndPatchCategory(new(Constants.GlobalPatches));
        Logging.LogInformation("Patched Prelude category: {Category}", Constants.GlobalPatches);
        
        Prelude.ScanAndPatchCategory(new(Constants.ConnectedPatches));
        Logging.LogInformation("Patched Prelude category: {Category}", Constants.ConnectedPatches);

        var category = Constants.IsCoop ? Constants.CoopPatches : Constants.PvpPatches;
        Prelude.ScanAndPatchCategory(new(category));
        Logging.LogInformation("Patched Prelude WukongMpMod {Patch}", category);
    }
    
    protected override void OnUnpatch()
    {
        var category = Constants.IsCoop ? Constants.CoopPatches : Constants.PvpPatches;
        Prelude.UnpatchCategory(new(category));
        Logging.LogInformation("Unpatched Prelude WukongMpMod {Patch}", category);
        
        Prelude.UnpatchCategory(new(Constants.ConnectedPatches));
        Logging.LogInformation("Unpatched Prelude category: {Category}", Constants.ConnectedPatches);

        Prelude.UnpatchCategory(new(Constants.GlobalPatches));
        Logging.LogInformation("Unpatched Prelude category: {Category}", Constants.GlobalPatches);
        
        base.OnUnpatch();
    }
}