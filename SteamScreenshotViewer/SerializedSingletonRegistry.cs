using System.IO;
using System.Text.Json;

namespace SteamScreenshotViewer;

public class SerializedSingletonRegistry
{
    private static readonly Dictionary<Type, string> _pathsByType = new(2);
    private static Dictionary<Type, object> _locksByType = new();

    static SerializedSingletonRegistry()
    {
        // types cannot register themselves
        // because their code can only ever be executed after they were loaded
        // -> e.g. static constructors won't necessarily be executed
        RegisterType<Config>(Config.configPath);
        RegisterType<Cache>(Cache.cachePath);
    }

    public static void RegisterType<T>(string path)
    {
        lock (dataStructuresLock)
        {
            _pathsByType[typeof(T)] = path;
            _locksByType[typeof(T)] = new();
        }
    }


    private static object dataStructuresLock = new();
    private static Dictionary<Type, object> InstancesByType = new();

    public static T Load<T>()
    {
        
        lock (_locksByType[typeof(T)]) // prevent multiple instantiiation of type T by concurrent access
        {
            string path;
            lock (dataStructuresLock) // protect data structures
            {
                if (InstancesByType.TryGetValue(typeof(T), out object instance))
                {
                    return (T)instance;
                }

                // get path before releasing data structure lock
                path = _pathsByType[typeof(T)];
            } // data structure lock is released

            // deserialize while holding type lock 
            T deserialized = Deserialize<T>(path);

            lock (dataStructuresLock) // protect data structure
            {
                InstancesByType[typeof(T)] = deserialized;
                return deserialized;
            }
        } // release type lock
    }

    public static void StoreAndSerialize<T>(T obj, bool allowAlreadyInstantiated = false)
    {
        string path = _pathsByType[typeof(T)];
        lock (_locksByType[typeof(T)]) // prevent concurrent deserialization of same type
        {
            lock (dataStructuresLock)
            {
                if (!allowAlreadyInstantiated)
                {
                    if (InstancesByType.ContainsKey(typeof(T)))
                    {
                        throw new InvalidOperationException("the singleton was already instaniated");
                    }
                }

                // store instance while still holding data lock
                InstancesByType[typeof(T)] = obj;
            }

            Serialize<T>(path, obj); // serialize while holding type lock
        }
    }


    private static T Deserialize<T>(string path)
    {
        if (!Path.Exists(path))
        {
            throw new InvalidOperationException("json file does not exists!");
        }

        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json) ??
               throw new JsonException("failed to deserialize type: " + typeof(T).Name);
    }

    private static void Serialize<T>(string path, T obj)
    {
        // create dir if missing
        string directoryPath = path.Substring(0, path.LastIndexOf("/"));
        Directory.CreateDirectory(directoryPath);

        string json = JsonSerializer.Serialize<T>(obj);
        File.WriteAllText(path, json);
    }
}