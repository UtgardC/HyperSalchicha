using UnityEngine;
using HyperManzana.Weapons;

[DisallowMultipleComponent]
[AddComponentMenu("HyperManzana/UI/Player Weapons UI Bridge")]
public class PlayerWeaponsUIBridge : MonoBehaviour
{
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private InGameUIManager inGameUIManager;

    private void Awake()
    {
        if (weaponManager == null)
            weaponManager = GetComponentInParent<WeaponManager>();
        if (inGameUIManager == null)
            inGameUIManager = FindAnyObjectByType<InGameUIManager>();
    }

    private void OnEnable()
    {
        if (weaponManager == null || inGameUIManager == null) return;
        weaponManager.OnWeaponChanged += HandleWeaponChanged;
        weaponManager.OnAmmoChanged += HandleAmmoChanged;
    }

    private void OnDisable()
    {
        if (weaponManager == null) return;
        weaponManager.OnWeaponChanged -= HandleWeaponChanged;
        weaponManager.OnAmmoChanged -= HandleAmmoChanged;
    }

    private void HandleWeaponChanged(WeaponController weapon, int slotIndex)
    {
        if (weapon == null)
        {
            inGameUIManager.UpdateAmmoDisplay(0, 0);
            inGameUIManager.UpdateWeaponNameDisplay(string.Empty);
            return;
        }

        string displayName = weapon.Definition != null ? weapon.Definition.displayName : "Weapon";
        inGameUIManager.UpdateAmmoDisplay(weapon.CurrentAmmo, weapon.ReserveAmmo);
        inGameUIManager.UpdateWeaponNameDisplay(displayName);
    }

    private void HandleAmmoChanged(int magazine, int reserve)
    {
        inGameUIManager.UpdateAmmoDisplay(magazine, reserve);
    }
}
