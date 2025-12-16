using System.Text.RegularExpressions;

namespace WukongMp.Api.PathCompressors;

public class NameCompressor
{
    private readonly string _commonFolder;
    private readonly Regex _longNameRegex;
    private readonly Regex _shortNameRegex;

    public NameCompressor(string commonFolder, Regex longNameRegex, Regex shortNameRegex)
    {
        _commonFolder = commonFolder;
        _longNameRegex = longNameRegex;
        _shortNameRegex = shortNameRegex;
    }

    public bool Compress(string? fullName, out string shortName)
    {
        if (fullName is null)
        {
            shortName = "";   
            return false;
        }
        
        var match = _longNameRegex.Match(fullName);
        if (match.Success)
        {
            if (match.Groups[2].Value != match.Groups[3].Value)
            {
                Logging.LogError("Found full name with mismatched package/asset name: {FullName}", fullName);
                shortName = "";
                return false;
            }

            shortName = $"{match.Groups[1].Value}/{match.Groups[2].Value}";
            return true;
        }

        Logging.LogDebug("Failed to compress asset name: {FullName}", fullName);
        shortName = "";
        return false;
    }

    public string Decompress(string shortName)
    {
        var match = _shortNameRegex.Match(shortName);
        if (match.Success)
        {
            return $"{_commonFolder}/{match.Groups[1].Value}/{match.Groups[2].Value}.{match.Groups[2].Value}";
        }

        Logging.LogError("Failed to decompress asset name: {ShortName}", shortName);
        return "";
    }
}