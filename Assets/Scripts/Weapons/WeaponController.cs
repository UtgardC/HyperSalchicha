using System;
using HyperSalchicha.Enemies;
using UnityEngine;

namespace HyperSalchicha.Weapons
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HyperSalchicha/Weapons/Weapon Controller")]
    public class WeaponController : MonoBehaviour
    {
        private const float HeatedDamageMultiplier = 1.75f;
        private const float OverclockFireRateMultiplier = 1.5f;
        private const int SpreadGizmoSegments = 24;

        [Header("Definition")]
        [SerializeField] private WeaponDefinition weaponDefinition;

        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private Transform firePoint;
        [SerializeField] private WeaponVisibleBullets visibleBullets;
        [SerializeField] private WeaponCameraRecoil weaponCameraRecoil;
        [SerializeField] private WeaponAudioController weaponAudio;

        [Header("Animator Parameters")]
        [SerializeField] private string isEquippedParam = "IsEquipped";
        [SerializeField] private string isReloadingParam = "IsReloading";
        [SerializeField] private string fireTriggerParam = "Fire";

        [Header("Combat / Hitscan")]
        [SerializeField] private QueryTriggerInteraction hitscanTriggers = QueryTriggerInteraction.Ignore;

        [Header("Camera Kick")]
        [SerializeField] private int cameraKickPresetIndex;
        [SerializeField] private float cameraKickPositionMultiplier = 1f;
        [SerializeField] private float cameraKickRotationMultiplier = 1f;
        [SerializeField] private float cameraKickDurationMultiplier = 1f;

        [Header("Audio")]
        [SerializeField] private float emptyMagazineAudioCooldown = 0.1f;

        [Header("Debug")]
        [SerializeField] private bool logWarnings = true;

        private WeaponManager ownerManager;
        private int slotIndex = -1;
        private int currentAmmo;
        private int reserveAmmo;
        private float nextShotTime;
        private bool inputEnabled;
        private bool waitForFireReleaseAfterReloadCancel;
        private bool isEquippedLocal;
        private bool isReloadingLocal;
        private bool reloadStepAppliedThisCycle;
        private float nextEmptyMagazineAudioTime;
        private readonly RaycastHit[] hitscanBuffer = new RaycastHit[32];
        private PlayerControllerAlt ownerPlayerController;
        private int enemyLayerMask;
        private WeaponEnhancementFlags currentEnhancements = WeaponEnhancementFlags.None;
        private WeaponEnhancementVisuals enhancementVisuals;

        public event Action<WeaponController, int, int> AmmoChanged;

        public WeaponDefinition Definition => weaponDefinition;
        public int SlotIndex => slotIndex;
        public int CurrentAmmo => currentAmmo;
        public int ReserveAmmo => reserveAmmo;
        public bool IsReloading => animator != null ? animator.GetBool(isReloadingParam) : isReloadingLocal;
        public bool IsEquippedDesired => animator != null ? animator.GetBool(isEquippedParam) : isEquippedLocal;
        public WeaponEnhancementFlags ActiveEnhancements => currentEnhancements;
        public bool HasInfiniteAmmoSupply => HasInfiniteMagazine() || HasInfiniteReserve();

        private void Awake()
        {
            CacheMissingReferences();
            CacheEnemyLayerMask();
        }

        private void Reset()
        {
            CacheMissingReferences();
        }

        public void Initialize(WeaponManager manager, WeaponDefinition definition, int index)
        {
            ownerManager = manager;
            weaponDefinition = definition;
            slotIndex = index;
            CacheMissingReferences();
            ownerPlayerController = ownerManager != null ? ownerManager.GetComponent<PlayerControllerAlt>() : null;
            ConfigureAudioAnchorParent();
            if (enhancementVisuals != null)
            {
                enhancementVisuals.ApplyDefinitionOverrides(weaponDefinition);
                enhancementVisuals.Apply(currentEnhancements);
            }

            if (weaponDefinition == null)
            {
                currentAmmo = 0;
                reserveAmmo = 0;
            }
            else
            {
                currentAmmo = Mathf.Clamp(
                    weaponDefinition.startingMagazineAmmo,
                    0,
                    Mathf.Max(0, weaponDefinition.magazineCapacity));
                reserveAmmo = Mathf.Max(0, weaponDefinition.startingReserveAmmo);
            }

            nextShotTime = 0f;
            inputEnabled = false;
            waitForFireReleaseAfterReloadCancel = false;
            reloadStepAppliedThisCycle = false;
            nextEmptyMagazineAudioTime = 0f;
            SetAnimatorBool(isReloadingParam, false);
            SetAnimatorBool(isEquippedParam, false);
            SyncVisibleBullets();
            RaiseAmmoChanged();
        }

        private void ConfigureAudioAnchorParent()
        {
            if (weaponAudio == null)
                return;

            Transform anchorParent = ownerPlayerController != null ? ownerPlayerController.cameraTransform : null;
            if (anchorParent == null && ownerManager != null)
                anchorParent = ownerManager.transform;
            if (anchorParent != null)
                weaponAudio.SetAudioAnchorParent(anchorParent);
        }

        public void SetEquippedDesired(bool isEquipped)
        {
            SetAnimatorBool(isEquippedParam, isEquipped);
            if (!isEquipped)
            {
                inputEnabled = false;
                waitForFireReleaseAfterReloadCancel = false;
                reloadStepAppliedThisCycle = false;
                SetAnimatorBool(isReloadingParam, false);
            }
        }

        public bool TryStartReload()
        {
            if (!CanAcceptInput())
                return false;
            if (IsReloading)
                return false;
            if (!CanReload())
                return false;

            reloadStepAppliedThisCycle = false;
            SetAnimatorBool(isReloadingParam, true);
            return true;
        }

        public void CancelReloadBySwap()
        {
            if (IsReloading)
            {
                SetAnimatorBool(isReloadingParam, false);
                reloadStepAppliedThisCycle = false;
            }
        }

        public void TickCombatInput(bool fireDown, bool fireHeld, bool fireUp)
        {
            if (fireUp)
                waitForFireReleaseAfterReloadCancel = false;

            if (!CanAcceptInput())
                return;

            if (waitForFireReleaseAfterReloadCancel)
            {
                if (!fireHeld)
                    waitForFireReleaseAfterReloadCancel = false;
                return;
            }

            if (IsReloading)
            {
                if (fireDown && currentAmmo > 0)
                {
                    if (!reloadStepAppliedThisCycle)
                    {
                        // Early cancel before ammo transfer.
                        SetAnimatorBool(isReloadingParam, false);
                        waitForFireReleaseAfterReloadCancel = true;
                    }
                    else
                    {
                        // After transfer, allow immediate shot from reload.
                        SetAnimatorBool(isReloadingParam, false);
                        reloadStepAppliedThisCycle = false;
                        TryShoot();
                    }
                }
                return;
            }

            bool wantsToShoot = weaponDefinition != null && weaponDefinition.isAutomatic ? fireHeld : fireDown;
            if (!wantsToShoot)
                return;

            if (!HasAmmoToShoot())
            {
                TryPlayEmptyMagazineAudio();
                return;
            }

            TryShoot();
        }

        public void OnWeaponReadyToFire()
        {
            inputEnabled = true;
        }

        public void OnHolsteredStateEntered()
        {
            ownerManager?.OnWeaponHolstered(this);
        }

        public void OnCameraKick()
        {
            if (weaponCameraRecoil == null)
                CacheMissingReferences();

            if (weaponCameraRecoil != null)
            {
                weaponCameraRecoil.Event_PlayKickScaled(
                    cameraKickPresetIndex,
                    Mathf.Max(0f, cameraKickPositionMultiplier),
                    Mathf.Max(0f, cameraKickRotationMultiplier),
                    Mathf.Max(0.01f, cameraKickDurationMultiplier));
            }
        }

        public void OnAudioEvent(string eventID)
        {
            if (weaponAudio == null)
                CacheMissingReferences();
            if (weaponAudio == null)
                return;
            weaponAudio.PlayAnimSound(eventID);
        }

        public void OnSharedAudioEvent(string eventID)
        {
            ownerManager?.PlaySharedAudioEvent(eventID);
        }

        public void StopAllAudio()
        {
            if (weaponAudio == null)
                CacheMissingReferences();
            weaponAudio?.StopAllSounds();
        }

        public bool HasEnhancement(WeaponEnhancementFlags flag)
        {
            return (currentEnhancements & flag) != 0;
        }

        public void AddEnhancement(WeaponEnhancementFlags flag)
        {
            currentEnhancements |= flag;
            enhancementVisuals?.Apply(currentEnhancements);
        }

        public void RemoveEnhancement(WeaponEnhancementFlags flag)
        {
            currentEnhancements &= ~flag;
            enhancementVisuals?.Apply(currentEnhancements);
        }

        public void SetEnhancements(WeaponEnhancementFlags flags)
        {
            currentEnhancements = flags;
            enhancementVisuals?.Apply(currentEnhancements);
        }

        public void OnBulletInserted()
        {
            if (!IsReloading || weaponDefinition == null)
                return;

            int capacity = Mathf.Max(0, weaponDefinition.magazineCapacity);
            int missing = Mathf.Max(0, capacity - currentAmmo);
            if (missing <= 0)
            {
                SetAnimatorBool(isReloadingParam, false);
                reloadStepAppliedThisCycle = false;
                return;
            }

            bool infiniteReserve = HasInfiniteReserve();
            if (weaponDefinition.reloadMode == WeaponReloadMode.Magazine)
            {
                int add = infiniteReserve ? missing : Mathf.Min(missing, Mathf.Max(0, reserveAmmo));
                currentAmmo += add;
                if (!infiniteReserve)
                    reserveAmmo -= add;

                reloadStepAppliedThisCycle = add > 0;
            }
            else
            {
                int step = Mathf.Max(1, weaponDefinition.reloadStepAmount);
                int requested = Mathf.Min(step, missing);
                int add = infiniteReserve ? requested : Mathf.Min(requested, Mathf.Max(0, reserveAmmo));

                currentAmmo += add;
                if (!infiniteReserve)
                    reserveAmmo -= add;
                reloadStepAppliedThisCycle = add > 0;

                if (currentAmmo >= capacity || (!infiniteReserve && reserveAmmo <= 0))
                {
                    SetAnimatorBool(isReloadingParam, false);
                    reloadStepAppliedThisCycle = false;
                }
            }

            SyncVisibleBullets();
            RaiseAmmoChanged();
        }

        public void OnReloadAnimationFinished()
        {
            if (!IsReloading)
                return;

            SetAnimatorBool(isReloadingParam, false);
            reloadStepAppliedThisCycle = false;
        }

        private bool TryShoot()
        {
            if (weaponDefinition == null)
                return false;
            if (!HasAmmoToShoot())
                return false;

            float fireRate = Mathf.Max(0f, weaponDefinition.fireRateSeconds);
            if (HasEnhancement(WeaponEnhancementFlags.Overclocked))
                fireRate /= OverclockFireRateMultiplier;
            if (ownerManager != null)
                fireRate /= Mathf.Max(0.01f, ownerManager.ExternalFireRateMultiplier);

            if (fireRate > 0f && Time.time < nextShotTime)
                return false;

            if (!HasInfiniteMagazine())
                currentAmmo = Mathf.Max(0, currentAmmo - 1);

            FireShot();
            TriggerFireAnimation();

            if (fireRate > 0f)
                nextShotTime = Time.time + fireRate;

            SyncVisibleBullets();
            RaiseAmmoChanged();
            return true;
        }

        private void FireShot()
        {
            if (weaponDefinition == null)
                return;

            Transform origin = firePoint != null ? firePoint : transform;
            GetHitscanRay(out Vector3 rayOrigin, out Vector3 baseRayDirection);

            int pelletCount = GetPelletCount();
            float damagePerPellet = GetDamagePerTriggerPull() / pelletCount;

            if (weaponDefinition.fireMode == WeaponFireMode.Projectile)
            {
                if (weaponDefinition.projectilePrefab == null)
                {
                    LogWarn("Projectile mode requires projectilePrefab.");
                    return;
                }

                for (int i = 0; i < pelletCount; i++)
                {
                    Vector3 shotDirection = BuildShotDirection(rayOrigin, baseRayDirection);
                    FireProjectilePellet(origin, shotDirection, damagePerPellet);
                }
                return;
            }

            for (int i = 0; i < pelletCount; i++)
            {
                Vector3 shotDirection = BuildShotDirection(rayOrigin, baseRayDirection);
                FireHitscanPellet(rayOrigin, shotDirection, damagePerPellet);
            }
        }

        private void TriggerFireAnimation()
        {
            if (animator == null)
                return;

            if (!string.IsNullOrEmpty(fireTriggerParam))
                animator.SetTrigger(fireTriggerParam);
        }

        private bool CanAcceptInput()
        {
            return inputEnabled && IsEquippedDesired && gameObject.activeInHierarchy;
        }

        private bool CanReload()
        {
            if (weaponDefinition == null)
                return false;

            int capacity = Mathf.Max(0, weaponDefinition.magazineCapacity);
            if (currentAmmo >= capacity)
                return false;

            return HasInfiniteReserve() || reserveAmmo > 0;
        }

        private bool HasAmmoToShoot()
        {
            return HasInfiniteMagazine() || currentAmmo > 0;
        }

        private bool HasInfiniteMagazine()
        {
            if (weaponDefinition == null)
                return false;
            return weaponDefinition.infiniteMagazine || (ownerManager != null && ownerManager.GlobalInfiniteMagazinePowerupActive);
        }

        private bool HasInfiniteReserve()
        {
            return weaponDefinition != null &&
                (weaponDefinition.infiniteReserve ||
                HasEnhancement(WeaponEnhancementFlags.Quantum) ||
                (ownerManager != null && ownerManager.GlobalInfiniteMagazinePowerupActive));
        }

        private void SetAnimatorBool(string param, bool value)
        {
            if (param == isEquippedParam)
                isEquippedLocal = value;
            else if (param == isReloadingParam)
                isReloadingLocal = value;

            if (animator != null && !string.IsNullOrEmpty(param))
                animator.SetBool(param, value);
        }

        private void SyncVisibleBullets()
        {
            if (visibleBullets == null || weaponDefinition == null)
                return;

            visibleBullets.SetCapacity(Mathf.Max(0, weaponDefinition.magazineCapacity));
            visibleBullets.SetAmmo(currentAmmo);
            visibleBullets.Event_BulletsFollowAmmo();
        }

        private void RaiseAmmoChanged()
        {
            AmmoChanged?.Invoke(this, currentAmmo, reserveAmmo);
        }

        private void CacheMissingReferences()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);
            if (visibleBullets == null)
                visibleBullets = GetComponentInChildren<WeaponVisibleBullets>(true);
            if (weaponAudio == null)
                weaponAudio = GetComponentInChildren<WeaponAudioController>(true);
            if (enhancementVisuals == null)
                enhancementVisuals = GetComponentInChildren<WeaponEnhancementVisuals>(true);
            if (firePoint == null)
                firePoint = transform;
            if (weaponCameraRecoil == null)
            {
                weaponCameraRecoil = GetComponentInParent<WeaponCameraRecoil>();
                if (weaponCameraRecoil == null)
                {
                    Transform cursor = transform.parent;
                    while (cursor != null && weaponCameraRecoil == null)
                    {
                        weaponCameraRecoil = cursor.GetComponentInChildren<WeaponCameraRecoil>(true);
                        cursor = cursor.parent;
                    }
                }
            }
        }

        private void CacheEnemyLayerMask()
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer < 0)
            {
                Debug.LogError("[WeaponController] Layer 'Enemy' no existe.", this);
                enemyLayerMask = 0;
                return;
            }

            enemyLayerMask = 1 << enemyLayer;
        }

        private bool TryFindHitscanHit(
            Vector3 rayOrigin,
            Vector3 rayDirection,
            QueryTriggerInteraction triggerMode,
            out RaycastHit bestHit)
        {
            bestHit = default;
            if (weaponDefinition == null || enemyLayerMask == 0)
                return false;

            int hitCount = Physics.RaycastNonAlloc(
                rayOrigin,
                rayDirection,
                hitscanBuffer,
                weaponDefinition.raycastDistance,
                enemyLayerMask,
                triggerMode);
            if (hitCount <= 0)
                return false;

            float bestDistance = float.MaxValue;
            bool found = false;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = hitscanBuffer[i];
                if (hit.collider == null)
                    continue;

                if (hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    bestHit = hit;
                    found = true;
                }
            }

            return found;
        }

        private void GetHitscanRay(out Vector3 rayOrigin, out Vector3 rayDirection)
        {
            if (ownerPlayerController != null && ownerPlayerController.cameraTransform != null)
            {
                rayOrigin = ownerPlayerController.cameraTransform.position;
                rayDirection = ownerPlayerController.cameraTransform.forward;
                return;
            }

            Transform origin = firePoint != null ? firePoint : transform;

            rayOrigin = origin.position;
            rayDirection = origin.forward;
        }

        private void TryPlayEmptyMagazineAudio()
        {
            if (Time.time < nextEmptyMagazineAudioTime)
                return;
            if (weaponAudio == null)
                CacheMissingReferences();
            if (weaponAudio == null)
                return;

            weaponAudio.PlayDryFire();
            nextEmptyMagazineAudioTime = Time.time + Mathf.Max(0.01f, emptyMagazineAudioCooldown);
        }

        private int GetPelletCount()
        {
            if (weaponDefinition == null || !weaponDefinition.spreadShot)
                return 1;

            return Mathf.Max(1, weaponDefinition.spreadPelletCount);
        }

        private float GetDamagePerTriggerPull()
        {
            if (weaponDefinition == null)
                return 0f;

            float damage = weaponDefinition.damagePerShot;
            if (HasEnhancement(WeaponEnhancementFlags.Heated))
                damage *= HeatedDamageMultiplier;

            return damage;
        }

        private Vector3 BuildShotDirection(Vector3 rayOrigin, Vector3 baseDirection)
        {
            if (weaponDefinition == null || !weaponDefinition.spreadShot)
                return baseDirection;

            float spreadRadius = Mathf.Max(0f, weaponDefinition.spreadRadius);
            if (spreadRadius <= 0f)
                return baseDirection;

            float spreadDistance = Mathf.Max(0.01f, weaponDefinition.spreadDistance);
            Vector2 offset = UnityEngine.Random.insideUnitCircle * spreadRadius;
            GetSpreadBasis(baseDirection, out Vector3 right, out Vector3 up);

            Vector3 targetPoint =
                rayOrigin +
                baseDirection * spreadDistance +
                right * offset.x +
                up * offset.y;

            return (targetPoint - rayOrigin).normalized;
        }

        private void GetSpreadBasis(Vector3 forward, out Vector3 right, out Vector3 up)
        {
            if (ownerPlayerController != null && ownerPlayerController.cameraTransform != null)
            {
                right = ownerPlayerController.cameraTransform.right;
                up = ownerPlayerController.cameraTransform.up;
                return;
            }

            Quaternion rotation = Quaternion.LookRotation(forward);
            right = rotation * Vector3.right;
            up = rotation * Vector3.up;
        }

        private void FireHitscanPellet(Vector3 rayOrigin, Vector3 rayDirection, float damage)
        {
            if (!TryFindHitscanHit(rayOrigin, rayDirection, hitscanTriggers, out RaycastHit hit))
                return;

            EnemyBase enemy = hit.collider.GetComponentInParent<EnemyBase>();
            if (enemy != null)
                enemy.TakeDamage(damage);
        }

        private void FireProjectilePellet(Transform origin, Vector3 shotDirection, float damage)
        {
            GameObject projectile = Instantiate(
                weaponDefinition.projectilePrefab,
                origin.position,
                Quaternion.LookRotation(shotDirection));

            var payload = projectile.GetComponent<ProjectileDamagePayload>();
            if (payload == null)
                payload = projectile.AddComponent<ProjectileDamagePayload>();
            payload.SetDamage(damage);

            var bullet = projectile.GetComponent<BulletScript>();
            if (bullet != null)
                bullet.damage = damage;

            var rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
                rb.linearVelocity = shotDirection * weaponDefinition.projectileSpeed;
        }

        private void OnDrawGizmosSelected()
        {
            if (weaponDefinition == null)
                return;

            GetGizmoRay(out Vector3 rayOrigin, out Vector3 rayDirection);
            float maxDistance = Mathf.Max(0f, weaponDefinition.raycastDistance);
            if (maxDistance <= 0f)
                return;

            Gizmos.color = new Color(1f, 0.82f, 0.25f, 0.9f);
            Gizmos.DrawRay(rayOrigin, rayDirection * maxDistance);

            if (!weaponDefinition.spreadShot)
                return;

            float spreadDistance = Mathf.Min(Mathf.Max(0.01f, weaponDefinition.spreadDistance), maxDistance);
            float spreadRadius = Mathf.Max(0f, weaponDefinition.spreadRadius);
            if (spreadRadius <= 0f)
                return;

            GetSpreadBasis(rayDirection, out Vector3 right, out Vector3 up);
            Vector3 center = rayOrigin + rayDirection * spreadDistance;

            DrawWireCircle(center, right, up, spreadRadius);
            Gizmos.DrawLine(rayOrigin, center + right * spreadRadius);
            Gizmos.DrawLine(rayOrigin, center - right * spreadRadius);
            Gizmos.DrawLine(rayOrigin, center + up * spreadRadius);
            Gizmos.DrawLine(rayOrigin, center - up * spreadRadius);
        }

        private void GetGizmoRay(out Vector3 rayOrigin, out Vector3 rayDirection)
        {
            PlayerControllerAlt player = ownerPlayerController;
            if (player == null)
                player = GetComponentInParent<PlayerControllerAlt>();

            if (player != null && player.cameraTransform != null)
            {
                rayOrigin = player.cameraTransform.position;
                rayDirection = player.cameraTransform.forward;
                return;
            }

            Transform origin = firePoint != null ? firePoint : transform;
            rayOrigin = origin.position;
            rayDirection = origin.forward;
        }

        private static void DrawWireCircle(Vector3 center, Vector3 right, Vector3 up, float radius)
        {
            Vector3 previousPoint = center + right * radius;
            for (int i = 1; i <= SpreadGizmoSegments; i++)
            {
                float angle = (i / (float)SpreadGizmoSegments) * Mathf.PI * 2f;
                Vector3 nextPoint =
                    center +
                    (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * radius;

                Gizmos.DrawLine(previousPoint, nextPoint);
                previousPoint = nextPoint;
            }
        }

        private void LogWarn(string message)
        {
            if (!logWarnings)
                return;
            Debug.LogWarning($"[{nameof(WeaponController)}] {message}", this);
        }
    }
}
