using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

namespace HyperManzana.Managers
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HyperManzana/Managers/Enemy Counter + Victory")]
    public class EnemyCounter : MonoBehaviour
    {
        public static EnemyCounter Instance { get; private set; }

        [Header("UI")]
        [SerializeField] private TMP_Text countText;

        [Header("Victory")]
        [SerializeField] private GameObject victoryScreen;

        [Header("Conteo fijo por padre 'Enemigos'")]
        [SerializeField] private Transform enemiesRoot; // Asigna el GameObject padre con todos los enemigos

        private int enemyCount;
        private bool victoryShown;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (victoryScreen != null)
                victoryScreen.SetActive(false);

            UpdateUI();
        }

        private void Start()
        {
            RecountFromRoot();
            UpdateUI();
        }

        private void Update()
        {
            if (enemiesRoot != null)
            {
                int current = enemiesRoot.childCount;
                if (current != enemyCount)
                {
                    enemyCount = current;
                    UpdateUI();
                }
                if (enemyCount == 0)
                {
                    ActivateVictory();
                }
            }
        }

        private void RecountFromRoot()
        {
            enemyCount = enemiesRoot != null ? enemiesRoot.childCount : 0;
        }

        private void UpdateUI()
        {
            if (countText != null)
            {
                countText.text = enemyCount.ToString();
            }
        }

        private void ActivateVictory()
        {
            if (victoryShown) return;
            victoryShown = true;

            Time.timeScale = 0f;
            if (victoryScreen != null)
                victoryScreen.SetActive(true);

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
