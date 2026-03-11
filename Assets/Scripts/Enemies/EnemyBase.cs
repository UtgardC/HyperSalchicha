using System;
using System.Collections;
using HyperSalchicha.UI;
using UnityEngine;
using UnityEngine.AI;

namespace HyperSalchicha.Enemies
{
    [DisallowMultipleComponent]
    public class EnemyBase : MonoBehaviour
    {
        [Header("Enemy Stats")]
        [SerializeField] protected float maxHealth = 100f;
        protected float currentHealth;
        [SerializeField] private int cuajosRewardOnKill = 50;

        [Header("UI References")]
        [SerializeField] private UIBarFill healthBar;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private string deathBool = "Muerto";
        [SerializeField] private string movingBool = "Moving";
        [SerializeField] private string attackTrigger = "Attack";
        [SerializeField] private string spawnTrigger = "Spawn";

        [Header("Death Cleanup")]
        [SerializeField] private bool destroyOnDeath = true;
        [SerializeField] private float deathDestroyDelay = 3f;

        private EnemyPool ownerPool;
        private SpawnPoint lastSpawnPoint;
        private EnemyType enemyType = EnemyType.Normal;
        private bool startsRunning;
        private bool wasRecycled;
        private int spawnRound;
        private bool isDead;
        private float runtimeMaxHealth;
        private Coroutine deathRoutine;
        private NavMeshAgent navMeshAgent;
        private EnemyNavChase navChase;
        private EnemyMeleeAttack meleeAttack;
        private EnemyFlyerPlaceholder flyerPlaceholder;
        private EnemyBrutePlaceholder brutePlaceholder;

        public event Action<EnemyBase, float> OnDamaged;
        public event Action<EnemyBase> OnKilled;
        public event Action<EnemyBase, bool> OnReturnedToPool;
        public event Action<EnemyBase, EnemySpawnData> OnInitialized;

        public EnemyType Type => enemyType;
        public bool IsDead => isDead;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => runtimeMaxHealth > 0f ? runtimeMaxHealth : Mathf.Max(1f, maxHealth);
        public float BaseMaxHealth => Mathf.Max(1f, maxHealth);
        public bool StartsRunning => startsRunning;
        public bool WasRecycled => wasRecycled;
        public int SpawnRound => spawnRound;
        public int CuajosRewardOnKill => Mathf.Max(0, cuajosRewardOnKill);
        public Animator Animator => animator;
        public string MovingBoolParameter => movingBool;
        public string AttackTriggerParameter => attackTrigger;
        public SpawnPoint CurrentSpawnPoint => lastSpawnPoint;

        protected virtual void Awake()
        {
            CacheReferences();
            runtimeMaxHealth = Mathf.Max(1f, maxHealth);
            currentHealth = runtimeMaxHealth;
            SyncHealthBar();
        }

        public virtual void Initialize(EnemySpawnData data, EnemyPool pool, SpawnPoint spawnPoint)
        {
            CacheReferences();
            StopDeathRoutine();

            ownerPool = pool;
            lastSpawnPoint = spawnPoint;
            enemyType = data.type;
            spawnRound = Mathf.Max(1, data.round);
            startsRunning = data.startsRunning;
            wasRecycled = data.isRecycled;
            runtimeMaxHealth = data.maxHealth > 0f ? data.maxHealth : Mathf.Max(1f, maxHealth);
            currentHealth = Mathf.Clamp(
                data.currentHealth > 0f ? data.currentHealth : runtimeMaxHealth,
                0f,
                runtimeMaxHealth);
            isDead = false;

            ResetAnimatorState();
            EnableCombatComponents(true);
            ConfigureTypeBehaviours();
            SyncHealthBar();

            OnInitialized?.Invoke(this, data);
        }

        public virtual void TakeDamage(float damageAmount)
        {
            if (isDead || damageAmount <= 0f)
                return;

            currentHealth = Mathf.Max(0f, currentHealth - damageAmount);
            SyncHealthBar();
            OnDamaged?.Invoke(this, damageAmount);

            if (currentHealth <= 0f)
                Die();
        }

        public virtual void Die()
        {
            if (isDead)
                return;

            isDead = true;
            EnableCombatComponents(false);
            SetAnimatorDeathState(true);
            RewardKillCuajos();
            OnKilled?.Invoke(this);

            float delay = Mathf.Max(0f, deathDestroyDelay);
            if (delay <= 0f)
            {
                ReturnToPool(false);
                return;
            }

            deathRoutine = StartCoroutine(Co_ReturnAfterDelay(delay));
        }

        public virtual void ReturnToPool(bool recycled)
        {
            StopDeathRoutine();
            OnReturnedToPool?.Invoke(this, recycled);

            if (ownerPool != null)
            {
                ownerPool.Release(this);
                return;
            }

            if (!recycled && destroyOnDeath)
                Destroy(gameObject);
            else
                gameObject.SetActive(false);
        }

