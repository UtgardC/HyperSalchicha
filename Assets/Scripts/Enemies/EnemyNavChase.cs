using UnityEngine;
using UnityEngine.AI;

namespace HyperSalchicha.Enemies
{
    [RequireComponent(typeof(NavMeshAgent))]
    [DisallowMultipleComponent]
    [AddComponentMenu("HyperSalchicha/Enemies/NavMesh Chase (Simple)")]
    public class EnemyNavChase : MonoBehaviour
    {
        [Header("Target")]
        public Transform target; // Asignar el Player en el Inspector

        [Header("Agent Settings")]
        public float speed = 3.5f;
        public float runningSpeedMultiplier = 1.35f;
        public bool useOffMeshLinks = true; // Permite saltos/links si existen en el NavMesh

        private NavMeshAgent agent;
        [Header("Animation (opcional)")]
        public Animator animator; // Asignar el Animator del enemigo si quieres el bool Moving
        public string movingBool = "Moving";
        private EnemyBase enemyBase;
        private bool isRunning;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            enemyBase = GetComponent<EnemyBase>();
            ApplyAgentSettings();
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
            if (enemyBase != null && enemyBase.IsDead) return;
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
            if (agent == null)
                return;

            agent.speed = isRunning ? speed * Mathf.Max(1f, runningSpeedMultiplier) : speed;
            agent.autoTraverseOffMeshLink = useOffMeshLinks;
            agent.autoRepath = true;
        }

        public void SetRunning(bool running)
        {
            isRunning = running;
            ApplyAgentSettings();
        }
    }
}
