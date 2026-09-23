using UnityEngine;

public class SpeechTest : MonoBehaviour
{
    void Start()
    {
        Debug.Log("Speech test TEXT printed");
        Speech.Interrupt("Speech test successful");
        
    }
}