using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    private const string LobbySceneName = "LobbyScene";
    private const string InGameSceneName = "InGameScene";

    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[SceneLoader] Scene name is empty.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    public void LoadLobby()
    {
        LoadScene(LobbySceneName);
    }

    public void LoadInGame()
    {
        LoadScene(InGameSceneName);
    }
}
