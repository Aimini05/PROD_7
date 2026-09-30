using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public string gameSceneName = "Game"; // change to match your actual scene name

    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }
}