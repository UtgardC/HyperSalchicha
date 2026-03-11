using HyperSalchicha.Player;
using UnityEngine;

namespace HyperSalchicha.Enemies
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HyperSalchicha/Enemies/Flyer Placeholder")]
    public class EnemyFlyerPlaceholder : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float hoverHeight = 3.5f;
        [SerializeField] private float minHoverDistance = 8f;
        [SerializeField] private float maxHoverDistance = 13f;
        [SerializeField] private float orbitSpeedDegrees = 40f;
        [SerializeField] private float lookSlerpSpeed = 8f;

        [Header("Laser Attack")]
        [SerializeField] private float initialAttackDelay = 1.2f;
        [SerializeField] private float chargeTime = 1.4f;
        [SerializeField] private float attackCooldown = 4.5f;
        [SerializeField] private float laserDamage = 18f;
        [SerializeField] private float laserRadius = 1.75f;
        [SerializeField] private LineRenderer laserLine;
        [SerializeField] private float laserDisplayDuration = 0.2f;

        private EnemyBase enemy;
        private Transform target;
        private float orbitAngle;
        private float chargeEndTime;
        private float nextAttackTime;
        private float laserHideTime;
        private bool isCharging;
        private Vector3 lockedTargetPosition;

        private void Awake()
        {
            enemy = GetComponent<EnemyBase>();
            orbitAngle = Random.Range(0f, 360f);
            HideLaserLine();
        }

        private void OnEnable()
        {
            EnsureSubscriptions();
            nextAttackTime = Time.time + Mathf.Max(0f, initialAttackDelay);
            isCharging = false;
            orbitAngle = Random.Range(0f, 360f);
            HideLaserLine();
        }

        private void OnDisable()
        {
            RemoveSubscriptions();
            isCharging = false;
            HideLaserLine();
        }

        private void Update()
        {
            if (enemy == null || enemy.IsDead || enemy.Type != EnemyType.Flyer)
                return;

            ResolveTarget();
            if (target == null)
                return;

            UpdateMovement();
            UpdateAttack();
            UpdateLaserDisplay();
        }

        private void UpdateMovement()
        {
            orbitAngle += orbitSpeedDegrees * Time.deltaTime;
            float hoverDistance = Mathf.Lerp(minHoverDistance, maxHoverDistance, 0.5f + Mathf.Sin(Time.time * 0.4f) * 0.5f);
            Vector3 orbitOffset = Quaternion.Euler(0f, orbitAngle, 0f) * Vector3.forward * hoverDistance;
            Vector3 desiredPosition = target.position + orbitOffset + Vector3.up * hoverHeight;

            transform.position = Vector3.MoveTowards(
                transform.position,
                desiredPosition,
                moveSpeed * Time.deltaTime);

            Vector3 lookDirection = target.position - transform.position;
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                Quaternion lookRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    lookRotation,
                    lookSlerpSpeed * Time.deltaTime);
            }
        }

        private void UpdateAttack()
        {
            if (!isCharging)
            {
                if (Time.time >= nextAttackTime)
                    StartCharge();
                return;
            }

            if (Time.time >= chargeEndTime)
                FireLaser();
        }

        private void StartCharge()
        {
            isCharging = true;
            chargeEndTime = Time.time + Mathf.Max(0.1f, chargeTime);
            lockedTargetPosition = target.position;
        }

        private void CancelCharge()
        {
            isCharging = false;
            nextAttackTime = Time.time + Mathf.Max(0.2f, attackCooldown * 0.45f);
        }

        private void FireLaser()
        {
            isCharging = false;
            nextAttackTime = Time.time + Mathf.Max(0.1f, attackCooldown);

            PlayerStats stats =
                target.GetComponentInParent<PlayerStats>() ??
                target.GetComponent<PlayerStats>();
            if (stats != null && Vector3.Distance(target.position, lockedTargetPosition) <= laserRadius)
                stats.TakeDamage(laserDamage);

            ShowLaserLine(transform.position, lockedTargetPosition);
        }

        private void ShowLaserLine(Vector3 start, Vector3 end)
        {
            if (laserLine == null)
                return;

            laserLine.enabled = true;
            laserLine.positionCount = 2;
            laserLine.SetPosition(0, start);
            laserLine.SetPosition(1, end);
            laserHideTime = Time.time + Mathf.Max(0.01f, laserDisplayDuration);
        }

        private void UpdateLaserDisplay()
        {
            if (laserLine == null || !laserLine.enabled)
                return;

            if (Time.time >= laserHideTime)
                HideLaserLine();
        }

        private void HideLaserLine()
        {
            if (laserLine != null)
                laserLine.enabled = false;
            laserHideTime = 0f;
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

        private void HandleDamaged(EnemyBase damagedEnemy, float damageAmount)
        {
            if (damagedEnemy != enemy || !isCharging)
                return;

            CancelCharge();
        }

        private void HandleInitialized(EnemyBase initializedEnemy, EnemySpawnData data)
        {
            if (initializedEnemy != enemy)
                return;

            enabled = data.type == EnemyType.Flyer;
            nextAttackTime = Time.time + Mathf.Max(0f, initialAttackDelay);
            isCharging = false;
            orbitAngle = Random.Range(0f, 360f);
            HideLaserLine();
        }
    }
}
