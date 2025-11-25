using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using HyperManzana.Player; // Required for PlayerWeapons
using HyperManzana.Weapons; // Required for PlayerWeapons.Weapon

namespace HyperManzana.Managers
{
    [AddComponentMenu("HyperManzana/Managers/Effects Manager")]
    public class EffectsManager : MonoBehaviour
    {
        // Enum to define the types of effects available
        public enum PowerUpEffect
        {
            FireRateBoost = 0,
            InfiniteAmmo = 1,
            // Add other effects here
        }

        [Header("Setup")]
        [SerializeField] private PlayerWeapons playerWeapons;

        // Private class to store the original state of a weapon
        private class OriginalWeaponStats
        {
            public float originalShotCooldown;
            // Add other stats to save here in the future
        }

        private Dictionary<PlayerWeapons.Weapon, OriginalWeaponStats> originalStats;
        private Coroutine activeFireRateCoroutine;
        private Coroutine activeInfiniteAmmoCoroutine;

        private void Awake()
        {
            if (playerWeapons == null)
            {
                playerWeapons = GetComponentInParent<PlayerWeapons>();
            }
            originalStats = new Dictionary<PlayerWeapons.Weapon, OriginalWeaponStats>();
        }

        /// <summary>
        /// Applies a temporary effect based on an ID.
        /// </summary>
        /// <param name="effectId">The ID of the effect to apply.</param>
        /// <param name="duration">How long the effect should last, in seconds.</param>
        /// <param name="multiplier">A generic multiplier for the effect's strength (e.g., 2 for 2x).</param>
        public void ApplyEffect(int effectId, float duration, float multiplier)
        {
            PowerUpEffect effectType = (PowerUpEffect)effectId;

            switch (effectType)
            {
                case PowerUpEffect.FireRateBoost:
                    if (activeFireRateCoroutine != null)
                    {
                        StopCoroutine(activeFireRateCoroutine);
                    }
                    activeFireRateCoroutine = StartCoroutine(Co_FireRateBoost(duration, multiplier));
                    break;
                case PowerUpEffect.InfiniteAmmo:
                    if (activeInfiniteAmmoCoroutine != null)
                    {
                        StopCoroutine(activeInfiniteAmmoCoroutine);
                    }
                    activeInfiniteAmmoCoroutine = StartCoroutine(Co_InfiniteAmmo(duration));
                    break;
                default:
                    Debug.LogWarning($"Effect with ID {effectId} is not defined.");
                    break;
            }
        }

        private IEnumerator Co_InfiniteAmmo(float duration)
        {
            Debug.Log($"Applying Infinite Ammo for {duration} seconds.");
            
            // Apply effect
            foreach (var weapon in playerWeapons.Weapons)
            {
                weapon.ignoreAmmoConsumption = true;
            }

            yield return new WaitForSeconds(duration);

            // Revert effect
            Debug.Log("Infinite Ammo expired. Reverting to normal.");
            foreach (var weapon in playerWeapons.Weapons)
            {
                weapon.ignoreAmmoConsumption = false;
            }
            
            activeInfiniteAmmoCoroutine = null;
        }

        private IEnumerator Co_FireRateBoost(float duration, float multiplier)
        {
            Debug.Log($"Applying Fire Rate Boost (x{multiplier}) for {duration} seconds.");
            
            // Save original stats and apply effect
            foreach (var weapon in playerWeapons.Weapons)
            {
                if (!originalStats.ContainsKey(weapon))
                {
                    originalStats[weapon] = new OriginalWeaponStats { originalShotCooldown = weapon.shotCooldown };
                }
                weapon.shotCooldown = originalStats[weapon].originalShotCooldown / multiplier;
            }

            yield return new WaitForSeconds(duration);

            // Revert effect
            Debug.Log("Fire Rate Boost expired. Reverting to normal.");
            foreach (var weapon in playerWeapons.Weapons)
            {
                if (originalStats.ContainsKey(weapon))
                {
                    weapon.shotCooldown = originalStats[weapon].originalShotCooldown;
                }
            }
            
            activeFireRateCoroutine = null;
        }
    }
}
