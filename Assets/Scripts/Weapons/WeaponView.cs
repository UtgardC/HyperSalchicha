using UnityEngine;

namespace HyperManzana.Weapons
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HyperManzana/Weapons/Weapon View")]
    public class WeaponView : MonoBehaviour
    {
        [SerializeField] private Transform firePoint;
        [SerializeField] private Animator animator;
        [SerializeField] private WeaponVisibleBullets visibleBullets;
        [SerializeField] private WeaponEnhancementVisuals enhancementVisuals;

        public Transform FirePoint => firePoint != null ? firePoint : transform;
        public Animator Animator => animator;
        public WeaponVisibleBullets VisibleBullets => visibleBullets;
        public WeaponEnhancementVisuals EnhancementVisuals => enhancementVisuals;

        public void CacheMissingReferences()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);
            if (visibleBullets == null)
                visibleBullets = GetComponentInChildren<WeaponVisibleBullets>(true);
            if (enhancementVisuals == null)
                enhancementVisuals = GetComponentInChildren<WeaponEnhancementVisuals>(true);
            if (firePoint == null)
                firePoint = transform;
        }

        private void Reset()
        {
            CacheMissingReferences();
        }
    }
}
