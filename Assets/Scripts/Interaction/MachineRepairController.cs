using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class RepairPart
{
    public ItemData requiredItem;
    public GameObject visualProp;
    [HideInInspector] public bool isDelivered;
}

[DisallowMultipleComponent]
[RequireComponent(typeof(Interactable))]
[AddComponentMenu("HyperSalchicha/Interaction/Machine Repair Controller")]
public class MachineRepairController : MonoBehaviour
{
    [Header("Repair Parts")]
    [SerializeField] private List<RepairPart> requiredParts = new List<RepairPart>();

    [Header("Prompt Overrides")]
    [SerializeField] private string noItemPromptText = "Necesitas piezas adicionales";
    [SerializeField] private string invalidItemPromptText = "Pieza inválida";

    [Header("Events")]
    [SerializeField] private UnityEvent onPartDelivered = new UnityEvent();
    [SerializeField] private UnityEvent onRepairConditionMet = new UnityEvent();
    [SerializeField] private UnityEvent onRepairSequenceFinished = new UnityEvent();

    [SerializeField] private PlayerItemInventory playerInventory;

    private Interactable interactable;
    private string defaultPromptText;

    private void Awake()
    {
        interactable = GetComponent<Interactable>();
        defaultPromptText = interactable != null ? interactable.promptText : string.Empty;
        RefreshVisualProps();
        RefreshPrompt();

        if (AreAllPartsDelivered() && interactable != null)
            interactable.enabled = false;
    }

    private void Update()
    {
        RefreshPrompt();
    }

    public void TryDeliverItem()
    {
        if (requiredParts == null || requiredParts.Count == 0)
            return;

        if (playerInventory == null)
            playerInventory = FindFirstObjectByType<PlayerItemInventory>();
        if (playerInventory == null)
        {
            Debug.LogWarning("[MachineRepairController] No se encontro PlayerItemInventory.", this);
            return;
        }

        ItemData currentItem = playerInventory.CurrentItem;
        if (currentItem == null)
            return;

        for (int i = 0; i < requiredParts.Count; i++)
        {
            RepairPart part = requiredParts[i];
            if (part == null || part.isDelivered || part.requiredItem != currentItem)
                continue;

            part.isDelivered = true;
            playerInventory.ConsumeItem();

            if (part.visualProp != null)
                part.visualProp.SetActive(true);

            onPartDelivered.Invoke();
            RefreshPrompt();

            if (AreAllPartsDelivered())
            {
                if (interactable == null)
                    interactable = GetComponent<Interactable>();

                if (interactable != null)
                    interactable.enabled = false;

                onRepairConditionMet.Invoke();
            }

            return;
        }
    }

    public void CompleteRepairSequence()
    {
        onRepairSequenceFinished.Invoke();
    }

    private bool AreAllPartsDelivered()
    {
        if (requiredParts == null || requiredParts.Count == 0)
            return false;

        for (int i = 0; i < requiredParts.Count; i++)
        {
            RepairPart part = requiredParts[i];
            if (part == null || !part.isDelivered)
                return false;
        }

        return true;
    }

    private void RefreshVisualProps()
    {
        if (requiredParts == null)
            return;

        for (int i = 0; i < requiredParts.Count; i++)
        {
            RepairPart part = requiredParts[i];
            if (part?.visualProp != null)
                part.visualProp.SetActive(part.isDelivered);
        }
    }

    private void RefreshPrompt()
    {
        if (interactable == null || !interactable.enabled)
            return;

        interactable.promptText = BuildPromptText();
    }

    private string BuildPromptText()
    {
        if (requiredParts == null || requiredParts.Count == 0)
            return defaultPromptText;

        PlayerItemInventory inventory = ResolveInventory();
        ItemData currentItem = inventory != null ? inventory.CurrentItem : null;
        if (currentItem == null)
            return noItemPromptText;

        return HasMatchingUndeliveredPart(currentItem) ? defaultPromptText : invalidItemPromptText;
    }

    private bool HasMatchingUndeliveredPart(ItemData item)
    {
        if (item == null || requiredParts == null)
            return false;

        for (int i = 0; i < requiredParts.Count; i++)
        {
            RepairPart part = requiredParts[i];
            if (part != null && !part.isDelivered && part.requiredItem == item)
                return true;
        }

        return false;
    }

    private PlayerItemInventory ResolveInventory()
    {
        if (playerInventory == null)
            playerInventory = FindFirstObjectByType<PlayerItemInventory>();

        return playerInventory;
    }
}
