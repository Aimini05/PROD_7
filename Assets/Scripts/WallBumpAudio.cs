using UnityEngine;
namespace AmazingMaze.Player
{
    [RequireComponent(typeof(GridPlayerController))]
    public class WallBumpAudio : MonoBehaviour
    {
        [Header("Wall Bump Audio")]
        [Tooltip("Ljudkällan som ska spela upp väggljudet. Om tom skapas en automatiskt.")]
        [SerializeField] private AudioSource audioSource;
        [Tooltip("Ljudklippet för när spelaren slår i en vägg.")]
        [SerializeField] private AudioClip wallBumpSound;
        [SerializeField, Range(0f, 1f)] private float volume = 0.75f;

        private GridPlayerController playerController;

        private void Awake()
        {
            playerController = GetComponent<GridPlayerController>();

            // Säkerställ att det finns en AudioSource
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 0f; // Sätt till 1f om ljudet ska vara 3D-anpassat i rummet
                }
            }
        }

        private void OnEnable()
        {
            if (playerController != null)
            {
                // Lyssna på när spelaren stoppas av en vägg
                playerController.MoveBlocked += OnMoveBlocked;
            }
        }

        private void OnDisable()
        {
            if (playerController != null)
            {
                playerController.MoveBlocked -= OnMoveBlocked;
            }
        }

        private void OnMoveBlocked(Vector3 blockedPosition)
        {
            if (wallBumpSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(wallBumpSound, volume);
            }
        }
    }
}