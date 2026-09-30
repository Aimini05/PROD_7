using UnityEngine;

public class PlayerWhistle : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip whistleSound;

    [Header("Monster")]
    public MonsterMovement monster;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            Whistle();
        }
    }

    void Whistle()
    {
        // Spela visselljud
        if (whistleSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(whistleSound);
        }

        // Berätta för monstret var spelaren är
        if (monster != null)
        {
            monster.HearWhistle(transform.position);
        }

        Debug.Log("Player whistles!");
    }
}