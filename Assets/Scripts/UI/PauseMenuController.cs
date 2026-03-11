using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[AddComponentMenu("HyperSalchicha/UI/Pause Menu Controller")]
public class PauseMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject pauseMenuRoot;
    [SerializeField] private RectTransform pauseMenuPanel;
    [SerializeField] private GameObject optionsMenuRoot;
    [SerializeField] private RectTransform optionsMenuPanel;

    [Header("Animation")]
    [SerializeField] private float openDuration = 0.35f;
    [SerializeField] private float closeSpeedMultiplier = 2f;
    [SerializeField] private Ease openEase = Ease.OutCubic;
    [SerializeField] private Ease closeEase = Ease.InCubic;

    [Header("Options")]
    [SerializeField] private PauseOptionsController pauseOptionsController;
    [SerializeField] private GameManager gameManager;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private Tween pauseTween;
    private Tween optionsTween;
    private bool isPaused;
    private bool isAnimating;
    private Vector2 pauseShownPosition;
    private Vector2 optionsShownPosition;

    private void Awake()
    {
        if (!ValidateWiring())
        {
            enabled = false;
            return;
        }

        pauseShownPosition = pauseMenuPanel.anchoredPosition;
        optionsShownPosition = optionsMenuPanel.anchoredPosition;

        pauseMenuRoot.SetActive(false);
        optionsMenuRoot.SetActive(false);
    }

    private void Update()
    {
        if (gameManager.IsGameOverShown)
        {
            if (isPaused)
                ForceClosePauseForGameOver();
            return;
        }

        if (!Input.GetKeyDown(KeyCode.Escape) || isAnimating)
            return;

        if (!isPaused)
        {
            OpenPauseMenu();
            return;
        }

        if (IsOptionsOpen())
        {
            CloseOptionsMenu();
            return;
        }

        ResumeGame();
    }

    public void OnResumePressed()
    {
        if (!isPaused || isAnimating)
            return;
        ResumeGame();
    }

    public void OnOptionsPressed()
    {
        if (!isPaused || isAnimating || IsOptionsOpen())
            return;
        OpenOptionsMenu();
    }

    public void OnApplyAndCloseOptionsPressed()
    {
        pauseOptionsController.ApplySettings();
        CloseOptionsMenu();
    }

    public void OnExitToMainMenuPressed()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void OpenPauseMenu()
    {
        isPaused = true;
        isAnimating = true;
        Time.timeScale = 0f;
        UnlockCursorForMenu();

        float width = GetPanelWidth(pauseMenuPanel);
        pauseMenuRoot.SetActive(true);
        pauseMenuPanel.anchoredPosition = pauseShownPosition + Vector2.left * width;

        KillTween(ref pauseTween);
        pauseTween = pauseMenuPanel
            .DOAnchorPos(pauseShownPosition, openDuration)
            .SetEase(openEase)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                isAnimating = false;
                pauseTween = null;
            });
    }

    private void ResumeGame()
    {
        isAnimating = true;

        if (IsOptionsOpen())
            CloseOptionsMenuImmediate();

        float width = GetPanelWidth(pauseMenuPanel);
        float duration = GetCloseDuration();

        KillTween(ref pauseTween);
        pauseTween = pauseMenuPanel
            .DOAnchorPos(pauseShownPosition + Vector2.left * width, duration)
            .SetEase(closeEase)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                pauseMenuRoot.SetActive(false);
                Time.timeScale = 1f;
                isPaused = false;
                isAnimating = false;
                LockCursorForGameplay();
                pauseTween = null;
            });
    }

    private void OpenOptionsMenu()
    {
        isAnimating = true;
        float width = GetPanelWidth(optionsMenuPanel);
        optionsMenuRoot.SetActive(true);
        optionsMenuPanel.anchoredPosition = optionsShownPosition + Vector2.left * width;

        KillTween(ref optionsTween);
        optionsTween = optionsMenuPanel
            .DOAnchorPos(optionsShownPosition, openDuration)
            .SetEase(openEase)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                isAnimating = false;
                optionsTween = null;
            });
    }

    private void CloseOptionsMenu()
    {
        if (!IsOptionsOpen())
            return;

        isAnimating = true;
        float width = GetPanelWidth(optionsMenuPanel);
        float duration = GetCloseDuration();

        KillTween(ref optionsTween);
        optionsTween = optionsMenuPanel
            .DOAnchorPos(optionsShownPosition + Vector2.left * width, duration)
            .SetEase(closeEase)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                optionsMenuRoot.SetActive(false);
                isAnimating = false;
                optionsTween = null;
            });
    }

    private void CloseOptionsMenuImmediate()
    {
        float width = GetPanelWidth(optionsMenuPanel);
        optionsMenuPanel.anchoredPosition = optionsShownPosition + Vector2.left * width;
        optionsMenuRoot.SetActive(false);
        KillTween(ref optionsTween);
    }

    private bool IsOptionsOpen()
    {
        return optionsMenuRoot.activeSelf;
    }

    private float GetPanelWidth(RectTransform panel)
    {
        if (panel == null)
            return 0f;
        Canvas.ForceUpdateCanvases();
        return Mathf.Abs(panel.rect.width);
    }

    private float GetCloseDuration()
    {
        return openDuration / Mathf.Max(1f, closeSpeedMultiplier);
    }

    private static void KillTween(ref Tween tween)
    {
        if (tween == null)
            return;
        tween.Kill();
        tween = null;
    }

    private static void UnlockCursorForMenu()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private static void LockCursorForGameplay()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ForceClosePauseForGameOver()
    {
        KillTween(ref pauseTween);
        KillTween(ref optionsTween);
        pauseMenuRoot.SetActive(false);
        optionsMenuRoot.SetActive(false);

        isPaused = false;
        isAnimating = false;
    }

    private bool ValidateWiring()
    {
        bool ok = true;
        if (pauseMenuRoot == null)
        {
            Debug.LogError("[PauseMenuController] Falta referencia: pauseMenuRoot.", this);
            ok = false;
        }
        if (pauseMenuPanel == null)
        {
            Debug.LogError("[PauseMenuController] Falta referencia: pauseMenuPanel.", this);
            ok = false;
        }
        if (optionsMenuRoot == null)
        {
            Debug.LogError("[PauseMenuController] Falta referencia: optionsMenuRoot.", this);
            ok = false;
        }
        if (optionsMenuPanel == null)
        {
            Debug.LogError("[PauseMenuController] Falta referencia: optionsMenuPanel.", this);
            ok = false;
        }
        if (pauseOptionsController == null)
        {
            Debug.LogError("[PauseMenuController] Falta referencia: pauseOptionsController.", this);
            ok = false;
        }
        if (gameManager == null)
        {
            Debug.LogError("[PauseMenuController] Falta referencia: gameManager.", this);
            ok = false;
        }
        return ok;
    }
}
