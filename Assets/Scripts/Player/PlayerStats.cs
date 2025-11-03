using UnityEngine;
using UnityEngine.Events;
using HyperManzana.Managers;

namespace HyperManzana.Player
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HyperManzana/Player/Player Stats")]
    public class PlayerStats : MonoBehaviour
    {
        [System.Serializable]
        public class FloatFloatEvent : UnityEvent<float, float> { }

        [Header("Salud")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float health = 100f;

        [Header("Eventos")] 
        [SerializeField] private FloatFloatEvent onHealthChanged = new FloatFloatEvent();
        [SerializeField] private UnityEvent onDeath = new UnityEvent();

        [Header("(Opcional) UI Bars")] 
        [SerializeField] private UI.UIBarFill healthBar;

        public float MaxHealth => maxHealth;
        public float Health => health;

        public FloatFloatEvent OnHealthChanged => onHealthChanged;
        public UnityEvent OnDeath => onDeath;

        private bool isDead;

        private void Awake()
        {
            ClampAll();
        }

        private void Start()
        {
            UpdateUIImmediate();
        }

        private void OnValidate()
        {
            ClampAll();
            UpdateUIImmediate();
        }

        private void ClampAll()
        {
            if (maxHealth < 1f) maxHealth = 1f;
            health = Mathf.Clamp(health, 0f, maxHealth);
        }

        private void UpdateUIImmediate()
        {
            if (healthBar != null)
                healthBar.Set(health, maxHealth);
        }

        // Salud
        public void TakeDamage(float amount)
        {
            if (isDead) return;
            if (amount <= 0f) return;
            health = Mathf.Max(0f, health - amount);
            onHealthChanged.Invoke(health, maxHealth);
            if (healthBar != null) healthBar.Set(health, maxHealth);
            if (health <= 0f)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (amount <= 0f) return;
            if (isDead) return;
            health = Mathf.Min(maxHealth, health + amount);
            onHealthChanged.Invoke(health, maxHealth);
            if (healthBar != null) healthBar.Set(health, maxHealth);
        }

        public void SetHealth(float value)
        {
            health = Mathf.Clamp(value, 0f, maxHealth);
            onHealthChanged.Invoke(health, maxHealth);
            if (healthBar != null) healthBar.Set(health, maxHealth);
            if (!isDead && health <= 0f) Die();
        }

        public void SetMaxHealth(float newMax, bool keepPercent = true)
        {
            newMax = Mathf.Max(1f, newMax);
            if (keepPercent)
            {
                float percent = maxHealth > 0f ? health / maxHealth : 0f;
                maxHealth = newMax;
                health = Mathf.Clamp(percent * maxHealth, 0f, maxHealth);
            }
            else
            {
                maxHealth = newMax;
                health = Mathf.Clamp(health, 0f, maxHealth);
            }
            onHealthChanged.Invoke(health, maxHealth);
            if (healthBar != null) healthBar.Set(health, maxHealth);
        }


        private void Die()
        {
            if (isDead) return;
            isDead = true;
            Debug.Log("moriste");
            onDeath.Invoke();
            // Mostrar Game Over si hay controlador en la escena
            GameOverController.Instance?.ShowGameOver();
        }
    }
}
