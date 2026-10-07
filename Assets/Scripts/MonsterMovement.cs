using AmazingMaze.Player;
using System.Collections;
using UnityEngine;

public class MonsterMovement : MonoBehaviour
{
    [Header("Grid")]
    public float gridSize = 2f;

    [Header("Wandering")]
    public float timeBetweenSteps = 1f;
    public float moveSpeed = 3f;

    [Header("Whistle Reaction")]
    public int stepsTowardsPlayer = 3;
    public float whistleStepSpeed = 0.4f;
    public float whistleGrowlDelay = 3f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip walkingGrowl;
    public AudioClip whistleResponseGrowl;
    public AudioClip deathSound;

    private bool reactingToWhistle = false;
    private bool playerDead = false;

    private Vector3[] directions =
    {
        Vector3.forward,
        Vector3.back,
        Vector3.left,
        Vector3.right
    };

    void Start()
    {
        StartCoroutine(Wander());

        // Lågt kontinuerligt ljud från monstret
        if (walkingGrowl != null && audioSource != null)
        {
            audioSource.clip = walkingGrowl;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    IEnumerator Wander()
    {
        while (true)
        {
            if (!reactingToWhistle)
            {
                Vector3 direction =
                    directions[Random.Range(0, directions.Length)];

                Vector3 targetPosition =
                    transform.position + direction * gridSize;

                // Gå bara dit om det finns WalkableSurface
                if (HasFloor(targetPosition))
                {
                    // Glid mjukt till nästa ruta
                    yield return StartCoroutine(
                        MoveSmoothly(targetPosition)
                    );
                }
            }

            yield return new WaitForSeconds(timeBetweenSteps);
        }
    }

    IEnumerator MoveSmoothly(Vector3 targetPosition)
    {
        Vector3 startPosition = transform.position;

        float distance =
            Vector3.Distance(startPosition, targetPosition);

        float duration = distance / moveSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / duration
            );

            transform.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                t
            );

            yield return null;
        }

        // Se till att monstret hamnar exakt på gridrutan
        transform.position = targetPosition;
    }

    public void HearWhistle(Vector3 playerPosition)
    {
        if (!reactingToWhistle)
        {
            StartCoroutine(
                ReactToWhistle(playerPosition)
            );
        }
    }

    IEnumerator PlayGrowlAfterDelay()
    {
        yield return new WaitForSeconds(
            whistleGrowlDelay
        );

        if (whistleResponseGrowl != null &&
            audioSource != null &&
            !playerDead)
        {
            audioSource.PlayOneShot(
                whistleResponseGrowl
            );
        }
    }

    IEnumerator ReactToWhistle(Vector3 playerPosition)
    {
        reactingToWhistle = true;

        Debug.Log("Monster heard the whistle!");

        // Morra efter fördröjningen
        StartCoroutine(
            PlayGrowlAfterDelay()
        );

        // Ta steg mot platsen där spelaren visslade
        for (int i = 0; i < stepsTowardsPlayer; i++)
        {
            Vector3 difference =
                playerPosition - transform.position;

            // Om monstret redan nått spelaren
            if (Mathf.Abs(difference.x) < 0.1f &&
                Mathf.Abs(difference.z) < 0.1f)
            {
                break;
            }

            Vector3 direction;

            if (Mathf.Abs(difference.x) >
                Mathf.Abs(difference.z))
            {
                direction = difference.x > 0
                    ? Vector3.right
                    : Vector3.left;
            }
            else
            {
                direction = difference.z > 0
                    ? Vector3.forward
                    : Vector3.back;
            }

            Vector3 targetPosition =
                transform.position +
                direction * gridSize;

            // Gå bara om nästa ruta har WalkableSurface
            if (HasFloor(targetPosition))
            {
                // Glid mot nästa ruta
                yield return StartCoroutine(
                    MoveSmoothly(targetPosition)
                );
            }

            yield return new WaitForSeconds(
                whistleStepSpeed
            );
        }

        reactingToWhistle = false;
    }

    // Kollar om det finns WalkableSurface
    // under positionen
    bool HasFloor(Vector3 position)
    {
        RaycastHit[] hits = Physics.RaycastAll(
            position + Vector3.up * 2f,
            Vector3.down,
            5f
        );

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider
                .GetComponentInParent<WalkableSurface>() != null)
            {
                return true;
            }
        }

        return false;
    }

    // När monstret träffar spelaren
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            RespawnPlayer respawn =
                collision.gameObject.GetComponent<RespawnPlayer>();

            if (respawn != null)
            {
                respawn.Respawn();
            }

            PlayerCaught();
        }
    }

    void PlayerCaught()
    {
        if (playerDead)
            return;

        playerDead = true;

        Debug.Log("PLAYER DIED!");

        if (deathSound != null &&
            audioSource != null)
        {
            audioSource.PlayOneShot(
                deathSound
            );
        }
    }
}