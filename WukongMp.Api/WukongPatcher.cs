using System.Reflection;
using PreludeLib.Runtime.Public;
using WukongMp.Api.Configuration;

namespace WukongMp.Api;

internal class WukongPatcher(Assembly assembly, string modName, RuntimePrelude prelude) : PreludePatcherBase(modName, prelude)
{
    protected override void OnPatch()
    {
        base.OnPatch();

        Prelude.ScanAndPatchCategory(assembly, new(PatchCategory.Global));
        Logging.LogInformation("Patched Prelude category: {Category}", PatchCategory.Global);

        Prelude.ScanAndPatchCategory(assembly, new(PatchCategory.Connected));
        Logging.LogInformation("Patched Prelude category: {Category}", PatchCategory.Connected);
    }

    protected override void OnUnpatch()
    {
        Prelude.UnpatchCategory(assembly, new(PatchCategory.Connected));
        Logging.LogInformation("Unpatched Prelude category: {Category}", PatchCategory.Connected);

        Prelude.UnpatchCategory(assembly, new(PatchCategory.Global));
        Logging.LogInformation("Unpatched Prelude category: {Category}", PatchCategory.Global);

        base.OnUnpatch();
    }
}