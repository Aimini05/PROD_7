using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Chest : MonoBehaviour
{
    [Header("Ljud")]
    public AudioClip openClip;
    public AudioClip closeClip;

    [Header("Interaktion")]
    public KeyCode interactKey = KeyCode.F;
    public string playerTag = "Player";

    [Header("Innehåll (valfritt - lämna tom om kistan är tom)")]
    [Tooltip("Nyckeln är ett eget GameObject. Den göms när kistan är stängd.")]
    public GameObject key;

    [Header("Lock (valfritt)")]
    public Transform lid;
    public Vector3 openRotation = new Vector3(-100f, 0f, 0f);
    public float lidSpeed = 4f;

    [Header("Startläge")]
    public bool startOpen = false;

    public bool IsOpen { get; private set; }

    private AudioSource audioSource;
    private bool playerInRange;
    private bool keyTaken;
    private Quaternion closedRot;
    private Quaternion openRot;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        if (lid != null)
        {
            closedRot = lid.localRotation;
            openRot = closedRot * Quaternion.Euler(openRotation);
        }
    }

    private void Start()
    {
        IsOpen = startOpen;
        if (lid != null)
            lid.localRotation = IsOpen ? openRot : closedRot;

        UpdateKeyState();
    }

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(interactKey))
            Toggle();

        // Mjuk rörelse på locket
        if (lid != null)
        {
            Quaternion target = IsOpen ? openRot : closedRot;
            lid.localRotation = Quaternion.Slerp(lid.localRotation, target, Time.deltaTime * lidSpeed);
        }
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        Play(openClip);
        UpdateKeyState();
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        Play(closeClip);
        UpdateKeyState();
    }

    // Anropas av KeyPickup när nyckeln plockats upp
    public void OnKeyTaken()
    {
        keyTaken = true;
    }

    // Nyckeln syns/kan plockas bara när kistan är öppen och nyckeln inte redan tagits
    private void UpdateKeyState()
    {
        if (key == null || keyTaken) return;
        key.SetActive(IsOpen);
    }

    private void Play(AudioClip clip)
    {
        if (clip != null)
            audioSource.PlayOneShot(clip);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
            playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
            playerInRange = false;
    }
}