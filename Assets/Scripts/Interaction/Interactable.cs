using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    [TextArea] public string promptText = "Pulsa E para interactuar";
    [SerializeField] private UnityEvent onInteract = new UnityEvent();
    [Header("Coste")]
    [SerializeField] private int price = 0;
    [Tooltip("0 = infinitas, >0 = cantidad máxima de interacciones")]
    [SerializeField] private int maxUses = 0;

    private int usedCount = 0;

    public string Prompt => promptText;
    public bool CanUse => maxUses == 0 || usedCount < maxUses;
    public int Price => price;

    public void Interact()
    {
        if (!CanUse) return;
        onInteract.Invoke();
        usedCount++;
    }
}
