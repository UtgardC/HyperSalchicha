using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[AddComponentMenu("HyperManzana/UI/Pause Menu Controller")]
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
        if (pauseMenuRoot != null && pauseMenuPanel == null)
            pauseMenuPanel = pauseMenuRoot.GetComponent<RectTransform>();
        if (optionsMenuRoot != null && optionsMenuPanel == null)
            optionsMenuPanel = optionsMenuRoot.GetComponent<RectTransform>();
        if (pauseOptionsController == null)
            pauseOptionsController = GetComponentInChildren<PauseOptionsController>(true);

        if (pauseMenuPanel != null)
            pauseShownPosition = pauseMenuPanel.anchoredPosition;
        if (optionsMenuPanel != null)
            optionsShownPosition = optionsMenuPanel.anchoredPosition;

        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);
        if (optionsMenuRoot != null)
            optionsMenuRoot.SetActive(false);
    }

    private void Update()
    {
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
        if (pauseOptionsController != null)
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
        if (pauseMenuRoot == null || pauseMenuPanel == null)
            return;

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
        if (pauseMenuRoot == null || pauseMenuPanel == null)
            return;

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
        if (optionsMenuRoot == null || optionsMenuPanel == null)
            return;

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
        if (!IsOptionsOpen() || optionsMenuPanel == null)
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
                if (optionsMenuRoot != null)
                    optionsMenuRoot.SetActive(false);
                isAnimating = false;
                optionsTween = null;
            });
    }

    private void CloseOptionsMenuImmediate()
    {
        if (optionsMenuPanel == null || optionsMenuRoot == null)
            return;

        float width = GetPanelWidth(optionsMenuPanel);
        optionsMenuPanel.anchoredPosition = optionsShownPosition + Vector2.left * width;
        optionsMenuRoot.SetActive(false);
        KillTween(ref optionsTween);
    }

    private bool IsOptionsOpen()
    {
        return optionsMenuRoot != null && optionsMenuRoot.activeSelf;
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
}
