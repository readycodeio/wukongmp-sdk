using System.Text.RegularExpressions;
using WukongMp.Api.PathCompressors;

namespace WukongMp.Api.NameCompressors;

public static class Compressors
{
    public static readonly NameCompressor MontageNameCompressor = new(
        "/Game/00Main/Animation",
        new(@"/Game/00Main/Animation/([\w/]+)/(\w+)\.(\w+)", RegexOptions.Compiled),
        new(@"([\w/]+)/(\w+)", RegexOptions.Compiled)
    );

    public static readonly NameCompressor VigorNameCompressor = new(
        "/Game/00MainHZ/Characters/Transform/VigorSkill",
        new Regex(@"/Game/00MainHZ/Characters/Transform/VigorSkill/([\w/]+)/(\w+)\.(\w+)", RegexOptions.Compiled),
        new Regex(@"([\w/]+)/(\w+)", RegexOptions.Compiled)
    );
}
