using UnityEngine;
using UnityEngine.SceneManagement;


public class GameManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public int currentRound = 0;
    public int maxRoundRecord = 0;

    public int cuajosActuales = 0;

    public bool newRecord = false;
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }








    [Header("Screen")]
    [SerializeField] private GameObject gameOverScreen;

    private bool shown;
    public void ShowGameOver()
    {
        if (shown) return;
        shown = true;

        Time.timeScale = 0f;
        if (gameOverScreen != null) gameOverScreen.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // UI Button
    public void RestartLevel()
    {
        Time.timeScale = 1f;
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }

    // UI Button

    public void ReturnMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    // UI Button
    public void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}


