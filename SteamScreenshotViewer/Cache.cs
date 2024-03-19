using System.IO;
using System.Text.Json;

namespace SteamScreenshotViewer;

public static class Cache
{
    private const string cachePath = @"plumbing/cache.json";

    public static Dictionary<string, string> LoadIds()
    {
        if (Path.Exists(cachePath))
        {
            string json = File.ReadAllText(cachePath);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                   ?? throw new JsonException("could not load cache file");
        }

        return new Dictionary<string, string>();
    }

    public static void StoreIds(Dictionary<string, string> cachedNamesToIds)
    {
        string data = JsonSerializer.Serialize(cachedNamesToIds);

        // remember that this writes to debug/release directory and is not visible in ide
        File.WriteAllText(cachePath, data);
    }
}