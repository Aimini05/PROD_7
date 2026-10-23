using System;
using System.Collections;
using UnityEngine;

namespace AmazingMaze.Player
{
    [DisallowMultipleComponent]
    public sealed class SimpleDoor : Interactable
    {
        [Header("Door")]
        [Tooltip("Unlocked by default. Enable to make the door require a key (PlayerInventory).")]
        [SerializeField] private bool isLocked;
        [Tooltip("Av = nyckeln förbrukas inte, en nyckel öppnar alla dörrar.")]
        [SerializeField] private bool consumeKey = true;

        [Header("Door sounds")]
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip closeSound;
        [SerializeField] private AudioClip lockedSound;
        [SerializeField] private AudioClip unlockSound;
        [SerializeField, Range(0f, 2f)] private float interactionSoundVolume = 1f;

        [Header("Door proximity sound")]
        [SerializeField] private AudioSource proximityAudioSource;
        [SerializeField] private AudioClip proximityCreak;
        [SerializeField, Range(0f, 1f)] private float proximityVolume = 0.3f;
        [SerializeField, Min(0.1f)] private float proximityMinDistance = 0.5f;
        [SerializeField, Min(0.1f)] private float proximityMaxDistance = 4f;

        [Header("Passage")]
        [SerializeField] private bool closeAfterPassage = true;

        public bool IsOpen { get; private set; }
        public bool IsLocked => isLocked;
        private float nextLockedSoundTime;
        private Renderer[] panels;
        private Collider[] obstacles;
        private bool[] panelEnabledStates;
        private bool[] obstacleEnabledStates;
        private Bounds[] cachedObstacleBounds; // Sparar dörrens gränser
        private Coroutine closeRoutine;
        private AudioSource interactionAudioSource;

        public event Action Opened;
        public event Action Closed;
        public event Action LockedAttempted;

        private void Awake()
        {
            CacheDoorParts();

            interactionAudioSource = GetComponent<AudioSource>();
            if (interactionAudioSource == null || interactionAudioSource == proximityAudioSource)
            {
                AudioSource[] sources = GetComponents<AudioSource>();
                foreach (var src in sources)
                {
                    if (src != proximityAudioSource)
                    {
                        interactionAudioSource = src;
                        break;
                    }
                }
                if (interactionAudioSource == null || interactionAudioSource == proximityAudioSource)
                {
                    interactionAudioSource = gameObject.AddComponent<AudioSource>();
                }
            }
            interactionAudioSource.playOnAwake = false;
            interactionAudioSource.spatialBlend = 1f;

            if (proximityAudioSource != null && proximityCreak != null)
            {
                proximityAudioSource.clip = proximityCreak;
                proximityAudioSource.loop = true;
                proximityAudioSource.playOnAwake = false;

                proximityAudioSource.spatialBlend = 1f;
                proximityAudioSource.dopplerLevel = 0f;

                proximityAudioSource.rolloffMode = AudioRolloffMode.Linear;
                proximityAudioSource.minDistance = proximityMinDistance;
                proximityAudioSource.maxDistance = proximityMaxDistance;

                proximityAudioSource.volume = proximityVolume;
                proximityAudioSource.Play();
            }
        }

        private void OnDisable()
        {
            if (closeRoutine != null)
            {
                StopCoroutine(closeRoutine);
                closeRoutine = null;
            }

            if (IsOpen)
            {
                SetPassageOpen(false);
                IsOpen = false;
                Physics.SyncTransforms();
            }
        }

        public override bool Interact(GridPlayerController player)
        {
            if (IsOpen) return false;

            if (isLocked)
            {
                PlayerInventory inventory = null;
                if (player != null)
                {
                    inventory = player.GetComponentInParent<PlayerInventory>();
                    if (inventory == null) inventory = player.GetComponentInChildren<PlayerInventory>();
                }

                if (inventory != null && inventory.UseKey(consumeKey))
                {
                    isLocked = false;
                    PlayCustomSound(unlockSound);
                }
                else
                {
                    if (Time.unscaledTime >= nextLockedSoundTime)
                    {
                        PlayCustomSound(lockedSound);
                        nextLockedSoundTime = Time.unscaledTime + 0.25f;
                    }
                    LockedAttempted?.Invoke();
                    return false;
                }
            }

            Open(player);
            return true;
        }

        public void Open()
        {
            Open(null);
        }

