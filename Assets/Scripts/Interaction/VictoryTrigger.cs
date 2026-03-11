using UnityEngine;

[DisallowMultipleComponent]
public class VictoryTrigger : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject victoryScreen;

    [Header("Trigger")]
    [SerializeField] private bool winOnPlayerEnter = true;
    [SerializeField] private string playerTag = "Player";

    private bool shown;

    public void WinGame()
    {
        if (shown)
            return;

        shown = true;
        Time.timeScale = 0f;

        if (victoryScreen != null)
            victoryScreen.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!winOnPlayerEnter || shown || other == null)
            return;

        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag))
            return;

        WinGame();
    }
}