        public virtual EnemyRecycleState CaptureRecycleState()
        {
            return new EnemyRecycleState
            {
                type = enemyType,
                round = spawnRound,
                maxHealth = MaxHealth,
                currentHealth = Mathf.Clamp(currentHealth, 0f, MaxHealth),
                startsRunning = startsRunning,
                isRecycled = true,
            };
        }

        public virtual void ApplyRecycleState(EnemyRecycleState state)
        {
            enemyType = state.type;
            spawnRound = Mathf.Max(1, state.round);
            startsRunning = state.startsRunning;
            wasRecycled = state.isRecycled;
            runtimeMaxHealth = state.maxHealth > 0f ? state.maxHealth : Mathf.Max(1f, maxHealth);
            currentHealth = Mathf.Clamp(state.currentHealth, 0f, runtimeMaxHealth);
            isDead = false;
            ConfigureTypeBehaviours();
            SyncHealthBar();
        }

        public bool TryGetNavMeshAgent(out NavMeshAgent agent)
        {
            if (navMeshAgent == null)
                navMeshAgent = GetComponent<NavMeshAgent>();

            agent = navMeshAgent;
            return agent != null;
        }

        public Transform ResolveTarget()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            return player != null ? player.transform : null;
        }

        protected virtual void CacheReferences()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);
            if (navMeshAgent == null)
                navMeshAgent = GetComponent<NavMeshAgent>();
            if (navChase == null)
                navChase = GetComponent<EnemyNavChase>();
            if (meleeAttack == null)
                meleeAttack = GetComponent<EnemyMeleeAttack>();
            if (flyerPlaceholder == null)
                flyerPlaceholder = GetComponent<EnemyFlyerPlaceholder>();
            if (brutePlaceholder == null)
                brutePlaceholder = GetComponent<EnemyBrutePlaceholder>();
        }

        private void ConfigureTypeBehaviours()
        {
            bool useNormalChase = enemyType == EnemyType.Normal;
            bool useFlyer = enemyType == EnemyType.Flyer;
            bool useBrute = enemyType == EnemyType.Brute;

            if (navChase != null)
            {
                navChase.enabled = useNormalChase;
                if (useNormalChase)
                    navChase.SetRunning(startsRunning);
            }

            if (meleeAttack != null)
                meleeAttack.enabled = useNormalChase;

            if (flyerPlaceholder != null)
                flyerPlaceholder.enabled = useFlyer;

            if (brutePlaceholder != null)
                brutePlaceholder.enabled = useBrute;

            if (navMeshAgent != null)
            {
                bool usesAgent = useNormalChase || useBrute;
                if (navMeshAgent.enabled != usesAgent)
                    navMeshAgent.enabled = usesAgent;

                if (navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
                {
                    navMeshAgent.isStopped = false;
                    navMeshAgent.ResetPath();
                }
            }
        }

        private void EnableCombatComponents(bool enabledState)
        {
            if (navChase != null)
                navChase.enabled = enabledState && enemyType == EnemyType.Normal;
            if (meleeAttack != null)
                meleeAttack.enabled = enabledState && enemyType == EnemyType.Normal;
            if (flyerPlaceholder != null)
                flyerPlaceholder.enabled = enabledState && enemyType == EnemyType.Flyer;
            if (brutePlaceholder != null)
                brutePlaceholder.enabled = enabledState && enemyType == EnemyType.Brute;

            if (navMeshAgent != null)
            {
                bool shouldUseAgent = enabledState && (enemyType == EnemyType.Normal || enemyType == EnemyType.Brute);
                navMeshAgent.enabled = shouldUseAgent;
                if (navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
                {
                    navMeshAgent.isStopped = false;
                    navMeshAgent.ResetPath();
                }
            }
        }

        private void ResetAnimatorState()
        {
            if (animator == null)
                return;

            if (!string.IsNullOrEmpty(deathBool))
                animator.SetBool(deathBool, false);
            if (!string.IsNullOrEmpty(movingBool))
                animator.SetBool(movingBool, false);
            if (!string.IsNullOrEmpty(spawnTrigger))
                animator.SetTrigger(spawnTrigger);
        }

        private void SetAnimatorDeathState(bool value)
        {
            if (animator == null || string.IsNullOrEmpty(deathBool))
                return;

            animator.SetBool(deathBool, value);
        }

        private void SyncHealthBar()
        {
            if (healthBar == null)
                return;

            if (healthBar.gameObject.activeSelf == isDead)
                healthBar.gameObject.SetActive(!isDead);

            if (!isDead)
                healthBar.Set(currentHealth, MaxHealth);
        }

        private IEnumerator Co_ReturnAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            ReturnToPool(false);
        }

        private void StopDeathRoutine()
        {
            if (deathRoutine == null)
                return;

            StopCoroutine(deathRoutine);
            deathRoutine = null;
        }

        private void RewardKillCuajos()
        {
            if (CuajosRewardOnKill <= 0)
                return;

            GameManager.Instance?.AddCuajos(CuajosRewardOnKill);
        }
    }
}
