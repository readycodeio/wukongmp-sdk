using PreludeLib.Runtime.Public;
using WukongMp.Api;

namespace WukongMp.Tests;

public class TestPatcher(RuntimePrelude prelude) : WukongPatcher(typeof(Mod).Assembly, "WukongMp.Tests", prelude);