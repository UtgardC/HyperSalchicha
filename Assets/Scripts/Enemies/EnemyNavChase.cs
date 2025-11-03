using UnityEngine;
using UnityEngine.AI;

namespace HyperManzana.Enemies
{
    [RequireComponent(typeof(NavMeshAgent))]
    [DisallowMultipleComponent]
    [AddComponentMenu("HyperManzana/Enemies/NavMesh Chase (Simple)")]
    public class EnemyNavChase : MonoBehaviour
    {
        [Header("Target")]
        public Transform target; // Asignar el Player en el Inspector

        [Header("Agent Settings")]
        public float speed = 3.5f;
        public bool useOffMeshLinks = true; // Permite saltos/links si existen en el NavMesh

        private NavMeshAgent agent;
        [Header("Animation (opcional)")]
        public Animator animator; // Asignar el Animator del enemigo si quieres el bool Moving
        public string movingBool = "Moving";
        private EnemyScript enemyScript;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            ApplyAgentSettings();
            enemyScript = GetComponent<EnemyScript>();
        }

        private void Start()
        {
            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) target = player.transform;
            }
        }

        private void Update()
        {
            if (agent == null || target == null) return;
            if (enemyScript != null && enemyScript.IsDead) return;
            if (!agent.isOnNavMesh) return;
            agent.destination = target.position;

            if (animator != null && !string.IsNullOrEmpty(movingBool))
            {
                // Consideramos "moviéndose" si la velocidad del agente supera un umbral
                bool isMoving = agent.velocity.sqrMagnitude > 0.05f * 0.05f;
                animator.SetBool(movingBool, isMoving);
            }
        }

        private void OnValidate()
        {
            if (agent != null)
            {
                ApplyAgentSettings();
            }
        }

        private void ApplyAgentSettings()
        {
            agent.speed = speed;
            agent.autoTraverseOffMeshLink = useOffMeshLinks;
            agent.autoRepath = true;
        }
    }
}
