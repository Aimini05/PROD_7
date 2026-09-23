using UnityEngine;

public class SettingsMenu : MonoBehaviour
{
    public GameObject settingsPanel; // the Settings UI Canvas/Panel, assign in Inspector
    public GameObject mainMenuPanel; // your main menu Canvas/Panel, so we can hide it

    public void Open()
    {
        settingsPanel.SetActive(true);
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
    }

    public void Close()
    {
        settingsPanel.SetActive(false);
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }
}