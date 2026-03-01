using System.Collections;
using UnityEngine;
using HyperManzana.Weapons;

namespace HyperManzana.Managers
{
    [AddComponentMenu("HyperManzana/Managers/Effects Manager")]
    public class EffectsManager : MonoBehaviour
    {
        public enum PowerUpEffect
        {
            FireRateBoost = 0,
            InfiniteAmmo = 1
        }

        [Header("Setup")]
        [SerializeField] private WeaponManager weaponManager;

        private Coroutine activeFireRateCoroutine;
        private Coroutine activeInfiniteAmmoCoroutine;

        private void Awake()
        {
            if (weaponManager == null)
                weaponManager = GetComponentInParent<WeaponManager>();
        }

        public void ApplyEffect(int effectId, float duration, float multiplier)
        {
            if (weaponManager == null) return;

            PowerUpEffect effectType = (PowerUpEffect)effectId;
            switch (effectType)
            {
                case PowerUpEffect.FireRateBoost:
                    if (activeFireRateCoroutine != null)
                        StopCoroutine(activeFireRateCoroutine);
                    activeFireRateCoroutine = StartCoroutine(Co_FireRateBoost(duration, multiplier));
                    break;

                case PowerUpEffect.InfiniteAmmo:
                    if (activeInfiniteAmmoCoroutine != null)
                        StopCoroutine(activeInfiniteAmmoCoroutine);
                    activeInfiniteAmmoCoroutine = StartCoroutine(Co_InfiniteAmmo(duration));
                    break;

                default:
                    Debug.LogWarning($"Effect with ID {effectId} is not defined.");
                    break;
            }
        }

        private IEnumerator Co_FireRateBoost(float duration, float multiplier)
        {
            float clamped = Mathf.Max(0.01f, multiplier);
            weaponManager.SetExternalFireRateMultiplier(clamped);

            if (duration > 0f)
                yield return new WaitForSeconds(duration);

            weaponManager.SetExternalFireRateMultiplier(1f);
            activeFireRateCoroutine = null;
        }

        private IEnumerator Co_InfiniteAmmo(float duration)
        {
            weaponManager.SetGlobalInfiniteMagazinePowerup(true);

            if (duration > 0f)
                yield return new WaitForSeconds(duration);

            weaponManager.SetGlobalInfiniteMagazinePowerup(false);
            activeInfiniteAmmoCoroutine = null;
        }
    }
}
