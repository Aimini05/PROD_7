using UnityEngine;

public class Key : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip shimmerSound;  // loopas tills nyckeln plockas upp
    public AudioClip pickupSound;   // spelas en gång

    [Header("Pickup")]
    public KeyCode pickupKey = KeyCode.F;

    private PlayerInventory playerInRange;
    private bool pickedUp = false;

    void Start()
    {
        if (audioSource != null && shimmerSound != null)
        {
            audioSource.clip = shimmerSound;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    void Update()
    {
        if (!pickedUp && playerInRange != null && Input.GetKeyDown(pickupKey))
        {
            PickUp();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = other.GetComponentInParent<PlayerInventory>();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = null;
        }
    }

    void PickUp()
    {
        pickedUp = true;
        playerInRange.hasKey = true;

        // Stäng av shimmer och spela pickup-ljudet en gång
        float destroyDelay = 0f;
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.loop = false;

            if (pickupSound != null)
            {
                audioSource.PlayOneShot(pickupSound);
                destroyDelay = pickupSound.length;
            }
        }

        // Göm nyckeln direkt, men låt objektet leva tills ljudet spelats klart
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            r.enabled = false;
        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.enabled = false;

        Debug.Log("Player picked up the key!");
        Destroy(gameObject, destroyDelay);
    }
}