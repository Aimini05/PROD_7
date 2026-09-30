using UnityEngine;

namespace AmazingMaze.Player
{
    [RequireComponent(typeof(AudioSource))]
    public class Chest : Interactable
    {
        [Header("Ljud")]
        public AudioClip openClip;
        public AudioClip closeClip;

        [Header("Innehåll (valfritt - lämna tom om kistan är tom)")]
        [Tooltip("Nyckeln är ett eget GameObject. Den göms när kistan är stängd.")]
        public GameObject key;

        [Header("Startläge")]
        public bool startOpen = false;

        public bool IsOpen { get; private set; }

        private AudioSource audioSource;
        private bool keyTaken;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        private void Start()
        {
            IsOpen = startOpen;
            UpdateKeyState();
        }

        // Anropas av spelarens interact-system (F)
        public override bool Interact(GridPlayerController player)
        {
            Toggle();
            return true;
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
    }
}