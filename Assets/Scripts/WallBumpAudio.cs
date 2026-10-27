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
        [SerializeField] private AudioClip furnitureBumpSound;
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
                playerController.MoveBlocked += OnMoveBlocked;
                playerController.ObstacleBlocked += OnObstacleBlocked;
            }
        }

        private void OnDisable()
        {
            if (playerController != null)
            {
                playerController.MoveBlocked -= OnMoveBlocked;
                playerController.ObstacleBlocked -= OnObstacleBlocked;
            }
        }

        private bool handledObstacle;

        private void OnMoveBlocked(Vector3 blockedPosition)
        {
            if (handledObstacle)
            {
                handledObstacle = false;
                return;
            }

            PlaySound(wallBumpSound);
        }

        private void OnObstacleBlocked(Collider obstacle)
        {
            handledObstacle = true;

            AudioClip selectedSound = wallBumpSound;

            if (obstacle != null)
            {
                BumpSurface surface = obstacle.GetComponentInParent<BumpSurface>();

                if (surface != null &&
                    surface.surfaceType == BumpSurface.SurfaceType.Furniture)
                {
                    selectedSound = furnitureBumpSound;
                }
            }

            PlaySound(selectedSound);
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip, volume);
            }
        }
    }
}