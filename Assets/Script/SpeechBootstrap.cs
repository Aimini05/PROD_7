using UnityEngine;

public class SpeechBootstrap : MonoBehaviour
{
    private static SpeechBootstrap instance;

    void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        Speech.Init();
    }

    void OnApplicationQuit()
    {
        Speech.Shutdown();
    }
}