        public void Open(GridPlayerController player)
        {
            if (IsOpen || isLocked) return;

            CacheDoorParts();
            IsOpen = true;
            PlayCustomSound(openSound);
            SetPassageOpen(true);
            Physics.SyncTransforms();
            Opened?.Invoke();

            if (closeAfterPassage)
            {
                if (player == null)
                {
                    player = FindFirstObjectByType<GridPlayerController>();
                }
                closeRoutine = StartCoroutine(CloseWhenPlayerHasPassed(player));
            }
        }

        public void Close()
        {
            if (!IsOpen) return;

            if (closeRoutine != null)
            {
                StopCoroutine(closeRoutine);
                closeRoutine = null;
            }

            PlayCustomSound(closeSound);
            SetPassageOpen(false);
            IsOpen = false;
            Physics.SyncTransforms();
            Closed?.Invoke();
        }

        private IEnumerator CloseWhenPlayerHasPassed(GridPlayerController player)
        {
            float clipDuration = openSound != null ? openSound.length : 1f;
            yield return new WaitForSeconds(clipDuration);

            if (player != null)
            {
                // Spara dörrens position som referenspunkt
                Vector3 doorPosition = transform.position;

                // Mät avståndet till spelaren när dörren öppnades (borde vara ca 1 tile bort)
                float initialDistance = Vector3.Distance(player.transform.position, doorPosition);

                float timeWaited = 0f;
                float maxWaitTime = 6f;
                bool playerMovedThrough = false;

                // 1. Vänta tills spelaren antingen går närmare (kliver in i dörren) eller flyttar sig bort
                while (timeWaited < maxWaitTime)
                {
                    float currentDistance = Vector3.Distance(player.transform.position, doorPosition);

                    // Om spelaren har gått igenom (avståndet ökar eller de har passerat dörren)
                    if (currentDistance > initialDistance + 0.5f || IsPlayerInsideCachedBounds(player))
                    {
                        playerMovedThrough = true;
                        break;
                    }

                    timeWaited += Time.deltaTime;
                    yield return null;
                }

                // 2. Om spelaren gick in i/igenom dörren, vänta tills de är minst 1.5 tiles bort
                if (playerMovedThrough)
                {
                    while (Vector3.Distance(player.transform.position, doorPosition) < player.TileSize * 1.2f)
                    {
                        yield return null;
                    }
                }
            }

            // 3. Kort paus så spelaren hinner ta ett steg till innan dörren slår igen
            yield return new WaitForSeconds(0.3f);

            closeRoutine = null;
            PlayCustomSound(closeSound);
            SetPassageOpen(false);
            IsOpen = false;
            Physics.SyncTransforms();
            Closed?.Invoke();
        }

        private bool IsPlayerInsideCachedBounds(GridPlayerController player)
        {
            Collider playerCollider = player.GetComponent<Collider>();
            if (playerCollider == null) return false;

            Bounds playerBounds = playerCollider.bounds;
            for (int i = 0; i < cachedObstacleBounds.Length; i++)
            {
                if (cachedObstacleBounds[i].Intersects(playerBounds))
                {
                    return true;
                }
            }

            return false;
        }

        private void CacheDoorParts()
        {
            if (panels != null && obstacles != null && cachedObstacleBounds != null) return;

            panels = GetComponentsInChildren<Renderer>(true);
            obstacles = GetComponentsInChildren<Collider>(true);
            panelEnabledStates = new bool[panels.Length];
            obstacleEnabledStates = new bool[obstacles.Length];
            cachedObstacleBounds = new Bounds[obstacles.Length];

            for (int i = 0; i < panels.Length; i++) panelEnabledStates[i] = panels[i].enabled;
            for (int i = 0; i < obstacles.Length; i++)
            {
                obstacleEnabledStates[i] = obstacles[i].enabled;
                cachedObstacleBounds[i] = obstacles[i].bounds; // Spara storleken medan dörren är stängd
            }
        }

        private void SetPassageOpen(bool open)
        {
            for (int i = 0; i < panels.Length; i++)
            {
                if (panels[i] != null) panels[i].enabled = open ? false : panelEnabledStates[i];
            }

            for (int i = 0; i < obstacles.Length; i++)
            {
                if (obstacles[i] != null) obstacles[i].enabled = open ? false : obstacleEnabledStates[i];
            }
        }

        private void PlayCustomSound(AudioClip clip)
        {
            if (clip == null || interactionAudioSource == null) return;
            interactionAudioSource.PlayOneShot(clip, interactionSoundVolume);
        }

        public void SetLocked(bool locked)
        {
            isLocked = locked;
        }
    }
}