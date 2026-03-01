using UnityEngine;

namespace HyperManzana.Weapons
{
    [CreateAssetMenu(fileName = "WeaponDefinition", menuName = "HyperManzana/Weapons/Weapon Definition")]
    public class WeaponDefinition : ScriptableObject
    {
        [System.Serializable]
        public class EnhancementVisualOverrides
        {
            [Header("Materials")]
            public Material quantumMaterial;
            public Material quantumOverheatedMaterial;
            public Material heatedMaterial;

            [Header("GameObjects")]
            public GameObject overclockDialBase;
            public GameObject heatedSmokeVfx;
            public GameObject overclockHeatedAddon;

            [Header("Dial Animator (optional)")]
            public Animator overclockDialAnimator;
            public string quantumDialBool = "QuantumMode";
        }

        [Header("Identity")]
        public string displayName = "Weapon";
        public GameObject weaponPrefab;

        [Header("Fire")]
        public WeaponFireMode fireMode = WeaponFireMode.Hitscan;
        public float damagePerShot = 10f;
        [Tooltip("Minimum time between shots in seconds.")]
        public float fireRateSeconds = 0.15f;
        public bool isAutomatic;
        public float raycastDistance = 150f;
        public GameObject projectilePrefab;
        public float projectileSpeed = 40f;

        [Header("Ammo")]
        public int magazineCapacity = 10;
        public int startingMagazineAmmo = 10;
        public int startingReserveAmmo = 30;
        public bool infiniteMagazine;
        public bool infiniteReserve;

        [Header("Reload")]
        public WeaponReloadMode reloadMode = WeaponReloadMode.Magazine;
        [Tooltip("Used only in shell-by-shell mode.")]
        public int reloadStepAmount = 1;

        [Header("Enhancement Visual Overrides (optional)")]
        public bool useEnhancementVisualOverrides;
        public EnhancementVisualOverrides enhancementVisuals = new EnhancementVisualOverrides();
    }
}
