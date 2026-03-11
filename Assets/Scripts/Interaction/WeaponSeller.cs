using UnityEngine;
using HyperSalchicha.Weapons;

[DisallowMultipleComponent]
[AddComponentMenu("HyperSalchicha/Interaction/Weapon Seller")]
public class WeaponSeller : MonoBehaviour
{
    [SerializeField] private int fixedSellValue = 250;
    [SerializeField] private WeaponManager weaponManager;

    public void OnSellInteraction()
    {
        if (weaponManager == null)
            weaponManager = FindFirstObjectByType<WeaponManager>();
        if (weaponManager == null)
        {
            Debug.LogWarning("[WeaponSeller] No se encontró WeaponManager.", this);
            return;
        }

        WeaponDefinition removedWeapon = weaponManager.RemoveCurrentWeapon();
        if (removedWeapon == null)
            return;

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[WeaponSeller] GameManager.Instance es null.", this);
            return;
        }

        GameManager.Instance.AddCuajos(Mathf.Max(0, fixedSellValue));
    }
}
