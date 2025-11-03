using UnityEngine;
using HyperManzana.Player;

namespace HyperManzana.Enemies
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HyperManzana/Enemies/Melee Attack (Sphere)")]
    public class EnemyMeleeAttack : MonoBehaviour
    {
        [Header("Attack")]
        public float damage = 10f;
        public float cooldown = 1.0f;
        public float range = 1.5f;   // offset hacia adelante
        public float radius = 0.75f; // radio de la esfera
        public LayerMask playerLayer; // asignar Layer "Player"
        public Transform origin;      // opcional; por defecto el transform propio

        [Tooltip("Tiempo de preparación antes de aplicar el daño tras detectar al jugador en rango.")]
        public float windup = 0.3f;

        private float nextTime;
        private Coroutine attackRoutine;
        [Header("Animation (opcional)")]
        public Animator animator; // Asignar el Animator del enemigo si quieres disparar trigger Attack
        public string attackTrigger = "Attack";
        private EnemyScript enemyScript;

        private Transform Origin => origin != null ? origin : transform;

        private void Awake()
        {
            enemyScript = GetComponent<EnemyScript>();
        }

        private void Update()
        {
            if (Time.time < nextTime) return;
            if (enemyScript != null && enemyScript.IsDead) return;

            // Detectar si el jugador está en rango; si lo está, iniciar la preparación del ataque
            Vector3 center = Origin.position + Origin.forward * range;
            var hits = Physics.OverlapSphere(center, radius, playerLayer, QueryTriggerInteraction.Ignore);
            if (hits != null && hits.Length > 0)
            {
                if (attackRoutine == null)
                {
                    if (animator != null && !string.IsNullOrEmpty(attackTrigger))
                    {
                        animator.SetTrigger(attackTrigger);
                    }
                    attackRoutine = StartCoroutine(Co_AttackAfterWindup());
                }
            }
        }

        private System.Collections.IEnumerator Co_AttackAfterWindup()
        {
            float tEnd = Time.time + Mathf.Max(0f, windup);
            while (Time.time < tEnd)
            {
                if (enemyScript != null && enemyScript.IsDead)
                {
                    attackRoutine = null;
                    yield break;
                }
                yield return null;
            }

            // Tras el windup, aplicar daño solo si el jugador sigue en rango
            Vector3 center = Origin.position + Origin.forward * range;
            var hits = Physics.OverlapSphere(center, radius, playerLayer, QueryTriggerInteraction.Ignore);
            if (hits != null && hits.Length > 0)
            {
                foreach (var col in hits)
                {
                    var stats = col.GetComponentInParent<PlayerStats>() ?? col.GetComponent<PlayerStats>();
                    if (stats != null)
                    {
                        stats.TakeDamage(damage);
                        nextTime = Time.time + cooldown; // ventana de enfriamiento
                        break;
                    }
                }
            }

            attackRoutine = null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Transform o = origin != null ? origin : transform;
            Vector3 center = o.position + o.forward * range;
            Gizmos.DrawWireSphere(center, radius);
            Gizmos.DrawLine(o.position, center);
        }
    }
}
