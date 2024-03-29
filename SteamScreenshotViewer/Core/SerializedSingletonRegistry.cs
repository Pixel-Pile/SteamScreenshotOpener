using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using Serilog;
using SteamScreenshotViewer.Constants;

namespace SteamScreenshotViewer.Core;

public class SerializedSingletonRegistry
{
    private static ILogger log = Log.ForContext(typeof(SerializedSingletonRegistry));

    private static readonly Dictionary<Type, string> _pathsByType = new(2);
    private static Dictionary<Type, object> _locksByType = new();

    static SerializedSingletonRegistry()
    {
        // types cannot register themselves
        // because their code can only ever be executed after they were loaded
        // -> e.g. static constructors won't necessarily be executed
        RegisterType<Config>(Paths.ConfigFile);
        RegisterType<Cache>(Paths.CacheFile);
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

    /// <summary>
    /// Loads the singleton instance of type T.
    /// Returns false (and instance = null) if no instance was posted and the deserialization path for that type
    /// does not exist.
    /// </summary>
    /// <param name="instance"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static bool TryGetInstance<T>([MaybeNullWhen(false)] out T instance) where T : class
    {
        lock (_locksByType[typeof(T)]) // prevent multiple instantiiation of type T by concurrent access
        {
            string path;
            lock (dataStructuresLock) // protect data structures
            {
                if (InstancesByType.TryGetValue(typeof(T), out object? objInstance))
                {
                    Debug.Assert(objInstance is not null);
                    instance = (T)objInstance;
                    return true;
                }

                // get path before releasing data structure lock
                path = _pathsByType[typeof(T)];
            } // data structure lock is released

            // deserialize while holding type lock 
            if (!Path.Exists(path))
            {
                instance = null;
                return false;
            }

            T deserialized = Deserialize<T>(path);

            lock (dataStructuresLock) // protect data structure
            {
                InstancesByType[typeof(T)] = deserialized;
                instance = deserialized;
                return true;
            }
        } // release type lock
    }

    public static void Post<T>(T obj, bool allowAlreadyInstantiated = false)
    {
        log.Information($"posting new instance for type {typeof(T).Name}");
        _ = obj ?? throw new NullReferenceException("null is not a valid singleton instance");

        lock (_locksByType[typeof(T)]) // prevent concurrent serialization of same type
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
        }
    }

    public static void PostAndSerialize<T>(T obj, bool allowAlreadyInstantiated = false)
    {
        _ = obj ?? throw new NullReferenceException("null is not a valid singleton instance");

        string path = _pathsByType[typeof(T)];
        // lock type lock so store & serialize for a type cannot be interrupted
        // by concurrent store/serialization attempts
        lock (_locksByType[typeof(T)])
        {
            Post<T>(obj, allowAlreadyInstantiated);
            Serialize<T>(path, obj);
        }
    }

    private static T Deserialize<T>(string path)
    {
        log.Information($"deserializing instance of type {typeof(T).Name}");

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
        log.Information($"serializing instance of type {typeof(T).Name}");

        // create dir if missing
        string directoryPath = path.Substring(0, path.LastIndexOf("/"));
        Directory.CreateDirectory(directoryPath);

        string json = JsonSerializer.Serialize<T>(obj);
        File.WriteAllText(path, json);
    }
}