using System.Reflection;
using CSharpModBase;
using PreludeLib.Runtime.Public;
using WukongMp.Api.Configuration;

namespace WukongMp.Api;

public class WukongPatcher(Assembly assembly, string modName, RuntimePrelude prelude) : PreludePatcherBase(modName, prelude)
{
    protected override void OnPatch()
    {
        base.OnPatch();

        Prelude.ScanAndPatchCategory(assembly, new(Constants.GlobalPatches));
        Logging.LogInformation("Patched Prelude category: {Category}", Constants.GlobalPatches);

        Prelude.ScanAndPatchCategory(assembly, new(Constants.ConnectedPatches));
        Logging.LogInformation("Patched Prelude category: {Category}", Constants.ConnectedPatches);
    }

    protected override void OnUnpatch()
    {
        Prelude.UnpatchCategory(assembly, new(Constants.ConnectedPatches));
        Logging.LogInformation("Unpatched Prelude category: {Category}", Constants.ConnectedPatches);

        Prelude.UnpatchCategory(assembly, new(Constants.GlobalPatches));
        Logging.LogInformation("Unpatched Prelude category: {Category}", Constants.GlobalPatches);

        base.OnUnpatch();
    }
}