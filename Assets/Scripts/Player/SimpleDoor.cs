using System;
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
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip lockedSound;

        public bool IsOpen { get; private set; }
        public bool IsLocked => isLocked;
        private float nextLockedSoundTime;
        // A sound script can subscribe here to announce that the passage is open.
        public event Action Opened;
        public event Action LockedAttempted;

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
            Open();
            return true;
        }

        public void Open()
        {
            if (IsOpen || isLocked) return;
            IsOpen = true;
            // Keep this GameObject active so the opening sound can finish playing.
            PlayInteractionSound(openSound);
            // Prototype opening: remove the panel and its obstruction.
            foreach (var panel in GetComponentsInChildren<Renderer>()) panel.enabled = false;
            foreach (var obstacle in GetComponentsInChildren<Collider>()) obstacle.enabled = false;
            Physics.SyncTransforms();
            Opened?.Invoke();
        }

        // Future key/inventory code can unlock the door without replacing its sound logic.
        public void SetLocked(bool locked)
        {
            isLocked = locked;
        }
    }
}
