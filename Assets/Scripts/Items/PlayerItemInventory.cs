using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[AddComponentMenu("HyperSalchicha/Items/Player Item Inventory")]
public class PlayerItemInventory : MonoBehaviour
{
    [Header("Runtime")]
    [SerializeField] private ItemData currentItem;

    [Header("UI")]
    [SerializeField] private Image itemImage;

    public ItemData CurrentItem => currentItem;

    private void Awake()
    {
        RefreshItemUI();
    }

    public void PickupItem(ItemData newItem, Transform pickupTransform)
    {
        if (newItem == null)
            return;

        if (currentItem != null && currentItem.dropPrefab != null && pickupTransform != null)
        {
            Instantiate(currentItem.dropPrefab, pickupTransform.position, pickupTransform.rotation);
        }

        currentItem = newItem;
        RefreshItemUI();
    }

    public void ConsumeItem()
    {
        currentItem = null;
        RefreshItemUI();
    }

    public bool HasItem(ItemData item)
    {
        return item != null && currentItem == item;
    }

    private void RefreshItemUI()
    {
        if (itemImage == null)
            return;

        if (currentItem == null || currentItem.itemIcon == null)
        {
            itemImage.sprite = null;
            itemImage.enabled = false;
            return;
        }

        itemImage.sprite = currentItem.itemIcon;
        itemImage.enabled = true;
    }
}
