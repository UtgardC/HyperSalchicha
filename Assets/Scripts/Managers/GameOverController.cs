using UnityEngine;
using UnityEngine.SceneManagement;

namespace HyperManzana.Managers
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HyperManzana/Managers/Game Over Controller")]
    public class GameOverController : MonoBehaviour
    {
        public static GameOverController Instance { get; private set; }

        [Header("Screen")]
        [SerializeField] private GameObject gameOverScreen;

        private bool shown;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (gameOverScreen != null) gameOverScreen.SetActive(false);
        }

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
}

