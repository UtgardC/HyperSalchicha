using UnityEngine;
using TMPro;

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
            if (interactable != null && !interactable.isActiveAndEnabled)
                interactable = null;

            if (interactable != current)
            {
                current = interactable;
            }
        }
        else if (current != null)
        {
            current = null;
        }

        if (currentPromptLabel != null)
            currentPromptLabel.text = current != null ? current.Prompt : string.Empty;

        if (current != null && Input.GetKeyDown(interactKey))
        {
            TryInteract(current);
        }
    }

    private void OnDisable()
    {
        ClearCurrentInteractionPrompt();
    }

    private void TryInteract(Interactable interactable)
    {
        if (interactable == null || !interactable.isActiveAndEnabled || !interactable.CanUse)
            return;

        interactable.Interact();
    }

    public void ClearCurrentInteractionPrompt()
    {
        current = null;
        if (currentPromptLabel != null)
            currentPromptLabel.text = string.Empty;
    }
}
