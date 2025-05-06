using System.Text.RegularExpressions;

namespace WukongApi.Localization
{
    public  class Localizer
    {
        private static readonly Regex LocalizationRegex = new(@"\{(.*?)\}", RegexOptions.Compiled);

        public static string LocalizeMessage(string message)
        {
            Resources.Texts.ResourceManager.GetString("CameraDownDescription", Resources.Texts.Culture);
            return LocalizationRegex.Replace(message, match =>
            {
                var key = match.Groups[1].Value;
                var translated = Resources.Texts.ResourceManager.GetString(key, Resources.Texts.Culture);
                return translated ?? match.Value; // fallback: leave untranslated
            });
        }
    }
}
