/*using System;

using UnityEngine;

using UnityEngine.InputSystem;



namespace AmazingMaze.Player

{

    /*

    * First-person, tile-by-tile movement for the  prototype.

    * The player can only face the four cardinal directions.

    * Up arrow steps forward, left/right arrows turn, and Space jumps in place.

    * F interacts with the object directly ahead. Doors do not require a key.

    * Steps and turns animate smoothly while still ending exactly on the grid.

    

    [DisallowMultipleComponent]

    [RequireComponent(typeof(CapsuleCollider))]

    public sealed class OriginalGridPlayerController : MonoBehaviour
    {

        [Header("Grid")]

        // Distance between tiles and the reference point used to align the grid.

        [SerializeField, Min(0.1f)] private float tileSize = 2f;

        [SerializeField] private Vector3 gridOrigin = Vector3.zero;



        [Header("Timing")]

        [Tooltip("Time used to smoothly travel to the next tile.")]

        [SerializeField, Min(0.01f)] private float moveDuration = 0.18f;

        [Tooltip("Time used to smoothly rotate 90 degrees.")]

        [SerializeField, Min(0.01f)] private float turnDuration = 0.14f;



        [Header("Walking audio")]

        [Tooltip("Optional dedicated source. One is created at runtime when this is empty.")]

        [SerializeField] private AudioSource footstepAudioSource;

        [Tooltip("Optional footstep recordings. A quiet generated step is used while this list is empty.")]

        [SerializeField] private AudioClip[] footstepSounds;

        [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.65f;

        [SerializeField, Range(0f, 0.2f)] private float footstepPitchVariation = 0.06f;



        [Header("Jump")]

        // Height in Unity units and total time for going up and returning to the tile.

        [SerializeField, Min(0.01f)] private float jumpHeight = 0.75f;

        [SerializeField, Min(0.01f)] private float jumpDuration = 0.5f;



        [Header("Collision")]

        // Layers and box dimensions used to check whether the next tile is occupied.

        [SerializeField] private LayerMask obstacleLayers = Physics.DefaultRaycastLayers;

        [SerializeField, Min(0.05f)] private float collisionRadius = 0.35f;

        [SerializeField, Min(0.1f)] private float collisionHeight = 1.7f;



        [Header("Ground and stairs")]

        [SerializeField, Min(0.01f)] private float maxStepHeight = 0.6f;

        [SerializeField, Min(0.01f)] private float maxStepDown = 0.6f;



        [Header("Interaction")]

        [SerializeField, Min(0.1f)] private float interactionDistance = 2.5f;



        // Audio scripts can subscribe to these events; this controller does not play sounds.

        // Moved supplies the new position; MoveBlocked supplies the attempted position.

        // Turned supplies -1 for left or +1 for right.

        public event Action<Vector3> Moved;

        public event Action<int> Turned;

        public event Action<Vector3> MoveBlocked;

        public event Action<SimpleDoor> DoorOpened;

        // Shared event for successful interactions with any Interactable subclass.

        public event Action<Interactable> Interacted;



        public bool IsBusy => isJumping || isMoving || isTurning;

        // Editor tools read these values so their grid matches movement exactly.

        public float TileSize => tileSize;

        public Vector3 GridOrigin => gridOrigin;

        // 0 = forward (+Z), 1 = right (+X), 2 = back (-Z), 3 = left (-X).

        public int HeadingQuarterTurns { get; private set; }



        // Reuse this array for every collision query instead of creating one per step.

        private readonly Collider[] collisionHits = new Collider[16];

        private readonly RaycastHit[] groundHits = new RaycastHit[16];

        private bool isJumping;

        private Vector3 jumpStart;

        private float jumpElapsed;

        private bool isMoving;

        private Vector3 moveStart;

        private Vector3 moveDestination;

        private float moveElapsed;

        private bool isTurning;

        private Quaternion turnStart;

        private Quaternion turnDestination;

        private float turnElapsed;

        private AudioClip generatedFootstep;



        private void Awake()
        {

            // Align the starting position and rotation with the maze grid.

            SnapToGrid();

            Physics.SyncTransforms();

            if (TryGetGroundHeight(transform.position, out float groundHeight))
            {

                Vector3 grounded = transform.position;

                grounded.y = groundHeight;

                transform.position = grounded;

            }

            HeadingQuarterTurns = Mathf.RoundToInt(transform.eulerAngles.y / 90f);

            HeadingQuarterTurns = Mod(HeadingQuarterTurns, 4);

            transform.rotation = Quaternion.Euler(0f, HeadingQuarterTurns * 90f, 0f);

            PrepareFootstepAudio();

        }



        private void Update()
        {

            if (isJumping)
            {

                // Advance using seconds, so jump duration does not depend on frame rate.

                jumpElapsed += Time.deltaTime;

                float t = Mathf.Clamp01(jumpElapsed / Mathf.Max(0.01f, jumpDuration));

                // This arc is zero at takeoff/landing and reaches jumpHeight halfway through.

                // It is a scripted hop, with no gravity or ceiling collision simulation.

                transform.position = jumpStart + Vector3.up * (4f * jumpHeight * t * (1f - t));

                if (t >= 1f)
                {

                    transform.position = jumpStart;

                    isJumping = false;

                }

                return;

            }



            if (isMoving)
            {

                moveElapsed += Time.deltaTime;

                float t = Mathf.Clamp01(moveElapsed / Mathf.Max(0.01f, moveDuration));

                float easedT = t * t * (3f - 2f * t);

                transform.position = Vector3.LerpUnclamped(moveStart, moveDestination, easedT);

                if (t >= 1f)
                {

                    transform.position = moveDestination;

                    isMoving = false;

                    Moved?.Invoke(moveDestination);

                }

                return;

            }



            if (isTurning)
            {

                turnElapsed += Time.deltaTime;

                float t = Mathf.Clamp01(turnElapsed / Mathf.Max(0.01f, turnDuration));

                float easedT = t * t * (3f - 2f * t);

                transform.rotation = Quaternion.SlerpUnclamped(turnStart, turnDestination, easedT);

                if (t >= 1f)
                {

                    transform.rotation = turnDestination;

                    isTurning = false;

                }

                return;

            }



            // Ignore new actions during an animation or when no keyboard is connected.

            Keyboard keyboard = Keyboard.current;

            if (IsBusy || keyboard == null)
            {

                return;

            }



            // One action per key press; holding a key does not repeat it.

            // If several keys are pressed together, F takes priority, then Space/left/right/up.

            if (keyboard.fKey.wasPressedThisFrame)
            {

                TryInteract();

            }

            else if (keyboard.spaceKey.wasPressedThisFrame)
            {

                jumpStart = transform.position;

                jumpElapsed = 0f;

                isJumping = true;

            }

            else if (keyboard.leftArrowKey.wasPressedThisFrame)
            {

                Turn(-1);

            }

            else if (keyboard.rightArrowKey.wasPressedThisFrame)
            {

                Turn(1);

            }

            else if (keyboard.upArrowKey.wasPressedThisFrame)
            {

                TryMoveForward();

            }

        }



        private void TryMoveForward()
        {

            // Check the entire step so narrow walls, gaps and floor edges cannot be skipped.

            Vector3 direction = DirectionFromHeading(HeadingQuarterTurns);

            Vector3 destination = transform.position + direction * tileSize;

            Vector3 supported = transform.position;

            Physics.SyncTransforms();

            int samples = Mathf.CeilToInt(tileSize / 0.15f);



            for (int i = 1; i <= samples; i++)
            {

                Vector3 candidate = transform.position + direction * (tileSize * i / samples);

                candidate.y = supported.y;

                if (!TryGetGroundHeight(candidate, out float groundHeight))
                {

                    MoveBlocked?.Invoke(destination);

                    return;

                }

                candidate.y = groundHeight;

                if (IsDestinationBlocked(candidate))
                {

                    MoveBlocked?.Invoke(destination);

                    return;

                }

                supported = candidate;

            }



            moveStart = transform.position;

            moveDestination = supported;

            moveElapsed = 0f;

            isMoving = true;

            PlayFootstep();

        }



        private bool IsDestinationBlocked(Vector3 destination)
        {

            // Place the check box just above the floor so the floor does not block movement.

            // Called at each sample along the step after finding the floor height.

            Vector3 halfExtents = new Vector3(collisionRadius, collisionHeight * 0.5f, collisionRadius);

            Vector3 center = destination + Vector3.up * (collisionHeight * 0.5f + 0.05f);

            int hitCount = Physics.OverlapBoxNonAlloc(

                center,

                halfExtents,

                collisionHits,

                Quaternion.identity,

                obstacleLayers,

                QueryTriggerInteraction.Ignore);



            for (int i = 0; i < hitCount; i++)
            {

                // The player's own collider and child colliders are not obstacles.

                if (collisionHits[i].transform != transform && !collisionHits[i].transform.IsChildOf(transform))
                {

                    return true;

                }

            }



            // If the buffer filled, some colliders may be missing: block conservatively.

            return hitCount == collisionHits.Length;

        }



        private bool TryGetGroundHeight(Vector3 position, out float height)
        {

            // Require support under the centre and four edges of the player's footprint.

            // Use the highest support to lift the feet clear of a step while approaching it.

            height = float.NegativeInfinity;

            for (int i = 0; i < 5; i++)
            {

                Vector3 point = position;

                if (i == 1) point.x += collisionRadius;

                if (i == 2) point.x -= collisionRadius;

                if (i == 3) point.z += collisionRadius;

                if (i == 4) point.z -= collisionRadius;

                Vector3 origin = point + Vector3.up * (maxStepHeight + 0.02f);

                int count = Physics.RaycastNonAlloc(origin, Vector3.down, groundHits,

                    maxStepHeight + maxStepDown + 0.04f, obstacleLayers, QueryTriggerInteraction.Ignore);

                if (count == groundHits.Length) return false;

                float support = float.NegativeInfinity;

                for (int hit = 0; hit < count; hit++)
                {

                    RaycastHit result = groundHits[hit];

                    if (result.normal.y < 0.9f || result.collider.GetComponentInParent<WalkableSurface>() == null) continue;

                    float rise = result.point.y - position.y;

                    if (rise > maxStepHeight + 0.001f || rise < -maxStepDown - 0.001f) continue;

                    support = Mathf.Max(support, result.point.y);

                }

                if (float.IsNegativeInfinity(support)) return false;

                height = Mathf.Max(height, support);

            }

            return true;

        }



        private void TryInteract()
        {

            // Use the first solid object ahead, so interaction cannot pass through walls.

            Physics.SyncTransforms();

            Vector3 origin = transform.position + Vector3.up * (collisionHeight * 0.5f);

            if (!Physics.Raycast(origin, DirectionFromHeading(HeadingQuarterTurns), out RaycastHit hit,

                interactionDistance, obstacleLayers, QueryTriggerInteraction.Ignore)) return;

            Interactable target = hit.collider.GetComponentInParent<Interactable>();

            if (target == null || !target.isActiveAndEnabled || !target.Interact(this)) return;

            Interacted?.Invoke(target);

            // Preserve the existing door-specific sound hook for scripts already using it.

            if (target is SimpleDoor door) DoorOpened?.Invoke(door);

        }



        private void Turn(int direction)
        {

            // Wrap around the four headings and smoothly rotate to the exact cardinal angle.

            HeadingQuarterTurns = Mod(HeadingQuarterTurns + direction, 4);

            turnStart = transform.rotation;

            turnDestination = Quaternion.Euler(0f, HeadingQuarterTurns * 90f, 0f);

            turnElapsed = 0f;

            isTurning = true;

            Turned?.Invoke(direction);

        }



        private void PrepareFootstepAudio()
        {

            if (footstepAudioSource == null)
            {

                AudioSource existingSource = GetComponent<AudioSource>();

                footstepAudioSource = gameObject.AddComponent<AudioSource>();

                footstepAudioSource.playOnAwake = false;

                footstepAudioSource.loop = false;

                footstepAudioSource.spatialBlend = 0f;

                if (existingSource != null)
                {

                    footstepAudioSource.outputAudioMixerGroup = existingSource.outputAudioMixerGroup;

                }

            }



            if (HasFootstepClips()) return;



            const int sampleRate = 22050;

            const float duration = 0.14f;

            int sampleCount = Mathf.CeilToInt(sampleRate * duration);

            float[] samples = new float[sampleCount];

            System.Random random = new System.Random(7319);

            for (int i = 0; i < sampleCount; i++)
            {

                float time = i / (float)sampleRate;

                float envelope = Mathf.Exp(-time * 30f);

                float thump = Mathf.Sin(2f * Mathf.PI * 72f * time) * 0.55f;

                float noise = ((float)random.NextDouble() * 2f - 1f) * 0.22f;

                samples[i] = (thump + noise) * envelope;

            }



            generatedFootstep = AudioClip.Create("Generated Footstep", sampleCount, 1, sampleRate, false);

            generatedFootstep.SetData(samples, 0);

        }



        private bool HasFootstepClips()
        {

            if (footstepSounds == null) return false;

            for (int i = 0; i < footstepSounds.Length; i++)
            {

                if (footstepSounds[i] != null) return true;

            }

            return false;

        }



        private void PlayFootstep()
        {

            if (footstepAudioSource == null) return;



            AudioClip clip = generatedFootstep;

            if (HasFootstepClips())
            {

                int start = UnityEngine.Random.Range(0, footstepSounds.Length);

                for (int offset = 0; offset < footstepSounds.Length; offset++)
                {

                    AudioClip candidate = footstepSounds[(start + offset) % footstepSounds.Length];

                    if (candidate == null) continue;

                    clip = candidate;

                    break;

                }

            }



            if (clip == null) return;

            footstepAudioSource.pitch = 1f + UnityEngine.Random.Range(-footstepPitchVariation, footstepPitchVariation);

            footstepAudioSource.PlayOneShot(clip, footstepVolume);

        }



        private void OnDisable()
        {

            // Finish any partial action at its exact grid-aligned destination.

            if (isJumping)
            {

                transform.position = jumpStart;

            }

            else if (isMoving)
            {

                transform.position = moveDestination;

            }

            if (isTurning)
            {

                transform.rotation = turnDestination;

            }

            isJumping = false;

            isMoving = false;

            isTurning = false;

        }



        private void SnapToGrid()
        {

            // Round only horizontal coordinates; preserve the player's starting floor height.

            Vector3 position = transform.position;

            position.x = gridOrigin.x + Mathf.Round((position.x - gridOrigin.x) / tileSize) * tileSize;

            position.z = gridOrigin.z + Mathf.Round((position.z - gridOrigin.z) / tileSize) * tileSize;

            transform.position = position;

        }



        private static Vector3 DirectionFromHeading(int heading)
        {

            // Convert the numbered heading into a world-space movement direction.

            switch (Mod(heading, 4))

            {

                case 0: return Vector3.forward;

                case 1: return Vector3.right;

                case 2: return Vector3.back;

                default: return Vector3.left;

            }

        }



        private static int Mod(int value, int modulus)
        {

            // This wraps negative values correctly when turning left from 0.

            return (value % modulus + modulus) % modulus;

        }



        private void OnDrawGizmosSelected()
        {

            // Editor aid: show the next tile's collision box when the player is selected.

            Vector3 direction = DirectionFromHeading(Mathf.RoundToInt(transform.eulerAngles.y / 90f));

            Vector3 destination = transform.position + direction * tileSize;

            Vector3 center = destination + Vector3.up * (collisionHeight * 0.5f + 0.05f);



            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.65f);

            Gizmos.DrawWireCube(center, new Vector3(collisionRadius * 2f, collisionHeight, collisionRadius * 2f));

        }

    }

}

*/