using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AmazingMaze.Player
{
    /// <summary>
    /// First-person, tile-by-tile movement for the maze prototype.
    /// The player can only face the four cardinal directions.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class GridPlayerController : MonoBehaviour {
        [Header("Grid")]
        [SerializeField, Min(0.1f)] private float tileSize = 2f;
        [SerializeField] private Vector3 gridOrigin = Vector3.zero;

        [Header("Timing")]
        [SerializeField, Min(0.01f)] private float moveDuration = 0.18f;
        [SerializeField, Min(0.01f)] private float turnDuration = 0.14f;

        [Header("Collision")]
        [SerializeField] private LayerMask obstacleLayers = Physics.DefaultRaycastLayers;
        [SerializeField, Min(0.05f)] private float collisionRadius = 0.35f;
        [SerializeField, Min(0.1f)] private float collisionHeight = 1.7f;

        public event Action<Vector3> Moved;
        public event Action<int> Turned;
        public event Action<Vector3> MoveBlocked;

        public bool IsBusy { get; private set; }
        public int HeadingQuarterTurns { get; private set; }

        private void Awake() {
            SnapToGrid();
            HeadingQuarterTurns = Mathf.RoundToInt(transform.eulerAngles.y / 90f);
            HeadingQuarterTurns = Mod(HeadingQuarterTurns, 4);
            transform.rotation = Quaternion.Euler(0f, HeadingQuarterTurns * 90f, 0f);
        }

        private void Update() {
            if (IsBusy || Keyboard.current == null) {
                return;
            }

            if (Keyboard.current.leftArrowKey.wasPressedThisFrame) {
                StartCoroutine(Turn(-1));
            }
            else if (Keyboard.current.rightArrowKey.wasPressedThisFrame) {
                StartCoroutine(Turn(1));
            }
            else if (Keyboard.current.upArrowKey.wasPressedThisFrame) {
                TryMoveForward();
            }
        }

        private void TryMoveForward() {
            Vector3 direction = DirectionFromHeading(HeadingQuarterTurns);
            Vector3 destination = transform.position + direction * tileSize;

            if (IsDestinationBlocked(destination))
            {
                MoveBlocked?.Invoke(destination);
                return;
            }

            StartCoroutine(Move(destination));
        }

        private bool IsDestinationBlocked(Vector3 destination) {
            Vector3 halfExtents = new Vector3(collisionRadius, collisionHeight * 0.5f, collisionRadius);
            Vector3 center = destination + Vector3.up * (collisionHeight * 0.5f + 0.05f);
            Collider[] hits = Physics.OverlapBox(
                center,
                halfExtents,
                Quaternion.identity,
                obstacleLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hits.Length; i++) {
                if (hits[i].transform != transform && !hits[i].transform.IsChildOf(transform)) {
                    return true;
                }
            }

            return false;
        }

        private IEnumerator Move(Vector3 destination) {
            IsBusy = true;
            Vector3 start = transform.position;
            float elapsed = 0f;

            while (elapsed < moveDuration) {
                elapsed += Time.deltaTime;
                float t = SmoothStep01(elapsed / moveDuration);
                transform.position = Vector3.LerpUnclamped(start, destination, t);
                yield return null;
            }

            transform.position = destination;
            IsBusy = false;
            Moved?.Invoke(destination);
        }

        private IEnumerator Turn(int direction) {
            IsBusy = true;
            Quaternion start = transform.rotation;
            int targetHeading = Mod(HeadingQuarterTurns + direction, 4);
            Quaternion target = Quaternion.Euler(0f, targetHeading * 90f, 0f);
            float elapsed = 0f;

            while (elapsed < turnDuration) {
                elapsed += Time.deltaTime;
                float t = SmoothStep01(elapsed / turnDuration);
                transform.rotation = Quaternion.SlerpUnclamped(start, target, t);
                yield return null;
            }

            HeadingQuarterTurns = targetHeading;
            transform.rotation = target;
            IsBusy = false;
            Turned?.Invoke(direction);
        }

        private void SnapToGrid() {
            Vector3 position = transform.position;
            position.x = gridOrigin.x + Mathf.Round((position.x - gridOrigin.x) / tileSize) * tileSize;
            position.z = gridOrigin.z + Mathf.Round((position.z - gridOrigin.z) / tileSize) * tileSize;
            transform.position = position;
        }

        private static Vector3 DirectionFromHeading(int heading) {
            switch (Mod(heading, 4))
            {
                case 0: return Vector3.forward;
                case 1: return Vector3.right;
                case 2: return Vector3.back;
                default: return Vector3.left;
            }
        }

        private static float SmoothStep01(float value) {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private static int Mod(int value, int modulus) {
            return (value % modulus + modulus) % modulus;
        }

        private void OnDrawGizmosSelected() {
            Vector3 direction = DirectionFromHeading(Mathf.RoundToInt(transform.eulerAngles.y / 90f));
            Vector3 destination = transform.position + direction * tileSize;
            Vector3 center = destination + Vector3.up * (collisionHeight * 0.5f + 0.05f);

            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.65f);
            Gizmos.DrawWireCube(center, new Vector3(collisionRadius * 2f, collisionHeight, collisionRadius * 2f));
        }
    }
}
