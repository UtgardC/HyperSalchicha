using System;
using UnityEngine;

namespace HyperManzana.Weapons
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HyperManzana/Weapons/Weapon Controller")]
    public class WeaponController : MonoBehaviour
    {
        [Header("Definition")]
        [SerializeField] private WeaponDefinition weaponDefinition;

        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private Transform firePoint;
        [SerializeField] private WeaponVisibleBullets visibleBullets;
        [SerializeField] private WeaponCameraRecoil weaponCameraRecoil;
        [SerializeField] private IndexedWeaponAudioPlayer weaponAudio;

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
        [SerializeField] private int emptyMagazineAudioIndex = -1;
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

        public event Action<WeaponController, int, int> AmmoChanged;

        public WeaponDefinition Definition => weaponDefinition;
        public int SlotIndex => slotIndex;
        public int CurrentAmmo => currentAmmo;
        public int ReserveAmmo => reserveAmmo;
        public bool IsReloading => animator != null ? animator.GetBool(isReloadingParam) : isReloadingLocal;
        public bool IsEquippedDesired => animator != null ? animator.GetBool(isEquippedParam) : isEquippedLocal;

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

        public void OnAudioEvent(int eventIndex)
        {
            if (weaponAudio == null)
                CacheMissingReferences();
            if (weaponAudio == null)
                return;
            weaponAudio.PlayByIndex(eventIndex);
        }

        public void OnSharedAudioEvent(int eventIndex)
        {
            ownerManager?.PlaySharedAudioEvent(eventIndex);
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
            float damage = weaponDefinition.damagePerShot;

            if (weaponDefinition.fireMode == WeaponFireMode.Projectile)
            {
                if (weaponDefinition.projectilePrefab == null)
                {
                    LogWarn("Projectile mode requires projectilePrefab.");
                    return;
                }

                GameObject projectile = Instantiate(
                    weaponDefinition.projectilePrefab,
                    origin.position,
                    origin.rotation);

                var payload = projectile.GetComponent<ProjectileDamagePayload>();
                if (payload == null)
                    payload = projectile.AddComponent<ProjectileDamagePayload>();
                payload.SetDamage(damage);

                var bullet = projectile.GetComponent<BulletScript>();
                if (bullet != null)
                    bullet.damage = damage;

                var rb = projectile.GetComponent<Rigidbody>();
                if (rb != null)
                    rb.linearVelocity = origin.forward * weaponDefinition.projectileSpeed;
                return;
            }

            GetHitscanRay(out Vector3 rayOrigin, out Vector3 rayDirection);
            if (TryFindHitscanHit(rayOrigin, rayDirection, hitscanTriggers, out RaycastHit hit))
            {
                EnemyScript enemy = hit.collider.GetComponentInParent<EnemyScript>();
                if (enemy != null)
                    enemy.TakeDamage(damage);
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
            return weaponDefinition != null && weaponDefinition.infiniteReserve;
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
                weaponAudio = GetComponentInChildren<IndexedWeaponAudioPlayer>(true);
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
            if (emptyMagazineAudioIndex < 0)
                return;
            if (Time.time < nextEmptyMagazineAudioTime)
                return;
            if (weaponAudio == null)
                CacheMissingReferences();
            if (weaponAudio == null)
                return;

            weaponAudio.PlayByIndex(emptyMagazineAudioIndex);
            nextEmptyMagazineAudioTime = Time.time + Mathf.Max(0.01f, emptyMagazineAudioCooldown);
        }

        private void LogWarn(string message)
        {
            if (!logWarnings)
                return;
            Debug.LogWarning($"[{nameof(WeaponController)}] {message}", this);
        }
    }
}
