using System.Reflection;
using PreludeLib.Runtime.Public;
using WukongMp.Api.Configuration;

namespace WukongMp.Api;

public class WukongPatcher(RuntimePrelude prelude) : PreludePatcherBase("ReadyM.WukongMp", prelude)
{
    protected override void OnPatch()
    {
        base.OnPatch();

        Prelude.ScanAndPatchCategory(Assembly.GetExecutingAssembly(), new(Constants.GlobalPatches));
        Logging.LogInformation("Patched Prelude category: {Category}", Constants.GlobalPatches);

        Prelude.ScanAndPatchCategory(Assembly.GetExecutingAssembly(), new(Constants.ConnectedPatches));
        Logging.LogInformation("Patched Prelude category: {Category}", Constants.ConnectedPatches);
    }

    protected override void OnUnpatch()
    {
        Prelude.UnpatchCategory(Assembly.GetExecutingAssembly(), new(Constants.ConnectedPatches));
        Logging.LogInformation("Unpatched Prelude category: {Category}", Constants.ConnectedPatches);

        Prelude.UnpatchCategory(Assembly.GetExecutingAssembly(), new(Constants.GlobalPatches));
        Logging.LogInformation("Unpatched Prelude category: {Category}", Constants.GlobalPatches);

        base.OnUnpatch();
    }
}