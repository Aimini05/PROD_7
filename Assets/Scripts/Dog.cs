using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class Dog : MonoBehaviour
{
    [Header("Random spawn")]
    [Tooltip("Place independent markers on reachable tiles, then assign them here. Do not parent them to the dog.")]
    public Transform[] spawnPoints;
    [Tooltip("Assign the player, or leave empty to find the object tagged Player.")]
    public Transform player;
    [Min(0f), Tooltip("Minimum horizontal distance in Unity units, not number of tiles.")]
    public float minimumSpawnDistance = 8f;
    public bool randomizeSpawnOnStart = true;

    private Transform previousSpawnPoint;

    [Header("Wandering experiment")]
    public bool enableWandering = true;
    [Min(0.1f)] public float wanderInterval = 20f;
    [Min(0.1f)] public float wanderTileSize = 1f;
    [Min(0.05f)] public float secondsPerStep = 0.6f;
    [Tooltip("Solid walls, furniture, doors and characters. Triggers are ignored.")]
    public LayerMask wanderObstacleLayers = ~0;

    private float nextWanderTime;
    private int stepsRemaining;
    private bool isWandering;
    private Vector3 stepTarget;
    private Bounds dogBounds;
    private Rigidbody wanderBody;
    private bool savedKinematic;
    private bool ownsBody;
    private static readonly Vector3[] WanderDirections =
        { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip barkSound;
    public AudioClip collisionSound;

    [Header("Ending")]
    public float fadeOutDuration = 2f;

    // Keep this latched across movement and disable/enable cycles.
    // A new dog instance (for example after a scene reload) starts fresh.
    private bool hasPlayedCollisionSound;

    [Header("Timing")]
    public float barkDelay = 3f;

    private void Start()
    {
        if (randomizeSpawnOnStart)
            TryRandomizeSpawn();
        nextWanderTime = Time.time + Mathf.Max(0.1f, wanderInterval);
    }

    private void FixedUpdate()
    {
        if (!enableWandering)
        {
            StopWandering();
            nextWanderTime = Time.time + Mathf.Max(0.1f, wanderInterval);
            return;
        }

        if (!isWandering)
        {
            if (Time.time < nextWanderTime) return;
            nextWanderTime = Time.time + Mathf.Max(0.1f, wanderInterval);
            stepsRemaining = Random.Range(1, 3);
            if (!ReadDogBounds() || !ChooseNextStep()) return;
            wanderBody = GetComponent<Rigidbody>();
            if (wanderBody != null)
            {
                savedKinematic = wanderBody.isKinematic;
                wanderBody.isKinematic = true;
                ownsBody = true;
            }
            isWandering = true;
        }

        Vector3 next = Vector3.MoveTowards(transform.position, stepTarget,
            Mathf.Max(0.1f, wanderTileSize) / Mathf.Max(0.05f, secondsPerStep) * Time.fixedDeltaTime);
        // Recheck during travel, in case a door closes or a character enters the path.
        if (!CanTravel(transform.position, next))
        {
            StopWandering();
            return;
        }
        if (wanderBody != null) wanderBody.position = next;
        transform.position = next;
        if ((next - stepTarget).sqrMagnitude < 0.000001f)
        {
            stepsRemaining--;
            if (stepsRemaining <= 0 || !ChooseNextStep()) StopWandering();
        }
    }

    private bool ReadDogBounds()
    {
        bool found = false;
        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            if (!col.enabled || col.isTrigger || !col.gameObject.activeInHierarchy) continue;
            if (!found) dogBounds = col.bounds;
            else dogBounds.Encapsulate(col.bounds);
            found = true;
        }
        if (found) dogBounds.center -= transform.position;
        return found;
    }

    private bool ChooseNextStep()
    {
        int first = Random.Range(0, WanderDirections.Length);
        for (int i = 0; i < WanderDirections.Length; i++)
        {
            Vector3 candidate = transform.position + WanderDirections[(first + i) % 4]
                * Mathf.Max(0.1f, wanderTileSize);
            if (!CanTravel(transform.position, candidate)) continue;
            stepTarget = candidate;
            return true;
        }
        return false;
    }

    private bool CanTravel(Vector3 from, Vector3 to)
    {
        Physics.SyncTransforms();
        Vector3 delta = to - from;
        // Lift the bottom slightly so the supporting floor is not an obstacle.
        Vector3 half = dogBounds.extents;
        half.y = Mathf.Max(0.01f, half.y - 0.025f);
        Vector3 center = from + dogBounds.center + Vector3.up * 0.025f;
        foreach (Collider col in Physics.OverlapBox(center + delta, half, Quaternion.identity,
            wanderObstacleLayers, QueryTriggerInteraction.Ignore))
            if (!col.transform.IsChildOf(transform)) return false;
        if (delta.sqrMagnitude > 0f)
            foreach (RaycastHit hit in Physics.BoxCastAll(center, half, delta.normalized,
                Quaternion.identity, delta.magnitude, wanderObstacleLayers, QueryTriggerInteraction.Ignore))
                if (!hit.collider.transform.IsChildOf(transform)) return false;

        // Check floor support along the entire step, not only at its destination.
        int samples = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / 0.2f));
        for (int i = 0; i <= samples; i++)
        {
            Vector3 foot = Vector3.Lerp(from, to, (float)i / samples)
                + dogBounds.center - Vector3.up * dogBounds.extents.y;
            for (int corner = 0; corner < 4; corner++)
            {
                Vector3 probe = foot + new Vector3((corner % 2 == 0 ? -1 : 1) * half.x * 0.8f,
                    0.1f, (corner < 2 ? -1 : 1) * half.z * 0.8f);
                bool supported = false;
                foreach (RaycastHit hit in Physics.RaycastAll(probe, Vector3.down, 0.35f,
                    ~0, QueryTriggerInteraction.Ignore))
                    if (!hit.collider.transform.IsChildOf(transform) && hit.normal.y > 0.9f
                        && hit.collider.GetComponentInParent<AmazingMaze.Player.WalkableSurface>() != null)
                        supported = true;
                if (!supported) return false;
            }
        }
        return true;
    }

    private void StopWandering()
    {
        isWandering = false;
        if (ownsBody && wanderBody != null) wanderBody.isKinematic = savedKinematic;
        ownsBody = false;
    }

    private void OnDisable()
    {
        StopWandering();
        nextWanderTime = Time.time + Mathf.Max(0.1f, wanderInterval);
    }

    // Keeps this dog instance, so whistle/audio references remain intact.
    // Returns false rather than silently choosing a point too near the player.
    public bool TryRandomizeSpawn()
    {
        StopWandering();
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (player == null || spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("Dog spawn: assign a player and spawn points. Keeping the current position.", this);
            return false;
        }

        Transform chosen = null;
        int eligibleCount = 0;
        float minDistance = Mathf.Max(0f, minimumSpawnDistance);

        foreach (Transform point in spawnPoints)
        {
            if (point == null || point == transform || point.IsChildOf(transform)
                || !point.gameObject.activeInHierarchy || point == previousSpawnPoint)
                continue;

            Vector3 offset = point.position - player.position;
            offset.y = 0f;
            if (offset.sqrMagnitude < minDistance * minDistance)
                continue;

            // Reservoir sampling gives every eligible entry equal probability.
            eligibleCount++;
            if (Random.Range(0, eligibleCount) == 0)
                chosen = point;
        }

        if (chosen == null)
        {
            Debug.LogWarning("Dog spawn: no new point meets the minimum distance. Keeping the current position. Add more points or reduce the distance.", this);
            return false;
        }

        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null)
        {
            body.position = chosen.position;
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }
        transform.position = chosen.position;
        previousSpawnPoint = chosen;
        Physics.SyncTransforms();
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnPoints == null)
            return;

        foreach (Transform point in spawnPoints)
        {
            if (point == null)
                continue;
            Vector3 offset = player != null ? point.position - player.position : Vector3.zero;
            offset.y = 0f;
            Gizmos.color = player != null && offset.magnitude < minimumSpawnDistance
                ? Color.red : Color.cyan;
            Gizmos.DrawWireSphere(point.position, 0.3f);
        }
    }

    // När spelaren visslar
    public void HearWhistle()
    {
        StartCoroutine(BarkAfterDelay());
    }

    IEnumerator BarkAfterDelay()
    {
        yield return new WaitForSeconds(barkDelay);

        if (audioSource != null && barkSound != null)
        {
            audioSource.PlayOneShot(barkSound);
        }

        Debug.Log("Dog heard the whistle and barked!");
    }

    // Play the proximity voice line only once for this dog instance.

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (hasPlayedCollisionSound)
            return;

        PlaytestLogger.DogReached(other.transform.position);

        if (audioSource == null || collisionSound == null)
            return;

        hasPlayedCollisionSound = true;

        StartCoroutine(EndGame());
    }



    private IEnumerator EndGame()
    {
        enableWandering = false;
        StopWandering();

        // Samla alla aktiva ljudkällor
        AudioSource[] sources =
            FindObjectsByType<AudioSource>(
                FindObjectsSortMode.None
            );

        float[] startVolumes = new float[sources.Length];

        for (int i = 0; i < sources.Length; i++)
        {
            if (sources[i] != null)
                startVolumes[i] = sources[i].volume;
        }

        // Stoppa hundens tidigare ljud
        audioSource.Stop();

        // Starta avslutningsfrasen direkt
        audioSource.PlayOneShot(collisionSound);

        // Tona ut övriga ljud samtidigt
        float duration = Mathf.Max(0.01f, fadeOutDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            for (int i = 0; i < sources.Length; i++)
            {
                AudioSource source = sources[i];

                if (source != null && source != audioSource)
                {
                    source.volume =
                        Mathf.Lerp(startVolumes[i], 0f, t);
                }
            }

            yield return null;
        }

        // Stoppa övriga ljud efter uttoningen
        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source = sources[i];

            if (source != null && source != audioSource)
            {
                source.Stop();
                source.volume = startVolumes[i];
            }
        }

        // Vänta tills avslutningsfrasen är färdig
        float remainingTime =
            Mathf.Max(0f, collisionSound.length - duration);

        yield return new WaitForSecondsRealtime(
            remainingTime + 1f
        );

        SceneManager.LoadScene("Menutest");
    }


}
