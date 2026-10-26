using UnityEngine;

public class PlayerWhistle : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip[] whistleSounds; // dra in dina 3 ljud här

    [Header("Monster")]
    public MonsterMovement monster;

    [Header("Dog")]
    public Dog dog;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            Whistle();
        }
    }

    void Whistle()
    {
        PlaytestLogger.Whistle(transform.position);
        // Spela ett slumpat visselljud
        if (whistleSounds != null && whistleSounds.Length > 0 && audioSource != null)
        {
            AudioClip clip = whistleSounds[Random.Range(0, whistleSounds.Length)];
            audioSource.PlayOneShot(clip);
        }

        // Berätta för monstret var spelaren är
        if (monster != null)
        {
            monster.HearWhistle(transform.position);
        }

        // Få hunden att skälla
        if (dog != null)
        {
            dog.HearWhistle();
        }

        Debug.Log("Player whistles!");
    }
}
