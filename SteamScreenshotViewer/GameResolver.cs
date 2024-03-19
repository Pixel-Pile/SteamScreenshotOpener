using System.IO;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace SteamScreenshotViewer;

public class GameResolver
{
    HttpClient httpClient = new();

    public async Task<SteamApp[]> FindGameDirectories()
    {
        List<string> appScreenshotPaths = new();
        Dictionary<string, string> cachedNamesToIds = Cache.LoadIds();
        IEnumerable<string> screenshotDirectories = Directory.EnumerateDirectories(Config.Instance.ScreenshotBasePath);
        foreach (string path in screenshotDirectories)
        {
            string appId = GetAppIdFromScreenshotDirectoryPath(path);
            if (cachedNamesToIds.ContainsValue(appId))
            {
                // cached
                appScreenshotPaths.Add(path);
                Console.WriteLine("already cached:" + appId);
            }
            else
            {
                //not cached
                string? name = await GetAppNameAsync(appId);
                if (name is not null)
                {
                    // name found
                    appScreenshotPaths.Add(path);
                    cachedNamesToIds.Add(name, appId);
                    Console.WriteLine(name);
                }
                else
                {
                    // name not found
                    Console.WriteLine("could not resolve name for id: " + appId);
                }
            }
        }

        Cache.StoreIds(cachedNamesToIds);
        httpClient.Dispose();

        SteamApp[] apps = new SteamApp[appScreenshotPaths.Count];

        for (int i = 0; i < appScreenshotPaths.Count; i++)
        {
            string path = appScreenshotPaths[i] + @"\screenshots";
            string name = cachedNamesToIds.ElementAt(i).Key;
            string id = cachedNamesToIds.ElementAt(i).Value;
            apps[i] = new SteamApp(id, name, path);
        }

        return apps;
    }


    private async Task<string?> GetAppNameAsync(string appId)
    {
        try
        {
            string response =
                await httpClient.GetStringAsync(
                    $"https://store.steampowered.com/api/appdetails??filter=basic&appids={appId}");
            JsonNode responseJson = JsonObject.Parse(response)[appId];
            if (responseJson["success"].ToString() == "false")
            {
                return null;
            }

            JsonNode appData = responseJson["data"];
            return appData["name"].ToString();
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"request for {appId} failed");
            throw;
        }
    }


    private string GetAppIdFromScreenshotDirectoryPath(string directory)
    {
        int idStart = directory.LastIndexOf(@"\") + 1;
        string id = directory.Substring(idStart, directory.Length - idStart);
        if (id is null)
        {
            throw new NullReferenceException("id is null for path: " + directory);
        }

        return id;
    }
}