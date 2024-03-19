using System.IO;
using System.Text.Json;

namespace SteamScreenshotViewer;

public class Config
{
    private const string configPath = configSuperDir + "/Config.json";
    private const string configSuperDir = "plumbing";

    private static Config? _config;

    public static Config Instance
    {
        get
        {
            if (_config is null)
            {
                Instance = DeserializeConfig();
            }

            Thread.MemoryBarrier();
            return _config;
        }

        set
        {
            Thread.MemoryBarrier();
            _config = value;
            Thread.MemoryBarrier();
        }
    }

    public string ScreenshotBasePath { get; set; }

    private static Config DeserializeConfig()
    {
        if (!Exists())
        {
            throw new InvalidOperationException("config file does not exists!");
        }

        return JsonSerializer.Deserialize<Config>(File.ReadAllText(configPath));
    }

    public static bool Exists()
    {
        return Path.Exists(configPath);
    }

    public static Config CreateEmpty()
    {
        if (_config is not null)
        {
            throw new InvalidOperationException("an existing config has already been loaded");
        }

        // instance will indirectly be updated
        // callee must use Serialize() to save changes made to this instance
        // calling Instance afterwards will deserialize the new config
        return new Config();
    }

    public void Commit()
    {
        string json = JsonSerializer.Serialize<Config>(this);
        // create dir if missing
        Directory.CreateDirectory(configSuperDir);
        File.WriteAllText(configPath, json);
    }
}