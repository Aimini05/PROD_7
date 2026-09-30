using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using TMPro;

public class HannalieMenu : MonoBehaviour
{
    // Defined inside SettingsMenu to prevent duplicate class errors
    [System.Serializable]
    public class ControlEntry
    {
        public string label;
        public AudioClip clip;
    }

    public GameObject settingsPanel;
    public GameObject mainMenuPanel;
    public MainMenu mainMenu;

    [Header("Audio Mixer")]
    public AudioMixer mainMixer;

    [Header("Option Labels")]
    public TMP_Text masterLabel;
    public TMP_Text musicLabel;
    public TMP_Text sfxLabel;
    public TMP_Text voiceLabel;
    public TMP_Text controlsLabel;
    public TMP_Text returnLabel;
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;

    [Header("Audio")]
    public AudioSource audioSource;
    public bool clipsIgnoreVolume = false;
    public AudioClip settingsOpenedClip;

    [Header("Master Volume Option")]
    public AudioClip masterFocusClip;
    public AudioClip masterActivateClip;

    [Header("Music Volume Option")]
    public AudioClip musicFocusClip;
    public AudioClip musicActivateClip;

    [Header("SFX Volume Option")]
    public AudioClip sfxFocusClip;
    public AudioClip sfxActivateClip;

    [Header("Voice Volume Option")]
    public AudioClip voiceFocusClip;
    public AudioClip voiceActivateClip;

    [Tooltip("11 klipp: index 0 = 0%, 1 = 10%, 2 = 20% ... 10 = 100%")]
    public AudioClip[] volumeStepClips = new AudioClip[11];

    [Header("Controls Option")]
    public AudioClip controlsFocusClip;
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

    private const int OptionMaster = 0;
    private const int OptionMusic = 1;
    private const int OptionSFX = 2;
    private const int OptionVoice = 3;
    private const int OptionControls = 4;
    private const int OptionReturn = 5;
    private const int OptionCount = 6;

    private const int VolumeStep = 10;

    private const string MasterKey = "MasterVolume";
    private const string MusicKey = "MusicVolume";
    private const string SFXKey = "SFXVolume";
    private const string VoiceKey = "VoiceVolume";

    private int masterVol = 100;
    private int musicVol = 100;
    private int sfxVol = 100;
    private int voiceVol = 100;

    private int selected = OptionMaster;
    private int controlIndex = 0;
    private bool adjustingVolume = false;
    private bool inControls = false;
    private bool isOpen = false;
    private float nextInputTime = 0f;

    private void Start()
    {
        if (mainMenu == null) mainMenu = GetComponent<MainMenu>();
        if (mainMenu == null) Debug.LogWarning("SettingsMenu: hittar ingen MainMenu-komponent.");

        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        masterVol = PlayerPrefs.GetInt(MasterKey, 100);
        musicVol = PlayerPrefs.GetInt(MusicKey, 100);
        sfxVol = PlayerPrefs.GetInt(SFXKey, 100);
        voiceVol = PlayerPrefs.GetInt(VoiceKey, 100);

        ApplyMixerVolume(MasterKey, masterVol);
        ApplyMixerVolume(MusicKey, musicVol);
        ApplyMixerVolume(SFXKey, sfxVol);
        ApplyMixerVolume(VoiceKey, voiceVol);

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

    public void Open()
    {
        if (mainMenu != null) mainMenu.SetMenuActive(false);
        settingsPanel.SetActive(true);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);

        isOpen = true;
        adjustingVolume = false;
        inControls = false;
        selected = OptionMaster;
        StartCooldown();
        PlayClip(settingsOpenedClip);
        RefreshLabels();
    }

    public void Close()
    {
        isOpen = false;
        adjustingVolume = false;
        inControls = false;
        settingsPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);

