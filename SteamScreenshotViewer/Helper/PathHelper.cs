namespace SteamScreenshotViewer.Helper;

public static class PathHelper
{
    /// <summary>
    /// <p>
    /// Removes the last 2 directories from a path.
    /// </p>
    /// Example:
    /// <br/>
    /// C:\Program Files (x86)\Steam\userdata\{steam id}\760\remote\app id\screenshots
    /// <br/>
    /// is reduced to
    /// <br/>
    /// C:\Program Files (x86)\Steam\userdata\{steam id}\760\remote
    /// </summary>
    public static string ResolveScreenshotBasePath(string pathToASpecificGamesScreenshots)
    {
        char[] path = pathToASpecificGamesScreenshots.ToCharArray();
        int separatorsFound = 0;
        int i = path.Length - 1;
        for (; i >= 0; i--)
        {
            if (path[i] == System.IO.Path.DirectorySeparatorChar)
            {
                separatorsFound++;
                if (separatorsFound == 2)
                {
                    break;
                }
            }
        }

        return pathToASpecificGamesScreenshots.Substring(0, i);
    }
}