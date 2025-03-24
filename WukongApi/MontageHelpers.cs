using System.Text.RegularExpressions;

namespace WukongApi;

public static class MontageHelpers
{
    private const string CommonMontageFolder = "/Game/00Main/Animation";
    private static readonly Regex ShortMontageSplitRegex = new(@"([\w/]+)/(\w+)", RegexOptions.Compiled);
    private static readonly Regex LongMontageSplitRegex = new(@"/Game/00Main/Animation/([\w/]+)/(\w+)\.(\w+)", RegexOptions.Compiled);

    public static string CompressMontageName(string fullName)
    {
        var match = LongMontageSplitRegex.Match(fullName);
        if (match.Success)
        {
            if (match.Groups[2].Value != match.Groups[3].Value)
            {
                Logging.LogError("Found montage with mismatched package/asset name: {MontageName}", fullName);
                return "";
            }

            return $"{match.Groups[1].Value}/{match.Groups[2].Value}";
        }

        Logging.LogError("Failed to compress montage name: {MontageName}", fullName);
        return "";
    }

    public static string DecompressMontageName(string shortName)
    {
        var match = ShortMontageSplitRegex.Match(shortName);
        if (match.Success)
        {
            return $"{CommonMontageFolder}/{match.Groups[1].Value}/{match.Groups[2].Value}.{match.Groups[2].Value}";
        }

        Logging.LogError("Failed to decompress montage name: {MontageName}", shortName);
        return "";
    }
}