using UnityEngine;
using System.Collections.Generic;
using TMPro;

[RequireComponent(typeof(AudioSource))]
public class MainMenu : MonoBehaviour
{
    public List<MenuOption> options = new List<MenuOption>();
    public List<TextMeshProUGUI> optionLabels; // assign matching UI text objects, same order as options
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;

    public AudioClip menuOpenedClip;
    public float inputCooldown = 0.15f;

    private AudioSource audioSource;
    private int currentIndex = 0;
    private float lastInputTime = -999f;
    private bool menuActive = true;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        if (menuOpenedClip != null)
            audioSource.PlayOneShot(menuOpenedClip);

        RefreshVisuals();
        PlayFocusClip();
    }

    void Update()
    {
        
            if (!menuActive) return;
            if (Time.time - lastInputTime < inputCooldown) return;

            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                Move(1);
                lastInputTime = Time.time;
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                Move(-1);
                lastInputTime = Time.time;
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                Activate();
                lastInputTime = Time.time;
            }
        
    }

    void Move(int direction)
    {
        currentIndex = (currentIndex + direction + options.Count) % options.Count;
        RefreshVisuals();
        PlayFocusClip();
    }

    void RefreshVisuals()
    {
        for (int i = 0; i < optionLabels.Count; i++)
        {
            optionLabels[i].color = (i == currentIndex) ? highlightColor : normalColor;
        }
    }

    void PlayFocusClip()
    {
        audioSource.Stop();
        var clip = options[currentIndex].focusClip;
        if (clip != null)
            audioSource.PlayOneShot(clip);
    }

    void Activate()
    {
        var chosen = options[currentIndex];
        audioSource.Stop();
        if (chosen.activateClip != null)
            audioSource.PlayOneShot(chosen.activateClip);

        chosen.onSelect.Invoke();
    }

    public void SetMenuActive(bool active)
    {
        menuActive = active;
    }
}