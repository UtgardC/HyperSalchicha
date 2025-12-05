using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Screen")]
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private InGameUIManager inGameUIManager;

    [Header("Run Data")]
    public int currentRound;
    public int maxRoundRecord;
    public int cuajosActuales;
    public bool newRecord;

    private bool shown;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    public void NextRound()
    {
        int next = currentRound + 1;
        SetRound(next);
        inGameUIManager.UpdateCurrentRoundDisplay(next);
    }

    public void SetRound(int round)
    {
        currentRound = round;
        if (currentRound > maxRoundRecord)
        {
            maxRoundRecord = currentRound;
            newRecord = true;
        }
        else
        {
            newRecord = false;
        }
    }

    public void SetCuajos(int total)
    {
        cuajosActuales = total;
    }

    public void AddCuajos(int amount)
    {
        cuajosActuales += amount;
    }

    public void ShowGameOver()
    {
        if (shown) return;
        shown = true;

        Time.timeScale = 0f;
        gameOverScreen.SetActive(true);

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


