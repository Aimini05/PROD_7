using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioCutsceneManager : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip cutsceneClip;

    [Header("Controls Audio")]
    [Tooltip("Ljudklippet med kontroller som spelas efter cutscenen.")]
    public AudioClip controlsClip;

    [Header("Scene Transition")]
    [Tooltip("Type the exact name of the scene you want to load next.")]
    public string nextSceneName = "GameScene";

    [Header("Accessibility / Controls")]
    public bool allowSkip = true;

    private bool isLoadingNextScene = false;
    private bool isPlayingControls = false;

    private void Start()
    {
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (cutsceneClip != null)
        {
            audioSource.clip = cutsceneClip;
            audioSource.Play();
        }

        StartCoroutine(PlayCutsceneSequence());
    }

    private void Update()
    {
        if (allowSkip && !isLoadingNextScene)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape))
            {
                if (!isPlayingControls && controlsClip != null)
                {
                    StopAllCoroutines();
                    StartCoroutine(PlayControlsSequence());
                }
                else
                {
                    LoadNextScene();
                }
            }
        }
    }

    private IEnumerator PlayCutsceneSequence()
    {
        // Vänta tills cutscenen har spelat klart
        yield return new WaitWhile(() => audioSource.isPlaying);

        // Om det finns ett kontroll-ljud, starta det
        if (controlsClip != null)
        {
            yield return StartCoroutine(PlayControlsSequence());
        }
        else
        {
            LoadNextScene();
        }
    }

    private IEnumerator PlayControlsSequence()
    {
        isPlayingControls = true;
        audioSource.Stop();
        audioSource.clip = controlsClip;
        audioSource.Play();

        // Vänta tills kontroll-ljudet har spelat klart
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