using UnityEngine;

namespace AmazingMaze.Player
{
    // Base class for objects the player can use with F.
    // Future chest/key scripts can inherit this and implement their own interaction.
    public abstract class Interactable : MonoBehaviour
    {
        [Header("Interaction audio")]
        [Tooltip("Optional: assign a configured Audio Source. If empty, one is created when a sound is first played.")]
        [SerializeField] private AudioSource interactionAudioSource;
        [SerializeField, Range(0f, 1f)] private float interactionVolume = 1f;

        // Shared playback helper for doors and future interaction types.
        // An unassigned clip is allowed, so interaction works before sounds are added.
        protected void PlayInteractionSound(AudioClip clip)
        {
            if (clip == null) return;
            if (interactionAudioSource == null)
            {
                interactionAudioSource = gameObject.AddComponent<AudioSource>();
                interactionAudioSource.playOnAwake = false;
                interactionAudioSource.loop = false;
                // A positional sound lets the player hear where the object is.
                interactionAudioSource.spatialBlend = 1f;
                interactionAudioSource.minDistance = 1f;
                interactionAudioSource.maxDistance = 15f;
                interactionAudioSource.rolloffMode = AudioRolloffMode.Linear;
            }
            interactionAudioSource.PlayOneShot(clip, interactionVolume);
        }

        // Return true only when an interaction actually happens.
        // The player is supplied for future inventory or permission checks.
        public abstract bool Interact(GridPlayerController player);
    }
}
