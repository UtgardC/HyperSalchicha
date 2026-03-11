using UnityEngine;
using HyperSalchicha.Managers;

namespace HyperSalchicha.Items
{
    [AddComponentMenu("HyperSalchicha/Items/Power-Up Pickup")]
    [RequireComponent(typeof(Collider))]
    public class PowerUp : MonoBehaviour
    {
        [Header("Power-Up Settings")]
        [Tooltip("The ID of the effect to apply. 0: FireRateBoost, 1: AmmoRefill")]
        [SerializeField] private int effectID = 0;
        
        [Tooltip("How long the effect lasts in seconds.")]
        [SerializeField] private float duration = 10f;
        
        [Tooltip("The strength of the effect (e.g., 2 for 2x fire rate).")]
        [SerializeField] private float multiplier = 2f;

        [Header("Visuals")]
        [SerializeField] private GameObject pickupEffect;
        [SerializeField] private GameObject destroyEffect;

        private void Awake()
        {
            // Ensure the collider is a trigger
            var col = GetComponent<Collider>();
            if (!col.isTrigger)
            {
                Debug.LogWarning($"Collider on {gameObject.name} is not set to 'Is Trigger'. Forcing it.", this);
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // The player should have a specific tag, e.g., "Player"
            if (!other.CompareTag("Player")) return;

            EffectsManager effectsManager = other.GetComponentInChildren<EffectsManager>();
            if (effectsManager == null)
            {
                 effectsManager = other.GetComponentInParent<EffectsManager>();
            }

            if (effectsManager != null)
            {
                // Apply the effect
                effectsManager.ApplyEffect(effectID, duration, multiplier);
                
                // Visual feedback
                if (pickupEffect != null)
                {
                    Instantiate(pickupEffect, transform.position, Quaternion.identity);
                }
                if (destroyEffect != null)
                {
                    Instantiate(destroyEffect, transform.position, Quaternion.identity);
                }

                // Destroy the pickup object
                Destroy(gameObject);
            }
            else
            {
                Debug.LogError($"The object with tag '{other.tag}' does not have an EffectsManager component attached or in its hierarchy.", other.gameObject);
            }
        }
    }
}
