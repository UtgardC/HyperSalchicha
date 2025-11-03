using UnityEngine;
using UnityEngine.AI;
using HyperManzana.UI;

public class EnemyScript : MonoBehaviour
{
    [Header("Enemy Stats")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("UI References")]
    [SerializeField] private UIBarFill healthBar;

    [Header("Animation")]
    [SerializeField] private Animator animator; // Asignar el Animator del modelo del enemigo
    [SerializeField] private string deathBool = "Muerto";
    [SerializeField] private string movingBool = "Moving"; // lo manipula EnemyNavChase
    [SerializeField] private string attackTrigger = "Attack"; // lo dispara EnemyMeleeAttack

    [Header("Death Cleanup")]
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private float deathDestroyDelay = 3f;

    private bool isDead;
    public bool IsDead => isDead;

    // Start is called before the first frame update
    void Start()
    {
        currentHealth = maxHealth;
        
        // Initialize custom bar fill (container/fill must be assigned in Inspector)
        if (healthBar != null)
        {
            healthBar.Set(currentHealth, maxHealth);
        }
        // Animator opcional
        // Se asume asignado por Inspector; si está vacío, intentamos tomar del hijo
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    // Public method to take damage
    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;
        
        // Update custom bar
        if (healthBar != null)
        {
            healthBar.Set(currentHealth, maxHealth);
        }

        Debug.Log("Enemy took " + damageAmount + " damage. Current Health: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // Detener navegación y ataques
        var agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;
        var chase = GetComponent<HyperManzana.Enemies.EnemyNavChase>();
        if (chase != null) chase.enabled = false;
        var melee = GetComponent<HyperManzana.Enemies.EnemyMeleeAttack>();
        if (melee != null) melee.enabled = false;

        // Activar animación de muerte
        if (animator != null && !string.IsNullOrEmpty(deathBool))
        {
            animator.SetBool(deathBool, true);
        }

        // Ocultar barra de vida
        if (healthBar != null)
        {
            Destroy(healthBar.gameObject);
        }

        // Opción: destruir después de un delay para liberar el slot del EnemyCounter
        if (destroyOnDeath)
        {
            Destroy(gameObject, Mathf.Max(0f, deathDestroyDelay));
        }
    }
}
