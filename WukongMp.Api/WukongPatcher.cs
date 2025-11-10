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

        if (Constants.IsCoop)
        {
            Prelude.ScanAndPatchCategory(new(Constants.CoopPatches));
            Logging.LogInformation("Patched Prelude WukongMpMod {Patch}", Constants.CoopPatches);
        }
    }
    
    protected override void OnUnpatch()
    {
        if (Constants.IsCoop)
        {
            Prelude.UnpatchCategory(new(Constants.CoopPatches));
            Logging.LogInformation("Unpatched Prelude WukongMpMod {Patch}", Constants.CoopPatches);
        }
        
        Prelude.UnpatchCategory(new(Constants.ConnectedPatches));
        Logging.LogInformation("Unpatched Prelude category: {Category}", Constants.ConnectedPatches);

        Prelude.UnpatchCategory(new(Constants.GlobalPatches));
        Logging.LogInformation("Unpatched Prelude category: {Category}", Constants.GlobalPatches);
        
        base.OnUnpatch();
    }
}