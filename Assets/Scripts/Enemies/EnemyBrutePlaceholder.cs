using HyperSalchicha.Player;
using UnityEngine;
using UnityEngine.AI;

namespace HyperSalchicha.Enemies
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HyperSalchicha/Enemies/Brute Placeholder")]
    public class EnemyBrutePlaceholder : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float chaseSpeed = 4.5f;
        [SerializeField] private float chargeSpeed = 8.5f;
        [SerializeField] private float rotationSpeed = 8f;

        [Header("Charge")]
        [SerializeField] private float chargeStartDistance = 12f;
        [SerializeField] private float chargeDuration = 1.25f;
        [SerializeField] private float chargeCooldown = 4f;
        [SerializeField] private float chargeDamage = 28f;
        [SerializeField] private float chargeHitRadius = 1.4f;
        [SerializeField] private float chargeForwardOffset = 1.2f;

        [Header("Stun")]
        [SerializeField] private float stunWindow = 1.5f;
        [SerializeField] private float stunDamageThresholdPercent = 0.12f;
        [SerializeField] private float stunDuration = 2f;
        [SerializeField] private float postStunImmunity = 1.5f;

        private EnemyBase enemy;
        private NavMeshAgent agent;
        private Transform target;
        private float nextChargeTime;
        private float chargeEndTime;
        private float stunEndTime;
        private float immunityEndTime;
        private float damageWindowEndTime;
        private float accumulatedWindowDamage;
        private bool isCharging;
        private bool isStunned;
        private bool chargeHitApplied;
        private Vector3 chargeTargetPosition;

        private void Awake()
        {
            enemy = GetComponent<EnemyBase>();
            agent = GetComponent<NavMeshAgent>();
        }

        private void OnEnable()
        {
            EnsureSubscriptions();
            ResetState();
        }

        private void OnDisable()
        {
            RemoveSubscriptions();
            ResetMotion();
        }

        private void Update()
        {
            if (enemy == null || enemy.IsDead || enemy.Type != EnemyType.Brute)
                return;

            ResolveTarget();
            if (target == null)
                return;

            if (isStunned)
            {
                if (Time.time >= stunEndTime)
                    ExitStun();
                return;
            }

            if (isCharging)
            {
                UpdateCharge();
                return;
            }

            UpdateChase();
        }

        private void UpdateChase()
        {
            float distance = Vector3.Distance(transform.position, target.position);
            MoveTowards(target.position, chaseSpeed);

            if (Time.time >= nextChargeTime && distance <= chargeStartDistance)
                StartCharge();
        }

        private void StartCharge()
        {
            isCharging = true;
            chargeHitApplied = false;
            chargeTargetPosition = target.position;
            chargeEndTime = Time.time + Mathf.Max(0.2f, chargeDuration);
            nextChargeTime = Time.time + Mathf.Max(0.5f, chargeCooldown);
            MoveTowards(chargeTargetPosition, chargeSpeed);
        }

        private void UpdateCharge()
        {
            MoveTowards(chargeTargetPosition, chargeSpeed);
            TryApplyChargeDamage();

            if (Time.time >= chargeEndTime || Vector3.Distance(transform.position, chargeTargetPosition) <= 1f)
                EndCharge();
        }

        private void EndCharge()
        {
            isCharging = false;
            chargeHitApplied = false;
        }

        private void TryApplyChargeDamage()
        {
            if (chargeHitApplied)
                return;

            Vector3 center = transform.position + transform.forward * chargeForwardOffset;
            Collider[] hits = Physics.OverlapSphere(center, chargeHitRadius, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                PlayerStats stats =
                    hits[i].GetComponentInParent<PlayerStats>() ??
                    hits[i].GetComponent<PlayerStats>();
                if (stats == null)
                    continue;

                stats.TakeDamage(chargeDamage);
                chargeHitApplied = true;
                break;
            }
        }

        private void MoveTowards(Vector3 destination, float speed)
        {
            Vector3 flatDirection = destination - transform.position;
            flatDirection.y = 0f;
            if (flatDirection.sqrMagnitude > 0.001f)
            {
                Quaternion lookRotation = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    lookRotation,
                    rotationSpeed * Time.deltaTime);
            }

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.speed = speed;
                agent.isStopped = false;
                agent.SetDestination(destination);
                return;
            }

            transform.position = Vector3.MoveTowards(
                transform.position,
                destination,
                speed * Time.deltaTime);
        }

        private void EnterStun()
        {
            isStunned = true;
            isCharging = false;
            stunEndTime = Time.time + Mathf.Max(0.1f, stunDuration);
            immunityEndTime = stunEndTime + Mathf.Max(0f, postStunImmunity);
            accumulatedWindowDamage = 0f;
            damageWindowEndTime = 0f;
            ResetMotion();
        }

        private void ExitStun()
        {
            isStunned = false;
            nextChargeTime = Time.time + 1f;
        }

        private void HandleDamaged(EnemyBase damagedEnemy, float damageAmount)
        {
            if (damagedEnemy != enemy || isStunned || Time.time < immunityEndTime)
                return;

            if (Time.time > damageWindowEndTime)
                accumulatedWindowDamage = 0f;

            damageWindowEndTime = Time.time + Mathf.Max(0.1f, stunWindow);
            accumulatedWindowDamage += damageAmount;

            float threshold = enemy.MaxHealth * Mathf.Clamp01(stunDamageThresholdPercent);
            if (accumulatedWindowDamage >= threshold)
                EnterStun();
        }

        private void HandleInitialized(EnemyBase initializedEnemy, EnemySpawnData data)
        {
            if (initializedEnemy != enemy)
                return;

            enabled = data.type == EnemyType.Brute;
            ResetState();
        }

        private void ResetState()
        {
            nextChargeTime = Time.time + 1.5f;
            chargeEndTime = 0f;
            stunEndTime = 0f;
            immunityEndTime = 0f;
            damageWindowEndTime = 0f;
            accumulatedWindowDamage = 0f;
            isCharging = false;
            isStunned = false;
            chargeHitApplied = false;
            chargeTargetPosition = transform.position;
            ResetMotion();
        }

        private void ResetMotion()
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
        }

        private void ResolveTarget()
        {
            if (target != null)
                return;

            target = enemy != null ? enemy.ResolveTarget() : null;
        }

        private void EnsureSubscriptions()
        {
            if (enemy == null)
                enemy = GetComponent<EnemyBase>();
            if (enemy == null)
                return;

            enemy.OnDamaged -= HandleDamaged;
            enemy.OnDamaged += HandleDamaged;
            enemy.OnInitialized -= HandleInitialized;
            enemy.OnInitialized += HandleInitialized;
        }

        private void RemoveSubscriptions()
        {
            if (enemy == null)
                return;

            enemy.OnDamaged -= HandleDamaged;
            enemy.OnInitialized -= HandleInitialized;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.75f);
            Vector3 center = transform.position + transform.forward * chargeForwardOffset;
            Gizmos.DrawWireSphere(center, chargeHitRadius);
        }
    }
}
