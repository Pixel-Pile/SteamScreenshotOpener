using System.Text.RegularExpressions;

namespace SteamScreenshotViewer.Helper;

public static partial class StringHelper
{
    private const string RemoveDuplicateWhitespacePattern = @"\s{2,}";

    [GeneratedRegex(RemoveDuplicateWhitespacePattern)]
    private static partial Regex RemoveDuplicateWhitespaceRegex();

    public static string RemoveDuplicateWhitespace(string newValue)
    {
        return RemoveDuplicateWhitespaceRegex().Replace(newValue.Trim(), " ");
    }
}