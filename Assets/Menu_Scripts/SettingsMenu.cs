using UnityEngine;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class ControlEntry
{
    public string label;   // t.ex. "V: Whistle" (visas i texten)
    public AudioClip clip; // röstklippet som läser upp kontrollen
}

public class SettingsMenu : MonoBehaviour
{
    public GameObject settingsPanel; // Canvas_Settings, assign in Inspector
    public GameObject mainMenuPanel; // Canvas_MainMenu, so we can hide it
    public MainMenu mainMenu;        // MainMenu-komponenten (Menu.cs), så vi kan pausa den medan settings är öppen

    [Header("Option Labels")]
    public TMP_Text volumeLabel;     // Option 0: Volume
    public TMP_Text controlsLabel;   // Option 1: Controls
    public TMP_Text returnLabel;     // Option 2: Return to Menu
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;

    [Header("Audio")]
    public AudioSource audioSource;      // läggs till automatiskt om den saknas
    public bool clipsIgnoreVolume = false; // true = klippen hörs även när volymen är 0
    public AudioClip settingsOpenedClip;

    [Header("Volume Option")]
    public AudioClip volumeFocusClip;    // spelas när "Volume" markeras
    public AudioClip volumeActivateClip; // spelas när man väljer volymen (Enter)
    [Tooltip("11 klipp: index 0 = 0%, 1 = 10%, 2 = 20% ... 10 = 100%")]
    public AudioClip[] volumeStepClips = new AudioClip[11];

    [Header("Controls Option")]
    public AudioClip controlsFocusClip;  // spelas när "Controls" markeras, och när man lämnar listan
    [Tooltip("Varje kontroll får ett röstklipp. Ordningen här = ordningen man går igenom dem.")]
    public List<ControlEntry> controls = new List<ControlEntry>()
    {
        new ControlEntry { label = "V: Whistle" },
        new ControlEntry { label = "F: Interact" },
        new ControlEntry { label = "Space: Jump" },
        new ControlEntry { label = "A or Left Arrow: Turn Left" },
    };

    [Header("Return To Menu Option")]
    public AudioClip returnFocusClip;
    public AudioClip returnActivateClip;

    [Header("Input")]
    public float inputCooldown = 0.15f;

    private const int OptionVolume = 0;
    private const int OptionControls = 1;
    private const int OptionReturn = 2;
    private const int OptionCount = 3;

    private const int VolumeStep = 10;
    private const string VolumeKey = "MasterVolume";

    // Körs automatiskt när spelet startar, i vilken scen som helst,
    // så den sparade volymen alltid gäller (även om du startar direkt i spelscenen)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplySavedVolume()
    {
        AudioListener.volume = PlayerPrefs.GetInt(VolumeKey, 100) / 100f;
    }

    private int volume = 100;       // 0-100
    private int selected = OptionVolume;
    private int controlIndex = 0;
    private bool adjustingVolume = false;
    private bool inControls = false;
    private bool isOpen = false;
    private float nextInputTime = 0f;

    private void Start()
    {
        // Hitta huvudmenyn automatiskt om fältet inte är ifyllt (den ligger på samma objekt)
        if (mainMenu == null)
            mainMenu = GetComponent<MainMenu>();
        if (mainMenu == null)
            Debug.LogWarning("SettingsMenu: hittar ingen MainMenu-komponent. Dra in den i fältet 'Main Menu'.");

        // Egen AudioSource, så settings-ljuden inte delar källa med huvudmenyn
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        volume = PlayerPrefs.GetInt(VolumeKey, 100);
        AudioListener.volume = volume / 100f;
        RefreshLabels();
    }

