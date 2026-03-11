using UnityEngine;
using HyperSalchicha.Weapons;

[DisallowMultipleComponent]
[AddComponentMenu("HyperSalchicha/UI/Player Weapons UI Bridge")]
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
            inGameUIManager.UpdateAmmoDisplay(0, 0, false);
            inGameUIManager.UpdateWeaponNameDisplay(string.Empty);
            return;
        }

        string displayName = weapon.Definition != null ? weapon.Definition.displayName : "Weapon";
        inGameUIManager.UpdateAmmoDisplay(weapon.CurrentAmmo, weapon.ReserveAmmo, weapon.HasInfiniteAmmoSupply);
        inGameUIManager.UpdateWeaponNameDisplay(displayName);
    }

    private void HandleAmmoChanged(int magazine, int reserve)
    {
        WeaponController currentWeapon = weaponManager != null ? weaponManager.CurrentWeapon : null;
        bool reserveIsInfinite = currentWeapon != null && currentWeapon.HasInfiniteAmmoSupply;
        inGameUIManager.UpdateAmmoDisplay(magazine, reserve, reserveIsInfinite);
    }
}
