using UnityEngine;
using System.Collections;
using HyperManzana.Weapons;

namespace HyperManzana.Player
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HyperManzana/Player/Player Weapons (4 slots)")]
    public class PlayerWeapons : MonoBehaviour
    {
        [System.Serializable]
        public class Weapon
        {
            [Header("Setup")]
            public string name;
            public GameObject weaponObject; // Se activa al equipar
            public Transform firePoint;     // Origen del disparo

            [Header("Disparo")]
            [Tooltip("Segundos entre disparos (0 = sin límite)")]
            public float shotCooldown = 0.15f;
            [Tooltip("Disparo automático manteniendo click izquierdo")]
            public bool automatic = false;

            [Header("Munición")]
            public int clipSize = 10;
            public int loadedAmmo = 10;
            public bool useReserve = false;
            public int reserveAmmo = 0;

            [Header("Animación (opcional)")]
            public Animator animator;
            public string triggerEquip = "Equip";
            public string triggerUnequip = "Unequip";
            public string triggerReload = "Reload";
            public bool useAnimationEvents = true; // Requiere WeaponAnimationRelay en el arma
            public float equipDuration = 0.25f;    // Fallback si no hay eventos/animator
            public float unequipDuration = 0.25f;  // Fallback
            public float reloadDuration = 1.0f;    // Fallback

            [Header("Disparo opcional")]
            public GameObject projectilePrefab;
            public float projectileSpeed = 40f;
            public float raycastDistance = 150f; // si no hay proyectil

            [HideInInspector] public float nextShotTime;
        }

        private enum SwitchState { None, Unequipping, Equipping }

        [Header("Armas (máximo 4)")]
        [SerializeField] private Weapon[] weapons = new Weapon[2];

        [Header("Estado")]
        [SerializeField] private int currentIndex = 0; // 0..3
        [SerializeField] private bool isReloading = false;
        [SerializeField] private SwitchState switchState = SwitchState.None;
        [SerializeField] private int pendingIndex = -1;

        private Coroutine reloadRoutine;
        private Coroutine switchRoutine;

        public int CurrentIndex => currentIndex;
        public Weapon Current => (weapons != null && currentIndex >= 0 && currentIndex < weapons.Length) ? weapons[currentIndex] : null;

        private void Start()
        {
            // Activar solo el arma equipada al inicio
            if (weapons != null && weapons.Length > 0)
                currentIndex = Mathf.Clamp(currentIndex, 0, weapons.Length - 1);
            ActivateOnly(currentIndex);
            // Sincronizar visual de balas del arma equipada
            var cw = Current;
            if (cw != null && cw.weaponObject != null)
            {
                var vb = cw.weaponObject.GetComponent<WeaponVisibleBullets>();
                if (vb != null) { vb.SetCapacity(cw.clipSize); vb.SetAmmo(cw.loadedAmmo); vb.Event_BulletsFollowAmmo(); }
            }
        }

        private void Update()
        {
            HandleEquipInput();
            HandleReloadInput();
            HandleFireInput();
        }

        private void HandleEquipInput()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) RequestEquip(0);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) RequestEquip(1);
        }

        private void HandleReloadInput()
        {
            if (Input.GetKeyDown(KeyCode.R)) StartReload();
        }

        private void HandleFireInput()
        {
            var w = Current;
            if (w == null) return;
            if (switchState != SwitchState.None) return; // No disparar durante cambio de arma
            if (isReloading) return; // No disparar durante recarga

            bool firePressed = w.automatic ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0); // click izquierdo
            if (!firePressed) return;

            if (w.shotCooldown > 0f && Time.time < w.nextShotTime)
                return;

            TryFire(w);
        }

        // === Equip logic ===
        public void RequestEquip(int index)
        {
            if (weapons == null || weapons.Length == 0) return;
            index = Mathf.Clamp(index, 0, weapons.Length - 1);
            if (index == currentIndex && switchState == SwitchState.None) return;
            if (switchState != SwitchState.None) return; // Evitar encadenar cambios

            // Cancelar recarga si estuviera
            CancelReload();

            pendingIndex = index;
            BeginUnequip();
        }

        private void BeginUnequip()
        {
            var w = Current;
            if (w == null)
            {
                FinishUnequipAndActivateNew();
                return;
            }
            switchState = SwitchState.Unequipping;
            if (w.animator != null)
            {
                w.animator.ResetTrigger(w.triggerEquip);
                w.animator.SetTrigger(w.triggerUnequip);
            }
            if (switchRoutine != null) StopCoroutine(switchRoutine);
            if (w.animator == null || !w.useAnimationEvents)
            {
                switchRoutine = StartCoroutine(Co_WaitAndCall(w.unequipDuration, OnAnimUnequipCompleteInternal));
            }
        }

        private void FinishUnequipAndActivateNew()
        {
            // Desactivar todos y activar el seleccionado
            ActivateOnly(pendingIndex);
            currentIndex = pendingIndex;
            pendingIndex = -1;
            BeginEquip();
        }

        private void BeginEquip()
        {
            var w = Current;
            switchState = SwitchState.Equipping;
            if (w != null && w.animator != null)
            {
                w.animator.ResetTrigger(w.triggerUnequip);
                w.animator.SetTrigger(w.triggerEquip);
            }
            if (switchRoutine != null) StopCoroutine(switchRoutine);
            if (w == null || w.animator == null || !w.useAnimationEvents)
            {
                float wait = w != null ? w.equipDuration : 0f;
                switchRoutine = StartCoroutine(Co_WaitAndCall(wait, OnAnimEquipCompleteInternal));
            }
        }

        private void ActivateOnly(int index)
        {
            for (int i = 0; i < weapons.Length; i++)
            {
                var wo = weapons[i]?.weaponObject;
                if (wo != null) wo.SetActive(i == index);
            }
        }

        // === Reload logic ===
        public void StartReload()
        {
            var w = Current;
            if (w == null) return;
            if (switchState != SwitchState.None) return; // No recargar durante cambio
            if (isReloading) return;
            if (w.loadedAmmo >= w.clipSize) return; // ya lleno
            if (w.useReserve && w.reserveAmmo <= 0) return; // sin reserva

            isReloading = true;
            if (w.animator != null)
            {
                w.animator.SetTrigger(w.triggerReload);
            }
            if (reloadRoutine != null) StopCoroutine(reloadRoutine);
            if (w.animator == null || !w.useAnimationEvents)
            {
                reloadRoutine = StartCoroutine(Co_WaitAndCall(w.reloadDuration, OnAnimReloadCompleteInternal));
            }
        }

        public void CancelReload()
        {
            if (!isReloading) return;
            isReloading = false;
            if (reloadRoutine != null)
            {
                StopCoroutine(reloadRoutine);
                reloadRoutine = null;
            }
            // No completar recarga; solo salir del estado
        }

        private void CompleteReload()
        {
            var w = Current;
            if (w == null) return;

            int missing = Mathf.Max(0, w.clipSize - w.loadedAmmo);
            if (missing == 0) return;

            if (w.useReserve)
            {
                int taken = Mathf.Min(missing, Mathf.Max(0, w.reserveAmmo));
                w.reserveAmmo -= taken;
                w.loadedAmmo += taken;
            }
            else
            {
                // Por ahora, no consume: llena directamente
                w.loadedAmmo = w.clipSize;
            }
        }

        // === Fire ===
        private void TryFire(Weapon w)
        {
            if (w.firePoint == null)
            {
                Debug.LogWarning($"[{nameof(PlayerWeapons)}] El firePoint no está asignado en arma '{w.name}'.");
                return;
            }

            if (w.loadedAmmo <= 0)
            {
                Debug.Log("Sin munición cargada");
                return;
            }

            w.loadedAmmo--;
            if (w.shotCooldown > 0f)
                w.nextShotTime = Time.time + w.shotCooldown;
            // Actualizar visual de balas (solo si el arma tiene visibles)
            if (w.weaponObject != null)
            {
                var vb = w.weaponObject.GetComponent<WeaponVisibleBullets>();
                if (vb != null) vb.SetAmmo(w.loadedAmmo);
            }

            // Disparo
            if (w.projectilePrefab != null)
            {
                var proj = Instantiate(w.projectilePrefab, w.firePoint.position, w.firePoint.rotation);
                var rb = proj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // Mantener consistencia con tu proyecto (linearVelocity)
                    rb.linearVelocity = w.firePoint.forward * w.projectileSpeed;
                }
            }
            else
            {
                if (Physics.Raycast(w.firePoint.position, w.firePoint.forward, out var hit, w.raycastDistance))
                {
                    Debug.Log($"Impacto en {hit.collider.name}");
                }
                else
                {
                    Debug.Log("Disparo (sin impacto)");
                }
            }
        }

        // === Animator Events entry points (desde WeaponAnimationRelay) ===
        public void OnAnimReloadComplete(GameObject sourceWeaponGO)
        {
            // La recarga solo se completa si sigue activa y no se cambió de arma
            if (!isReloading) return;
            var w = Current;
            if (w == null || w.weaponObject != sourceWeaponGO) return;

            CompleteReload();
            isReloading = false;
            // Sincronizar visual de balas tras completar recarga
            var vb = w.weaponObject != null ? w.weaponObject.GetComponent<WeaponVisibleBullets>() : null;
            if (vb != null) { vb.SetAmmo(w.loadedAmmo); vb.Event_BulletsFollowAmmo(); }
        }

        public void OnAnimUnequipComplete(GameObject sourceWeaponGO)
        {
            var w = Current;
            if (switchState != SwitchState.Unequipping) return;
            if (w != null && w.weaponObject != sourceWeaponGO) return; // evento de otra arma
            OnAnimUnequipCompleteInternal();
        }

        public void OnAnimEquipComplete(GameObject sourceWeaponGO)
        {
            var w = Current;
            if (switchState != SwitchState.Equipping) return;
            if (w != null && w.weaponObject != sourceWeaponGO) return; // evento de otra arma
            OnAnimEquipCompleteInternal();
        }

        // === Internals for fallback timers ===
        private IEnumerator Co_WaitAndCall(float seconds, System.Action action)
        {
            if (seconds > 0f) yield return new WaitForSeconds(seconds);
            else yield return null;
            action?.Invoke();
        }

        private void OnAnimReloadCompleteInternal()
        {
            if (!isReloading) return;
            CompleteReload();
            isReloading = false;
            reloadRoutine = null;
            // Fallback sin eventos: sincronizar visual
            var w = Current;
            if (w != null && w.weaponObject != null)
            {
                var vb = w.weaponObject.GetComponent<WeaponVisibleBullets>();
                if (vb != null) { vb.SetAmmo(w.loadedAmmo); vb.Event_BulletsFollowAmmo(); }
            }
        }

        private void OnAnimUnequipCompleteInternal()
        {
            FinishUnequipAndActivateNew();
            switchRoutine = null;
        }

        private void OnAnimEquipCompleteInternal()
        {
            switchState = SwitchState.None;
            switchRoutine = null;
            // Sync visual de balas al equipar
            var cw = Current;
            if (cw != null && cw.weaponObject != null)
            {
                var vb = cw.weaponObject.GetComponent<WeaponVisibleBullets>();
                if (vb != null)
                {
                    vb.SetCapacity(cw.clipSize);
                    vb.SetAmmo(cw.loadedAmmo);
                    vb.Event_BulletsFollowAmmo();
                }
            }
        }
    }
}
