using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    [TextArea] public string promptText = "Pulsa E para interactuar";
    [SerializeField] private UnityEvent onInteract = new UnityEvent();
    [SerializeField] private UnityEvent onInteractionFailed = new UnityEvent();
    [Header("Coste")]
    [SerializeField] private int price = 0;
    [SerializeField] private float interactionCooldown = 0f;
    [Tooltip("0 = infinitas, >0 = cantidad máxima de interacciones")]
    [SerializeField] private int maxUses = 0;

    private int usedCount = 0;
    private float nextInteractionTime = 0f;

    public string Prompt => promptText;
    public bool CanUse =>
        (maxUses == 0 || usedCount < maxUses) &&
        Time.time >= nextInteractionTime;
    public int Price => price;

    public void Interact()
    {
        if (!isActiveAndEnabled || !CanUse) return;

        if (price > 0)
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("[Interactable] GameManager.Instance es null, no se puede validar pago.", this);
                onInteractionFailed.Invoke();
                return;
            }

            if (GameManager.Instance.cuajosActuales < price)
            {
                Debug.Log("No tienes cuajos suficientes.");
                onInteractionFailed.Invoke();
                return;
            }

            GameManager.Instance.SubtractCuajos(price);
        }

        onInteract.Invoke();
        usedCount++;

        float cooldown = Mathf.Max(0f, interactionCooldown);
        if (cooldown > 0f)
            nextInteractionTime = Time.time + cooldown;
    }
}
