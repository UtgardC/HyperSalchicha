using UnityEngine;

[CreateAssetMenu(menuName = "HyperManzana/Items/Item Data", fileName = "ItemData")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite itemIcon;
    public GameObject dropPrefab;
}
