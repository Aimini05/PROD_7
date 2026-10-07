using System;
using System.Collections;
using UnityEngine;

namespace AmazingMaze.Player
{
    [DisallowMultipleComponent]
    public sealed class SimpleDoor : Interactable
    {
       
        [Header("Door")]
        [Tooltip("Unlocked by default. Enable to test locked-door feedback; no key system is implemented yet.")]
        [SerializeField] private bool isLocked;

        [Header("Door sounds")]
        [Tooltip("A single clip containing the opening sound followed by the closing sound.")]
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip lockedSound;


        [Header("Door proximity sound")]
        [SerializeField] private AudioSource proximityAudioSource;
        [SerializeField] private AudioClip proximityCreak;
        [SerializeField, Range(0f, 1f)] private float proximityVolume = 0.3f;
        [SerializeField, Min(0.1f)] private float proximityMinDistance = 0.5f;
        [SerializeField, Min(0.1f)] private float proximityMaxDistance = 4f;

        [Header("Passage")]
        [Tooltip("Minimum time the doorway stays clear. The full open/close clip is always allowed to finish first.")]
        [SerializeField, Min(0.1f)] private float minimumPassableDuration = 1.5f;
        [SerializeField] private bool closeAfterPassage = true;

        public bool IsOpen { get; private set; }
        public bool IsLocked => isLocked;
        private float nextLockedSoundTime;
        private Renderer[] panels;
        private Collider[] obstacles;
        private bool[] panelEnabledStates;
        private bool[] obstacleEnabledStates;
        private Coroutine closeRoutine;
        // A sound script can subscribe here to announce that the passage is open.
        public event Action Opened;
        public event Action Closed;
        public event Action LockedAttempted;

        private void Awake()
        {
            CacheDoorParts();
            if (proximityAudioSource != null && proximityCreak != null)
            {
                proximityAudioSource.clip = proximityCreak;
                proximityAudioSource.loop = true;
                proximityAudioSource.playOnAwake = false;

                // Fullt 3D-ljud
                proximityAudioSource.spatialBlend = 1f;
                proximityAudioSource.dopplerLevel = 0f;

                // Smooth och tydlig avståndsskillnad
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

            // Do not leave the scene with invisible/non-solid doors after a script reload
            // or if a door component is disabled while its passage is open.
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
                // Avoid stacking many copies of the locked sound when F is pressed rapidly.
                if (Time.unscaledTime >= nextLockedSoundTime)
                {
                    PlayInteractionSound(lockedSound);
                    nextLockedSoundTime = Time.unscaledTime + 0.25f;
                }
                LockedAttempted?.Invoke();
                return false;
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
            // Keep this GameObject active so the combined open/close sound can finish.
            PlayInteractionSound(openSound);
            SetPassageOpen(true);
            Physics.SyncTransforms();
            Opened?.Invoke();

            if (closeAfterPassage)
            {
                closeRoutine = StartCoroutine(CloseWhenSafe(player));
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

            SetPassageOpen(false);
            IsOpen = false;
            Physics.SyncTransforms();
            Closed?.Invoke();
        }

        private IEnumerator CloseWhenSafe(GridPlayerController player)
        {
            // The supplied clip already contains the closing sound, so keep the physical
            // passage open through the whole clip (and for at least the configured window).
            float clipDuration = openSound != null ? openSound.length : 0f;
            yield return new WaitForSeconds(Mathf.Max(minimumPassableDuration, clipDuration));

            // Never recreate a solid door around the player. This also makes the timing
            // forgiving for players who navigate primarily by sound.
            while (player != null && IsPlayerInsideDoorway(player))
            {
                yield return null;
            }

            closeRoutine = null;
            SetPassageOpen(false);
            IsOpen = false;
            Physics.SyncTransforms();
            Closed?.Invoke();
        }

        private bool IsPlayerInsideDoorway(GridPlayerController player)
        {
            Collider playerCollider = player.GetComponent<Collider>();
            if (playerCollider == null) return false;

            Bounds playerBounds = playerCollider.bounds;
            for (int i = 0; i < obstacles.Length; i++)
            {
                if (obstacles[i] != null && obstacles[i].bounds.Intersects(playerBounds))
                {
                    return true;
                }
            }

            return false;
        }

        private void CacheDoorParts()
        {
            if (panels != null && obstacles != null) return;

            panels = GetComponentsInChildren<Renderer>(true);
            obstacles = GetComponentsInChildren<Collider>(true);
            panelEnabledStates = new bool[panels.Length];
            obstacleEnabledStates = new bool[obstacles.Length];

            for (int i = 0; i < panels.Length; i++) panelEnabledStates[i] = panels[i].enabled;
            for (int i = 0; i < obstacles.Length; i++) obstacleEnabledStates[i] = obstacles[i].enabled;
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

        // Future key/inventory code can unlock the door without replacing its sound logic.
        public void SetLocked(bool locked)
        {
            isLocked = locked;
        }
    }
}
