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