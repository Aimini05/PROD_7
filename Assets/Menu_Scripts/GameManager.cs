using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public string gameSceneName = "Cutscene"; // change to match your actual scene name

    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }
}