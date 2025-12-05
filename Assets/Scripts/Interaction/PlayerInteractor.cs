using UnityEngine;
using TMPro;
using HyperManzana.Managers;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private LayerMask interactableMask;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI currentPromptLabel;

    private Interactable current;

    private void Update()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (Physics.Raycast(ray, out var hit, interactRange, interactableMask, QueryTriggerInteraction.Collide))
        {
            var interactable = hit.collider.GetComponent<Interactable>();
            if (interactable != current)
            {
                current = interactable;
                currentPromptLabel.text = current != null ? current.Prompt : string.Empty;
            }
        }
        else if (current != null)
        {
            current = null;
            currentPromptLabel.text = string.Empty;
        }

        if (current != null && Input.GetKeyDown(interactKey))
        {
            TryInteract(current);
        }
    }

    private void OnDisable()
    {
        current = null;
        currentPromptLabel.text = string.Empty;
    }

    private void TryInteract(Interactable interactable)
    {
        if (!interactable.CanUse) return;

        int price = interactable.Price;
        if (price > 0)
        {
            if (GameManager.Instance.cuajosActuales < price)
            {
                Debug.Log("no tienes cuajos suficientes");
                return;
            }

            GameManager.Instance.SubtractCuajos(price);
        }

        interactable.Interact();
    }
}