        if (mainMenu != null) StartCoroutine(ReactivateMainMenu());
    }

    private System.Collections.IEnumerator ReactivateMainMenu()
    {
        yield return null;
        mainMenu.SetMenuActive(true);
    }

    public void ReturnToMenu()
    {
        PlayClip(returnActivateClip);
        Close();
    }

    private void MoveSelection(int dir)
    {
        selected = (selected + dir + OptionCount) % OptionCount;

        switch (selected)
        {
            case OptionMaster: PlayClip(masterFocusClip); break;
            case OptionMusic: PlayClip(musicFocusClip); break;
            case OptionSFX: PlayClip(sfxFocusClip); break;
            case OptionVoice: PlayClip(voiceFocusClip); break;
            case OptionControls: PlayClip(controlsFocusClip); break;
            case OptionReturn: PlayClip(returnFocusClip); break;
        }
        RefreshLabels();
    }

    private void ActivateSelected()
    {
        switch (selected)
        {
            case OptionMaster:
                adjustingVolume = true; PlayClip(masterActivateClip); RefreshLabels(); break;
            case OptionMusic:
                adjustingVolume = true; PlayClip(musicActivateClip); RefreshLabels(); break;
            case OptionSFX:
                adjustingVolume = true; PlayClip(sfxActivateClip); RefreshLabels(); break;
            case OptionVoice:
                adjustingVolume = true; PlayClip(voiceActivateClip); RefreshLabels(); break;

            case OptionControls:
                if (controls == null || controls.Count == 0) return;
                inControls = true;
                controlIndex = 0;
                PlayControl();
                RefreshLabels();
                break;

            case OptionReturn:
                PlayClip(returnActivateClip);
                Close();
                break;
        }
    }

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
        PlayClip(controlsFocusClip);
        RefreshLabels();
    }

    private void ChangeVolume(int delta)
    {
        int currentVol = 0;
        string key = "";

        if (selected == OptionMaster) { currentVol = masterVol; key = MasterKey; }
        else if (selected == OptionMusic) { currentVol = musicVol; key = MusicKey; }
        else if (selected == OptionSFX) { currentVol = sfxVol; key = SFXKey; }
        else if (selected == OptionVoice) { currentVol = voiceVol; key = VoiceKey; }

        int newVolume = Mathf.Clamp(currentVol + delta, 0, 100);
        if (newVolume == currentVol) return;

        if (selected == OptionMaster) masterVol = newVolume;
        else if (selected == OptionMusic) musicVol = newVolume;
        else if (selected == OptionSFX) sfxVol = newVolume;
        else if (selected == OptionVoice) voiceVol = newVolume;

        ApplyMixerVolume(key, newVolume);
        PlayerPrefs.SetInt(key, newVolume);
        PlayerPrefs.Save();

        int index = newVolume / VolumeStep;
        if (volumeStepClips != null && index < volumeStepClips.Length)
            PlayClip(volumeStepClips[index]);

        RefreshLabels();
    }

    private void ApplyMixerVolume(string parameterName, int volumePercentage)
    {
        if (mainMixer == null) return;
        float db = volumePercentage > 0 ? Mathf.Log10(volumePercentage / 100f) * 20f : -80f;
        mainMixer.SetFloat(parameterName, db);
    }

    private void RefreshLabels()
    {
        UpdateLabel(masterLabel, "Master", masterVol, OptionMaster);
        UpdateLabel(musicLabel, "Music", musicVol, OptionMusic);
        UpdateLabel(sfxLabel, "SFX", sfxVol, OptionSFX);
        UpdateLabel(voiceLabel, "Voice", voiceVol, OptionVoice);

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

    private void UpdateLabel(TMP_Text label, string prefix, int volValue, int optionIndex)
    {
        if (label == null) return;
        string text = prefix + " Volume: " + volValue + "%";
        label.text = (adjustingVolume && selected == optionIndex) ? "< " + text + " >" : text;
        label.color = selected == optionIndex ? highlightColor : normalColor;
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