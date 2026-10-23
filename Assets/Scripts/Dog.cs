using UnityEngine;
using System.Collections;

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

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip barkSound;
    public AudioClip collisionSound;

    [Header("Timing")]
    public float barkDelay = 3f;

    private void Start()
    {
        if (randomizeSpawnOnStart)
            TryRandomizeSpawn();
    }

    // Keeps this dog instance, so whistle/audio references remain intact.
    // Returns false rather than silently choosing a point too near the player.
    public bool TryRandomizeSpawn()
    {
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

    // Spelas endast när spelaren går in i trigger-zonen
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && audioSource != null && collisionSound != null)
        {
            audioSource.PlayOneShot(collisionSound);
        }
    }
}
