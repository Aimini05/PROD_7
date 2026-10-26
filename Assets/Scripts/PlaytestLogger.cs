using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Local playtest telemetry. One JSON file per gameplay scene/run.</summary>
public sealed class PlaytestLogger : MonoBehaviour
{
    [Serializable]
    private sealed class Session
    {
        public int schemaVersion = 2;
        public string playsession;
        public string startedUtc;
        public string endedUtc;
        public string scene;
        public List<Entry> events = new List<Entry>();
    }

    [Serializable]
    private sealed class Entry
    {
        public int sequence;
        public string timestampUtc;
        public double elapsedSeconds;
        public string eventName;
        public Vector3 position;
        public string objectId;
        public string objectName;
        public Vector3 objectPosition;
        public int keysRemaining = -1;
    }

    private static PlaytestLogger instance;
    private Session session;
    private string filePath;
    private double startedAt;
    private bool warnedAboutSave;
    private bool dogReached;
    private bool playerCaught;
    private readonly HashSet<string> reportedLockedDoors = new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Ignore additive content loads: these are not new attempts.
        if (mode == LoadSceneMode.Additive) return;
        if (instance != null) instance.EndSession();

        bool gameplay = false;
        foreach (PlayerWhistle whistle in FindObjectsByType<PlayerWhistle>(FindObjectsSortMode.None))
            if (whistle.gameObject.scene == scene) gameplay = true;
        if (!gameplay) return;

        if (instance == null)
        {
            GameObject logger = new GameObject("Playtest JSON Logger");
            instance = logger.AddComponent<PlaytestLogger>();
            DontDestroyOnLoad(logger);
        }
        instance.BeginSession(scene.name);
    }

    private void BeginSession(string sceneName)
    {
        session = new Session
        {
            playsession = Guid.NewGuid().ToString("N"),
            startedUtc = UtcNow(),
            scene = sceneName
        };
        startedAt = Time.realtimeSinceStartupAsDouble;
        dogReached = false;
        playerCaught = false;
        reportedLockedDoors.Clear();
        warnedAboutSave = false;
        filePath = Path.Combine(Application.persistentDataPath, "PlaytestLogs",
            "playtest_" + session.playsession + ".json");
        Save();
        Debug.Log("Playtest JSON log: " + filePath);
    }

    public static void Whistle(Vector3 position) => Record("player_whistled", position);

    public static void DogReached(Vector3 position)
    {
        if (instance == null || instance.session == null || instance.dogReached) return;
        instance.dogReached = true;
        Record("dog_reached", position);
    }

    public static void PlayerCaught(Vector3 position)
    {
        if (instance == null || instance.session == null || instance.playerCaught) return;
        instance.playerCaught = true;
        Record("player_caught", position);
    }

    public static void KeyCollected(Transform key, Vector3 playerPosition, int keysRemaining)
        => Record("key_collected", playerPosition, key, keysRemaining);

    public static void DoorOpened(Transform door, Vector3 playerPosition, int keysRemaining)
        => Record("door_opened", playerPosition, door, keysRemaining);

    public static void DoorUnlocked(Transform door, Vector3 playerPosition, int keysRemaining)
        => Record("door_unlocked", playerPosition, door, keysRemaining);

    public static void LockedDoorBlocked(Transform door, Vector3 playerPosition, int keysRemaining)
    {
        if (instance == null || instance.session == null || door == null) return;
        if (!instance.reportedLockedDoors.Add(ObjectId(door))) return;
        Record("locked_door_blocked", playerPosition, door, keysRemaining);
    }

    // Hierarchy indices distinguish duplicate prefab names without scene edits.
    // Stable for a fixed scene hierarchy; compare IDs within the same build.
    private static string ObjectId(Transform target)
    {
        if (target == null) return "";
        string path = "";
        for (Transform current = target; current != null; current = current.parent)
            path = "/" + current.name + "[" + current.GetSiblingIndex() + "]" + path;
        return target.gameObject.scene.name + path;
    }

    private static void Record(string eventName, Vector3 position, Transform target = null,
        int keysRemaining = -1)
    {
        if (instance == null || instance.session == null) return;
        instance.session.events.Add(new Entry
        {
            sequence = instance.session.events.Count + 1,
            timestampUtc = UtcNow(),
            elapsedSeconds = Math.Round(Time.realtimeSinceStartupAsDouble - instance.startedAt, 3),
            eventName = eventName,
            position = position,
            objectId = ObjectId(target),
            objectName = target != null ? target.name : "",
            objectPosition = target != null ? target.position : Vector3.zero,
            keysRemaining = keysRemaining
        });
        instance.Save();
    }

    private static string UtcNow() => DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

    private void Save()
    {
        if (session == null) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            string temporaryPath = filePath + ".tmp";
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(session, true));
            // Preserve the last complete JSON if writing is interrupted.
            if (File.Exists(filePath)) File.Replace(temporaryPath, filePath, null);
            else File.Move(temporaryPath, filePath);
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException
            || error is NotSupportedException)
        {
            if (!warnedAboutSave)
                Debug.LogWarning("Could not save playtest JSON to " + filePath + ": " + error.Message);
            warnedAboutSave = true;
        }
    }

    private void EndSession()
    {
        if (session == null) return;
        session.endedUtc = UtcNow();
        Save();
        session = null;
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) Save();
    }

    private void OnApplicationQuit() => EndSession();

    private void OnDestroy()
    {
        EndSession();
        if (instance == this) instance = null;
    }
}