    private void Update()
    {
        if (!isOpen) return;
        if (Time.unscaledTime < nextInputTime) return;

        bool up = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);
        bool down = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);
        bool confirm = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space);
        bool back = Input.GetKeyDown(KeyCode.Escape);

        if (adjustingVolume)
        {
            if (up) { ChangeVolume(+VolumeStep); StartCooldown(); }
            else if (down) { ChangeVolume(-VolumeStep); StartCooldown(); }
            else if (confirm || back) { adjustingVolume = false; RefreshLabels(); StartCooldown(); }
            return;
        }

        if (inControls)
        {
            if (up) { MoveControl(-1); StartCooldown(); }
            else if (down) { MoveControl(+1); StartCooldown(); }
            else if (confirm || back) { ExitControls(); StartCooldown(); }
            return;
        }

        if (up) { MoveSelection(-1); StartCooldown(); }
        else if (down) { MoveSelection(+1); StartCooldown(); }
        else if (confirm) { ActivateSelected(); StartCooldown(); }
        else if (back) { PlayClip(returnActivateClip); Close(); }
    }

    // ---------- Öppna / stäng ----------

    public void Open()
    {
        if (mainMenu != null)
            mainMenu.SetMenuActive(false); // huvudmenyn slutar lyssna på tangenter och spela ljud

        settingsPanel.SetActive(true);
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        isOpen = true;
        adjustingVolume = false;
        inControls = false;
        selected = OptionVolume;
        StartCooldown(); // hindrar att samma Enter-tryck från huvudmenyn triggar något här
        PlayClip(settingsOpenedClip);
        RefreshLabels();
    }

    public void Close()
    {
        isOpen = false;
        adjustingVolume = false;
        inControls = false;
        settingsPanel.SetActive(false);
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        // Slå på huvudmenyn igen först nästa frame, annars kan samma Enter-tryck öppna Settings på nytt
        if (mainMenu != null)
            StartCoroutine(ReactivateMainMenu());
    }

    private System.Collections.IEnumerator ReactivateMainMenu()
    {
        yield return null;
        mainMenu.SetMenuActive(true);
    }

    // Kan kopplas till en knapp om du även vill kunna klicka med musen
    public void ReturnToMenu()
    {
        PlayClip(returnActivateClip);
        Close();
    }

    // ---------- Meny-logik ----------

    private void MoveSelection(int dir)
    {
        selected = (selected + dir + OptionCount) % OptionCount;

        switch (selected)
        {
            case OptionVolume: PlayClip(volumeFocusClip); break;
            case OptionControls: PlayClip(controlsFocusClip); break;
            case OptionReturn: PlayClip(returnFocusClip); break;
        }
        RefreshLabels();
    }

    private void ActivateSelected()
    {
        switch (selected)
        {
            case OptionVolume:
                adjustingVolume = true;
                PlayClip(volumeActivateClip);
                RefreshLabels();
                break;

            case OptionControls:
                if (controls == null || controls.Count == 0) return;
                inControls = true;
                controlIndex = 0;
                PlayControl(); // läser upp första kontrollen direkt
                RefreshLabels();
                break;

            case OptionReturn:
                PlayClip(returnActivateClip);
                Close();
                break;
        }
    }

    // ---------- Controls ----------

    private void MoveControl(int dir)
    {
        controlIndex = (controlIndex + dir + controls.Count) % controls.Count;
        PlayControl();
        RefreshLabels();
    }

    private void PlayControl()
    {
        PlayClip(controls[controlIndex].clip);
    }

    private void ExitControls()
    {
        inControls = false;
        PlayClip(controlsFocusClip); // säger "Controls" igen så man hör att man är tillbaka i listan
        RefreshLabels();
    }

    // ---------- Volym ----------

    private void ChangeVolume(int delta)
    {
        int newVolume = Mathf.Clamp(volume + delta, 0, 100);
        if (newVolume == volume) return; // redan på min/max, inget klipp

        volume = newVolume;
        AudioListener.volume = volume / 100f;
        PlayerPrefs.SetInt(VolumeKey, volume);
        PlayerPrefs.Save();

        int index = volume / VolumeStep;
        if (volumeStepClips != null && index < volumeStepClips.Length)
            PlayClip(volumeStepClips[index]);

        RefreshLabels();
    }

    // ---------- Hjälpfunktioner ----------

    private void RefreshLabels()
    {
        if (volumeLabel != null)
        {
            string text = "Volume: " + volume + "%";
            volumeLabel.text = adjustingVolume ? "< " + text + " >" : text;
            volumeLabel.color = selected == OptionVolume ? highlightColor : normalColor;
        }

        if (controlsLabel != null)
        {
            string text = "Controls";
            if (inControls && controls != null && controls.Count > 0)
                text = "< " + controls[controlIndex].label + " >";
            controlsLabel.text = text;
            controlsLabel.color = selected == OptionControls ? highlightColor : normalColor;
        }

        if (returnLabel != null)
        {
            returnLabel.color = selected == OptionReturn ? highlightColor : normalColor;
        }
    }

    private void StartCooldown()
    {
        nextInputTime = Time.unscaledTime + inputCooldown;
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.ignoreListenerVolume = clipsIgnoreVolume;
        audioSource.clip = clip;
        audioSource.Play();
    }
}