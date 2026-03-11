using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Interactable))]
[AddComponentMenu("HyperManzana/Items/Item Pickup")]
public class ItemPickup : MonoBehaviour
{
    [SerializeField] private ItemData itemData;
    [SerializeField] private PlayerItemInventory playerInventory;

    public void OnPickedUp()
    {
        if (itemData == null)
        {
            Debug.LogWarning("[ItemPickup] itemData no asignado.", this);
            return;
        }

        if (playerInventory == null)
            playerInventory = FindFirstObjectByType<PlayerItemInventory>();
        if (playerInventory == null)
        {
            Debug.LogWarning("[ItemPickup] No se encontró PlayerItemInventory.", this);
            return;
        }

        playerInventory.PickupItem(itemData, transform);

        PlayerInteractor interactor = FindFirstObjectByType<PlayerInteractor>();
        if (interactor != null)
            interactor.ClearCurrentInteractionPrompt();

        Destroy(gameObject);
    }

}
