using AmazingMaze.Player;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MonsterMovement : MonoBehaviour
{
    [Header("Volumes")]
    [Range(0f, 1f)]
    public float walkingGrowlVolume = 0.5f;
    [Range(0f, 1f)]
    public float deathSoundVolume = 1.0f; // Egen volym för dödsrösten

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
    public AudioSource audioSource; // Endast en AudioSource behövs!
    public AudioClip walkingGrowl;
    public AudioClip whistleResponseGrowl;
    public AudioClip deathSound;

    private bool reactingToWhistle = false;
    private bool playerDead = false;
    private bool waitingForRestart = false;

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

        // Sätt igång det löpande morrandet med dess specifika volym
        if (walkingGrowl != null && audioSource != null)
        {
            audioSource.clip = walkingGrowl;
            audioSource.volume = walkingGrowlVolume;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    void Update()
    {
        if (waitingForRestart && Input.GetKeyDown(KeyCode.Return))
        {
            AudioListener.pause = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    IEnumerator Wander()
    {
        while (!playerDead)
        {
            if (!reactingToWhistle)
            {
                Vector3 direction = directions[Random.Range(0, directions.Length)];
                Vector3 targetPosition = transform.position + direction * gridSize;

                if (HasFloor(targetPosition))
                {
                    yield return StartCoroutine(MoveSmoothly(targetPosition));
                }
            }

            yield return new WaitForSeconds(timeBetweenSteps);
        }
    }

    IEnumerator MoveSmoothly(Vector3 targetPosition)
    {
        Vector3 startPosition = transform.position;
        float distance = Vector3.Distance(startPosition, targetPosition);
        float duration = distance / moveSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        transform.position = targetPosition;
    }

    public void HearWhistle(Vector3 playerPosition)
    {
        if (!reactingToWhistle && !playerDead)
        {
            StartCoroutine(ReactToWhistle(playerPosition));
        }
    }

    IEnumerator PlayGrowlAfterDelay()
    {
        yield return new WaitForSeconds(whistleGrowlDelay);

        if (whistleResponseGrowl != null && audioSource != null && !playerDead)
        {
            audioSource.PlayOneShot(whistleResponseGrowl);
        }
    }

    IEnumerator ReactToWhistle(Vector3 playerPosition)
    {
        reactingToWhistle = true;

        StartCoroutine(PlayGrowlAfterDelay());

        for (int i = 0; i < stepsTowardsPlayer; i++)
        {
            if (playerDead) break;

            Vector3 difference = playerPosition - transform.position;

            if (Mathf.Abs(difference.x) < 0.1f && Mathf.Abs(difference.z) < 0.1f)
            {
                break;
            }

            Vector3 direction;
            if (Mathf.Abs(difference.x) > Mathf.Abs(difference.z))
            {
                direction = difference.x > 0 ? Vector3.right : Vector3.left;
            }
            else
            {
                direction = difference.z > 0 ? Vector3.forward : Vector3.back;
            }

            Vector3 targetPosition = transform.position + direction * gridSize;

            if (HasFloor(targetPosition))
            {
                yield return StartCoroutine(MoveSmoothly(targetPosition));
            }

            yield return new WaitForSeconds(whistleStepSpeed);
        }

        reactingToWhistle = false;
    }

    bool HasFloor(Vector3 position)
    {
        RaycastHit[] hits = Physics.RaycastAll(position + Vector3.up * 2f, Vector3.down, 5f);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.GetComponentInParent<WalkableSurface>() != null)
            {
                return true;
            }
        }

        return false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") && !playerDead)
        {
            PlaytestLogger.PlayerCaught(collision.transform.position);
            // Stäng av spelarens rörelseskript direkt vid krock
            MonoBehaviour[] playerScripts = collision.gameObject.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in playerScripts)
            {
                if (script != this)
                {
                    script.enabled = false;
                }
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

        // Stoppa monstrets vanliga ljud direkt
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        StartCoroutine(HandleDeathSequence());
    }

    IEnumerator HandleDeathSequence()
    {
        // 1. Tysta allt annat ljud i spelet
        AudioListener.pause = true;

        // 2. Byt till dödsljudets volym på samma AudioSource och spela upp det
        if (audioSource != null && deathSound != null)
        {
            audioSource.ignoreListenerPause = true;
            audioSource.volume = deathSoundVolume; // Sätter separat volym direkt här!
            audioSource.PlayOneShot(deathSound);
        }

        // 3. Vänta tills döds-ljudet har spelat klart
        float audioLength = (deathSound != null) ? deathSound.length : 2f;
        float timer = 0f;
        while (timer < audioLength)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        // 4. Pausa spelet helt och hållet och aktivera Enter-omstart
        Time.timeScale = 0f;
        waitingForRestart = true;
    }
}
