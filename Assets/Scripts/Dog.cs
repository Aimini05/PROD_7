using UnityEngine;
using System.Collections;

public class Dog : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip barkSound;
    public AudioClip collisionSound;

    [Header("Timing")]
    public float barkDelay = 3f;

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