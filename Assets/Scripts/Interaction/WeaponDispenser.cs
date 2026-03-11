using UnityEngine;
using HyperManzana.Weapons;

[DisallowMultipleComponent]
[AddComponentMenu("HyperManzana/Interaction/Weapon Dispenser")]
public class WeaponDispenser : MonoBehaviour
{
    [SerializeField] private WeaponDefinition weaponToDispense;
    [SerializeField] private WeaponEnhancementFlags weaponEnhancements = WeaponEnhancementFlags.None;
    [SerializeField] private WeaponManager weaponManager;

    public void OnBuyInteraction()
    {
        if (weaponToDispense == null)
        {
            Debug.LogWarning("[WeaponDispenser] weaponToDispense no asignado.", this);
            return;
        }

        if (weaponManager == null)
            weaponManager = FindFirstObjectByType<WeaponManager>();
        if (weaponManager == null)
        {
            Debug.LogWarning("[WeaponDispenser] No se encontró WeaponManager.", this);
            return;
        }

        weaponManager.EquipNewWeapon(weaponToDispense, weaponEnhancements);
    }
}
