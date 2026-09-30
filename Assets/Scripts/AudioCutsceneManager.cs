using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioCutsceneManager : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip cutsceneClip;

    [Header("Scene Transition")]
    [Tooltip("Type the exact name of the scene you want to load next.")]
    public string nextSceneName = "GameScene";

    [Header("Accessibility / Controls")]
    public bool allowSkip = true;

    private bool isLoadingNextScene = false;

    private void Start()
    {
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (cutsceneClip != null)
        {
            audioSource.clip = cutsceneClip;
            audioSource.Play();
        }

        StartCoroutine(WaitForAudioToEnd());
    }

    private void Update()
    {
        // Allow player to skip the audio cutscene by pressing Space, Enter, or Escape
        if (allowSkip && !isLoadingNextScene)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape))
            {
                LoadNextScene();
            }
        }
    }

    private IEnumerator WaitForAudioToEnd()
    {
        // Wait until the audio finishes playing
        yield return new WaitWhile(() => audioSource.isPlaying);

        LoadNextScene();
    }

    private void LoadNextScene()
    {
        if (isLoadingNextScene) return;
        isLoadingNextScene = true;

        SceneManager.LoadScene(nextSceneName);
    }
}