using UnityEngine;
using System.Collections;

public class MonsterMovement : MonoBehaviour
{
    [Header("Grid")]
    public float gridSize = 1f;

    [Header("Wandering")]
    public float timeBetweenSteps = 1f;

    [Header("Whistle Reaction")]
    public int stepsTowardsPlayer = 3;
    public float whistleStepSpeed = 0.4f;

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

                transform.position = targetPosition;
            }

            yield return new WaitForSeconds(timeBetweenSteps);
        }
    }

    public void HearWhistle(Vector3 playerPosition)
    {
        if (!reactingToWhistle)
        {
            StartCoroutine(ReactToWhistle(playerPosition));
        }
    }

    IEnumerator ReactToWhistle(Vector3 playerPosition)
    {
        reactingToWhistle = true;

        Debug.Log("Monster heard the whistle!");

        // Monstret morrar när det hör visslingen
        if (whistleResponseGrowl != null && audioSource != null)
        {
            audioSource.PlayOneShot(whistleResponseGrowl);
        }

        // Ta tre steg mot platsen där spelaren visslade
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

            if (Mathf.Abs(difference.x) > Mathf.Abs(difference.z))
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

            transform.position += direction * gridSize;

            yield return new WaitForSeconds(whistleStepSpeed);
        }

        reactingToWhistle = false;
    }

    // När monstret träffar spelaren
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerCaught();
        }
    }

    void PlayerCaught()
    {
        if (playerDead)
            return;

        playerDead = true;

        Debug.Log("PLAYER DIED!");

        if (deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSound);
        }
    }
}