using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AmazingMaze.Player
{
    /*
    * First-person, tile-by-tile movement for the  prototype.
    * The player can only face the four cardinal directions.
    * Up arrow steps forward, left/right arrows turn, and Space jumps in place.
    * Steps and turns are instant; short delays keep actions separated.
    */
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class GridPlayerController : MonoBehaviour {
        [Header("Grid")]
        // Distance between tiles and the reference point used to align the grid.
        [SerializeField, Min(0.1f)] private float tileSize = 2f;
        [SerializeField] private Vector3 gridOrigin = Vector3.zero;

        [Header("Timing")]
        [Tooltip("Minimum delay after an instant grid step.")]
        [SerializeField, Min(0.01f)] private float moveDuration = 0.18f;
        [Tooltip("Minimum delay after an instant 90-degree turn.")]
        [SerializeField, Min(0.01f)] private float turnDuration = 0.14f;

        [Header("Jump")]
        // Height in Unity units and total time for going up and returning to the tile.
        [SerializeField, Min(0.01f)] private float jumpHeight = 0.75f;
        [SerializeField, Min(0.01f)] private float jumpDuration = 0.5f;

        [Header("Collision")]
        // Layers and box dimensions used to check whether the next tile is occupied.
        [SerializeField] private LayerMask obstacleLayers = Physics.DefaultRaycastLayers;
        [SerializeField, Min(0.05f)] private float collisionRadius = 0.35f;
        [SerializeField, Min(0.1f)] private float collisionHeight = 1.7f;

        // Audio scripts can subscribe to these events; this controller does not play sounds.
        // Moved supplies the new position; MoveBlocked supplies the attempted position.
        // Turned supplies -1 for left or +1 for right.
        public event Action<Vector3> Moved;
        public event Action<int> Turned;
        public event Action<Vector3> MoveBlocked;

        public bool IsBusy => isJumping || actionTimeRemaining > 0f;
        // 0 = forward (+Z), 1 = right (+X), 2 = back (-Z), 3 = left (-X).
        public int HeadingQuarterTurns { get; private set; }

        // Reuse this array for every collision query instead of creating one per step.
        private readonly Collider[] collisionHits = new Collider[16];
        private float actionTimeRemaining;
        private bool isJumping;
        private Vector3 jumpStart;
        private float jumpElapsed;

        private void Awake() {
            // Align the starting position and rotation with the maze grid.
            SnapToGrid();
            HeadingQuarterTurns = Mathf.RoundToInt(transform.eulerAngles.y / 90f);
            HeadingQuarterTurns = Mod(HeadingQuarterTurns, 4);
            transform.rotation = Quaternion.Euler(0f, HeadingQuarterTurns * 90f, 0f);
        }

        private void Update() {
            if (isJumping) {
                // Advance using seconds, so jump duration does not depend on frame rate.
                jumpElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(jumpElapsed / Mathf.Max(0.01f, jumpDuration));
                // This arc is zero at takeoff/landing and reaches jumpHeight halfway through.
                // It is a scripted hop, with no gravity or ceiling collision simulation.
                transform.position = jumpStart + Vector3.up * (4f * jumpHeight * t * (1f - t));
                if (t >= 1f) {
                    transform.position = jumpStart;
                    isJumping = false;
                }
                return;
            }

            // Ignore new actions during the step/turn delay or when no keyboard is connected.
            actionTimeRemaining = Mathf.Max(0f, actionTimeRemaining - Time.deltaTime);
            Keyboard keyboard = Keyboard.current;
            if (IsBusy || keyboard == null) {
                return;
            }

            // One action per key press; holding a key does not repeat it.
            // If several keys are pressed together, Space takes priority, then left/right/up.
            if (keyboard.spaceKey.wasPressedThisFrame) {
                jumpStart = transform.position;
                jumpElapsed = 0f;
                isJumping = true;
            }
            else if (keyboard.leftArrowKey.wasPressedThisFrame) {
                Turn(-1);
            }
            else if (keyboard.rightArrowKey.wasPressedThisFrame) {
                Turn(1);
            }
            else if (keyboard.upArrowKey.wasPressedThisFrame) {
                TryMoveForward();
            }
        }

        private void TryMoveForward() {
            // Check the destination before moving exactly one tile in the facing direction.
            Vector3 direction = DirectionFromHeading(HeadingQuarterTurns);
            Vector3 destination = transform.position + direction * tileSize;

            if (IsDestinationBlocked(destination))
            {
                MoveBlocked?.Invoke(destination);
                return;
            }

            transform.position = destination;
            actionTimeRemaining = moveDuration;
            Moved?.Invoke(destination);
        }

        private bool IsDestinationBlocked(Vector3 destination) {
            // Place the check box just above the floor so the floor does not block movement.
            // This checks the destination only, not the space between the two tiles.
            Vector3 halfExtents = new Vector3(collisionRadius, collisionHeight * 0.5f, collisionRadius);
            Vector3 center = destination + Vector3.up * (collisionHeight * 0.5f + 0.05f);
            int hitCount = Physics.OverlapBoxNonAlloc(
                center,
                halfExtents,
                collisionHits,
                Quaternion.identity,
                obstacleLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++) {
                // The player's own collider and child colliders are not obstacles.
                if (collisionHits[i].transform != transform && !collisionHits[i].transform.IsChildOf(transform)) {
                    return true;
                }
            }

            // If the buffer filled, some colliders may be missing: block conservatively.
            return hitCount == collisionHits.Length;
        }

        private void Turn(int direction) {
            // Wrap around the four headings and immediately update the facing direction.
            HeadingQuarterTurns = Mod(HeadingQuarterTurns + direction, 4);
            transform.rotation = Quaternion.Euler(0f, HeadingQuarterTurns * 90f, 0f);
            actionTimeRemaining = turnDuration;
            Turned?.Invoke(direction);
        }

        private void OnDisable() {
            // If the controller is disabled mid-jump, restore the tile position and clear delays.
            if (isJumping) {
                transform.position = jumpStart;
            }
            isJumping = false;
            actionTimeRemaining = 0f;
        }

        private void SnapToGrid() {
            // Round only horizontal coordinates; preserve the player's starting floor height.
            Vector3 position = transform.position;
            position.x = gridOrigin.x + Mathf.Round((position.x - gridOrigin.x) / tileSize) * tileSize;
            position.z = gridOrigin.z + Mathf.Round((position.z - gridOrigin.z) / tileSize) * tileSize;
            transform.position = position;
        }

        private static Vector3 DirectionFromHeading(int heading) {
            // Convert the numbered heading into a world-space movement direction.
            switch (Mod(heading, 4))
            {
                case 0: return Vector3.forward;
                case 1: return Vector3.right;
                case 2: return Vector3.back;
                default: return Vector3.left;
            }
        }

        private static int Mod(int value, int modulus) {
            // This raps negative values correctly when turning left from 0.
            return (value % modulus + modulus) % modulus;
        }

        private void OnDrawGizmosSelected() {
            // Editor aid: show the next tile's collision box when the player is selected.
            Vector3 direction = DirectionFromHeading(Mathf.RoundToInt(transform.eulerAngles.y / 90f));
            Vector3 destination = transform.position + direction * tileSize;
            Vector3 center = destination + Vector3.up * (collisionHeight * 0.5f + 0.05f);

            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.65f);
            Gizmos.DrawWireCube(center, new Vector3(collisionRadius * 2f, collisionHeight, collisionRadius * 2f));
        }
    }
}
