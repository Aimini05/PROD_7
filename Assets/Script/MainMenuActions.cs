using UnityEngine;

public class MainMenuActions : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip confirmExitClip; // e.g. "Press Enter to confirm, Escape to cancel"

    private bool awaitingExitConfirm = false;

    public void RequestExit()
    {
        awaitingExitConfirm = true;
        audioSource.Stop();
        if (confirmExitClip != null)
            audioSource.PlayOneShot(confirmExitClip);
    }

    void Update()
    {
        if (!awaitingExitConfirm) return;

        if (Input.GetKeyDown(KeyCode.Return))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            awaitingExitConfirm = false;
            // optionally play a "cancelled" sound here
        }
    }
}