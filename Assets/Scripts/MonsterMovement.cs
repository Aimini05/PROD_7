using AmazingMaze.Player;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement; // Krävs för att ladda om scenen

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
    private bool waitingForRestart = false; // Håller koll på om vi väntar på Enter

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

    void Update()
    {
        // Om spelaren är död och vi väntar på att de ska trycka på Enter för att starta om
        if (waitingForRestart && Input.GetKeyDown(KeyCode.Return))
        {
            // Återställ tidsskalan innan vi laddar om scenen
            Time.timeScale = 1f;

            // Laddar om den aktiva scenen (börjar om leveln)
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
            // Använd Mathf.Abs / deltaTime, men eftersom vi pausar vid dödsfall är detta säkert
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
        RaycastHit[] hits = Physics.RaycastAll(position + Vector3.up * Vector3.up.y * 2f, Vector3.down, 5f); // Korrigerat till standard Vector3.up * 2f

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
            // Vi tar bort det gamla direkt-respawn-anropet härifrån
            PlayerCaught();
        }
    }

    void PlayerCaught()
    {
        if (playerDead)
            return;

        playerDead = true;
        Debug.Log("PLAYER DIED!");

        // Starta en coroutine som hanterar ljudet och pausen
        StartCoroutine(HandleDeathSequence());
    }

    IEnumerator HandleDeathSequence()
    {
        // Spela upp dödsljudet. Eftersom vi kommer pausa spelet (Time.timeScale = 0) 
        // kan vi använda PlayClipAtPoint så att ljudet spelas klart oberoende av tids-skalan, 
        // eller AudioSource med ignoreListenerPause = true. 
        // Här är ett smidigt sätt via AudioSource om den är satt till att ignorera paus:
        if (audioSource != null && deathSound != null)
        {
            audioSource.ignoreListenerPause = true;
            audioSource.PlayOneShot(deathSound);
        }

        // Räkna ut hur lång tid döds-ljudet är så vi vet hur länge vi ska vänta
        float audioLength = (deathSound != null) ? deathSound.length : 2f;

        // Pausa spelet (observera att WaitForSeconds inte fungerar när Time.timeScale = 0, 
        // så vi använder unscaled tid)
        float timer = 0f;
        while (timer < audioLength)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        // Pausa hela spelet helt efter att ljudet har spelats klart (eller medan det spelas, 
        // beroende på om du vill att ljudet ska spelas under paus)
        Time.timeScale = 0f;

        // Tillåt nu att avlyssna Enter-tangenten i Update
        waitingForRestart = true;
    }
}