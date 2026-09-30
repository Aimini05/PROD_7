using UnityEngine;

public class Dog : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip barkSound;
    public AudioClip collisionSound;

    // När spelaren visslar
    public void HearWhistle()
    {
        if (audioSource != null && barkSound != null)
        {
            audioSource.PlayOneShot(barkSound);
        }

        Debug.Log("Dog heard the whistle and barked!");
    }

    // När spelaren kolliderar med hunden
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (audioSource != null && collisionSound != null)
            {
                audioSource.PlayOneShot(collisionSound);
            }

            Debug.Log("Player found the dog!");
        }
    }
}