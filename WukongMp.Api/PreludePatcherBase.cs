using PreludeLib.Runtime.Public;
using ReadyM.Api;

namespace WukongMp.Api;

public class PreludePatcherBase(string harmonyId, RuntimePrelude prelude) : PatcherBase
{
    protected readonly RuntimePreludeBuilder Prelude = prelude.Create(harmonyId);
}