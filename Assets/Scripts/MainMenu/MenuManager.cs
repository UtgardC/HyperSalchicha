using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject settingsPanel; 

    public void StartGame()
    {
        SceneManager.LoadScene("Level");
    }
    public void ToggleSettings()
    {
        settingsPanel.SetActive(!settingsPanel.activeSelf);
    }
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
