using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HyperManzana.Weapons
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HyperManzana/Weapons/Weapon Manager")]
    public class WeaponManager : MonoBehaviour
    {
        private sealed class WeaponSlotRuntime
        {
            public WeaponDefinition definition;
            public WeaponController controller;

            public bool HasWeapon => definition != null && controller != null;
        }

        [Header("Setup")]
        [SerializeField] private Transform weaponRoot;
        [SerializeField] private WeaponDefinition startingWeaponSlot1;
        [SerializeField] private WeaponDefinition startingWeaponSlot2;
        [SerializeField] private int startEquippedSlot;
        [SerializeField] private IndexedWeaponAudioPlayer sharedWeaponAudio;

#if ENABLE_INPUT_SYSTEM
        [Header("Input Asset (required)")]
        [SerializeField] private InputActionAsset inputActionsAsset;
        [SerializeField] private string gameplayActionMap = "Gameplay";
        [SerializeField] private string fireActionName = "Fire";
        [SerializeField] private string reloadActionName = "Reload";
        [SerializeField] private string equipWeapon1ActionName = "EquipWeapon1";
        [SerializeField] private string equipWeapon2ActionName = "EquipWeapon2";
        [SerializeField] private string toggleWeaponActionName = "ToggleWeapon";
#endif

        private readonly WeaponSlotRuntime[] slots = new WeaponSlotRuntime[2];
        private int equippedSlot = -1;
        private int pendingSlot = -1;
        private WeaponController holsteringWeapon;
        private bool globalInfiniteMagazinePowerupActive;
        private float externalFireRateMultiplier = 1f;
        private bool inputsSuppressedByPause;
        private bool waitForFireReleaseAfterPause;

#if ENABLE_INPUT_SYSTEM
        private InputAction fireAction;
        private InputAction reloadAction;
        private InputAction equipWeapon1Action;
        private InputAction equipWeapon2Action;
        private InputAction toggleWeaponAction;
#endif

        public event Action<WeaponController, int> OnWeaponChanged;
        public event Action<int, int> OnAmmoChanged;

        public bool GlobalInfiniteMagazinePowerupActive => globalInfiniteMagazinePowerupActive;
        public float ExternalFireRateMultiplier => externalFireRateMultiplier;
        public int EquippedSlotIndex => equippedSlot;
        public WeaponController CurrentWeapon => IsValidSlot(equippedSlot) ? slots[equippedSlot].controller : null;
        public bool BlocksSprint => CurrentWeapon != null && CurrentWeapon.IsReloading;

        private void Awake()
        {
            if (!ValidateWiring())
            {
                enabled = false;
                return;
            }

            for (int i = 0; i < slots.Length; i++)
                slots[i] = new WeaponSlotRuntime();

#if ENABLE_INPUT_SYSTEM
            if (!ResolveInputActions())
            {
                enabled = false;
                return;
            }
            EnableInputActions();
#else
            Debug.LogError("[WeaponManager] ENABLE_INPUT_SYSTEM está deshabilitado.", this);
            enabled = false;
#endif
        }

        private void Start()
        {
            if (startingWeaponSlot1 != null)
                InstallWeaponInSlot(0, startingWeaponSlot1, false);
            if (startingWeaponSlot2 != null)
                InstallWeaponInSlot(1, startingWeaponSlot2, false);

            int initialSlot = Mathf.Clamp(startEquippedSlot, 0, slots.Length - 1);
            if (!HasWeaponInSlot(initialSlot))
                initialSlot = FindFirstOccupiedSlot();

            if (initialSlot >= 0)
                EquipSlotImmediate(initialSlot);
            else
                PushEmptyUi();
        }

        private void OnDestroy()
        {
            for (int i = 0; i < slots.Length; i++)
                UnsubscribeSlot(slotIndex: i);

#if ENABLE_INPUT_SYSTEM
            DisableInputActions();
#endif
        }

        private void OnDisable()
        {
#if ENABLE_INPUT_SYSTEM
            DisableInputActions();
#endif
        }

        private void Update()
        {
            if (ShouldBlockInputThisFrame())
                return;

            HandleEquipInput();
            HandleCombatInput();
        }

        private void HandleEquipInput()
        {
            if (GetEquipWeapon1Down())
                RequestEquip(0);
            else if (GetEquipWeapon2Down())
                RequestEquip(1);
            else if (GetToggleWeaponDown())
                ToggleWeapon();
        }

        private void HandleCombatInput()
        {
            WeaponController weapon = CurrentWeapon;
            if (weapon == null)
                return;

            if (GetReloadDown())
                weapon.TryStartReload();

            weapon.TickCombatInput(GetFireDown(), GetFireHeld(), GetFireUp());
        }

        public void RequestEquip(int slotIndex)
        {
            if (!IsValidSlot(slotIndex) || !HasWeaponInSlot(slotIndex))
                return;

            WeaponController current = CurrentWeapon;
            if (current == null)
            {
                EquipSlotImmediate(slotIndex);
                return;
            }

            if (slotIndex == equippedSlot)
            {
                // "Jugueteo": if current weapon is being holstered, reverse it.
                if (holsteringWeapon == current)
                {
                    pendingSlot = equippedSlot;
                    holsteringWeapon = null;
                    current.SetEquippedDesired(true);
                }
                return;
            }

            pendingSlot = slotIndex;
            if (holsteringWeapon == current)
                return;

            current.CancelReloadBySwap();
            current.SetEquippedDesired(false);
            holsteringWeapon = current;
        }

        public void OnWeaponHolstered(WeaponController sourceWeapon)
        {
            if (sourceWeapon == null)
                return;
            if (sourceWeapon != holsteringWeapon)
                return;

            holsteringWeapon = null;
            sourceWeapon.gameObject.SetActive(false);

            int target = pendingSlot;
            pendingSlot = -1;

            if (!IsValidSlot(target) || !HasWeaponInSlot(target))
                return;

            EquipSlotImmediate(target);
        }

        public bool AcquireWeapon(WeaponDefinition definition, bool forceReplaceEquipped = false, bool autoEquip = true)
        {
            if (definition == null)
                return false;

            int slotIndex = ResolveSlotForAcquisition(forceReplaceEquipped);
            if (!IsValidSlot(slotIndex))
                return false;

            bool replacingEquipped = slotIndex == equippedSlot && HasWeaponInSlot(slotIndex);
            InstallWeaponInSlot(slotIndex, definition, false);

            if (!autoEquip)
                return true;

            if (replacingEquipped)
            {
                pendingSlot = -1;
                holsteringWeapon = null;
                EquipSlotImmediate(slotIndex);
            }
            else
            {
                RequestEquip(slotIndex);
            }

            return true;
        }

        public bool ReplaceEquippedWeapon(WeaponDefinition definition, bool autoEquip = true)
        {
            return AcquireWeapon(definition, true, autoEquip);
        }

        public void SetGlobalInfiniteMagazinePowerup(bool active)
        {
            globalInfiniteMagazinePowerupActive = active;
            WeaponController weapon = CurrentWeapon;
            if (weapon == null)
                OnAmmoChanged?.Invoke(0, 0);
            else
                OnAmmoChanged?.Invoke(weapon.CurrentAmmo, weapon.ReserveAmmo);
        }

        public void SetExternalFireRateMultiplier(float multiplier)
        {
            externalFireRateMultiplier = Mathf.Max(0.01f, multiplier);
        }

        public void PlaySharedAudioEvent(int eventIndex)
        {
            if (sharedWeaponAudio == null)
                return;
            sharedWeaponAudio.PlayByIndex(eventIndex);
        }

        private void ToggleWeapon()
        {
            if (GetOccupiedSlotCount() != 2 || equippedSlot < 0)
                return;

            int target = equippedSlot == 0 ? 1 : 0;
            RequestEquip(target);
        }

        private void EquipSlotImmediate(int slotIndex)
        {
            if (!IsValidSlot(slotIndex) || !HasWeaponInSlot(slotIndex))
                return;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].controller == null)
                    continue;
                slots[i].controller.gameObject.SetActive(i == slotIndex);
            }

            equippedSlot = slotIndex;
            slots[slotIndex].controller.SetEquippedDesired(true);
            PushCurrentUi();
        }

        private void InstallWeaponInSlot(int slotIndex, WeaponDefinition definition, bool active)
        {
            if (!IsValidSlot(slotIndex))
                return;

            RemoveWeaponFromSlot(slotIndex);
            if (definition == null || definition.weaponPrefab == null)
            {
                Debug.LogError($"[WeaponManager] Invalid definition/prefab for slot {slotIndex}.", this);
                return;
            }

            GameObject weaponObject = Instantiate(definition.weaponPrefab, weaponRoot);
            WeaponController controller = weaponObject.GetComponentInChildren<WeaponController>(true);
            if (controller == null)
            {
                Debug.LogError(
                    $"[WeaponManager] El prefab '{definition.name}' no tiene WeaponController.",
                    this);
                Destroy(weaponObject);
                return;
            }

            slots[slotIndex].definition = definition;
            slots[slotIndex].controller = controller;

            controller.Initialize(this, definition, slotIndex);
            controller.AmmoChanged += OnWeaponAmmoChanged;
            weaponObject.SetActive(active);
            controller.SetEquippedDesired(active);
        }

        private void RemoveWeaponFromSlot(int slotIndex)
        {
            if (!IsValidSlot(slotIndex))
                return;

            WeaponController controller = slots[slotIndex].controller;
            if (controller != null)
            {
                controller.AmmoChanged -= OnWeaponAmmoChanged;
                if (holsteringWeapon == controller)
                    holsteringWeapon = null;
                Destroy(controller.gameObject);
            }

            slots[slotIndex].definition = null;
            slots[slotIndex].controller = null;

            if (equippedSlot == slotIndex)
                equippedSlot = -1;
            if (pendingSlot == slotIndex)
                pendingSlot = -1;
        }

        private void OnWeaponAmmoChanged(WeaponController source, int magazine, int reserve)
        {
            if (source == CurrentWeapon)
                OnAmmoChanged?.Invoke(magazine, reserve);
        }

        private void PushCurrentUi()
        {
            WeaponController current = CurrentWeapon;
            OnWeaponChanged?.Invoke(current, equippedSlot);
            if (current == null)
                OnAmmoChanged?.Invoke(0, 0);
            else
                OnAmmoChanged?.Invoke(current.CurrentAmmo, current.ReserveAmmo);
        }

        private void PushEmptyUi()
        {
            OnWeaponChanged?.Invoke(null, -1);
            OnAmmoChanged?.Invoke(0, 0);
        }

        private void UnsubscribeSlot(int slotIndex)
        {
            if (!IsValidSlot(slotIndex))
                return;
            if (slots[slotIndex].controller != null)
                slots[slotIndex].controller.AmmoChanged -= OnWeaponAmmoChanged;
        }

        private bool HasWeaponInSlot(int slotIndex)
        {
            return IsValidSlot(slotIndex) && slots[slotIndex].HasWeapon;
        }

        private bool IsValidSlot(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < slots.Length;
        }

        private int ResolveSlotForAcquisition(bool forceReplaceEquipped)
        {
            if (forceReplaceEquipped && equippedSlot >= 0)
                return equippedSlot;

            int empty = FindFirstEmptySlot();
            if (empty >= 0)
                return empty;

            return equippedSlot >= 0 ? equippedSlot : 0;
        }

        private int FindFirstEmptySlot()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].HasWeapon)
                    return i;
            }
            return -1;
        }

        private int FindFirstOccupiedSlot()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].HasWeapon)
                    return i;
            }
            return -1;
        }

        private int GetOccupiedSlotCount()
        {
            int count = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].HasWeapon)
                    count++;
            }
            return count;
        }

        private bool GetFireDown()
        {
#if ENABLE_INPUT_SYSTEM
            return fireAction != null && fireAction.WasPressedThisFrame();
#else
            return false;
#endif
        }

        private bool GetFireHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return fireAction != null && fireAction.IsPressed();
#else
            return false;
#endif
        }

        private bool GetFireUp()
        {
#if ENABLE_INPUT_SYSTEM
            return fireAction != null && fireAction.WasReleasedThisFrame();
#else
            return false;
#endif
        }

        private bool GetReloadDown()
        {
#if ENABLE_INPUT_SYSTEM
            return reloadAction != null && reloadAction.WasPressedThisFrame();
#else
            return false;
#endif
        }

        private bool GetEquipWeapon1Down()
        {
#if ENABLE_INPUT_SYSTEM
            return equipWeapon1Action != null && equipWeapon1Action.WasPressedThisFrame();
#else
            return false;
#endif
        }

        private bool GetEquipWeapon2Down()
        {
#if ENABLE_INPUT_SYSTEM
            return equipWeapon2Action != null && equipWeapon2Action.WasPressedThisFrame();
#else
            return false;
#endif
        }

        private bool GetToggleWeaponDown()
        {
#if ENABLE_INPUT_SYSTEM
            return toggleWeaponAction != null && toggleWeaponAction.WasPressedThisFrame();
#else
            return false;
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private bool ResolveInputActions()
        {
            if (inputActionsAsset == null || string.IsNullOrEmpty(gameplayActionMap))
            {
                Debug.LogError("[WeaponManager] Falta InputActionAsset o gameplayActionMap.", this);
                return false;
            }

            InputActionMap map = inputActionsAsset.FindActionMap(gameplayActionMap, false);
            if (map == null)
            {
                Debug.LogError($"[WeaponManager] No existe ActionMap '{gameplayActionMap}' en el InputActionAsset.", this);
                return false;
            }

            fireAction = map.FindAction(fireActionName, false);
            reloadAction = map.FindAction(reloadActionName, false);
            equipWeapon1Action = map.FindAction(equipWeapon1ActionName, false);
            equipWeapon2Action = map.FindAction(equipWeapon2ActionName, false);
            toggleWeaponAction = map.FindAction(toggleWeaponActionName, false);

            if (fireAction == null)
            {
                Debug.LogError($"[WeaponManager] Falta action '{fireActionName}'.", this);
                return false;
            }
            if (reloadAction == null)
            {
                Debug.LogError($"[WeaponManager] Falta action '{reloadActionName}'.", this);
                return false;
            }
            if (equipWeapon1Action == null)
            {
                Debug.LogError($"[WeaponManager] Falta action '{equipWeapon1ActionName}'.", this);
                return false;
            }
            if (equipWeapon2Action == null)
            {
                Debug.LogError($"[WeaponManager] Falta action '{equipWeapon2ActionName}'.", this);
                return false;
            }
            if (toggleWeaponAction == null)
            {
                Debug.LogError($"[WeaponManager] Falta action '{toggleWeaponActionName}'.", this);
                return false;
            }

            return true;
        }

        private void EnableInputActions()
        {
            EnableAction(fireAction);
            EnableAction(reloadAction);
            EnableAction(equipWeapon1Action);
            EnableAction(equipWeapon2Action);
            EnableAction(toggleWeaponAction);
        }

        private void DisableInputActions()
        {
            DisableAction(fireAction);
            DisableAction(reloadAction);
            DisableAction(equipWeapon1Action);
            DisableAction(equipWeapon2Action);
            DisableAction(toggleWeaponAction);
        }

        private static void EnableAction(InputAction action)
        {
            if (action != null && !action.enabled)
                action.Enable();
        }

        private static void DisableAction(InputAction action)
        {
            if (action != null && action.enabled)
                action.Disable();
        }

#endif

        private bool ShouldBlockInputThisFrame()
        {
            if (Time.timeScale <= 0f)
            {
#if ENABLE_INPUT_SYSTEM
                if (!inputsSuppressedByPause)
                {
                    DisableInputActions();
                    inputsSuppressedByPause = true;
                }
#endif
                waitForFireReleaseAfterPause = true;
                return true;
            }

#if ENABLE_INPUT_SYSTEM
            if (inputsSuppressedByPause)
            {
                EnableInputActions();
                inputsSuppressedByPause = false;
                return true; // consume 1 frame post-pausa para evitar inputs en cola.
            }
#endif

            if (waitForFireReleaseAfterPause)
            {
                if (GetFireHeld())
                    return true;
                waitForFireReleaseAfterPause = false;
            }

            return false;
        }

        private bool ValidateWiring()
        {
            bool ok = true;
            if (weaponRoot == null)
            {
                Debug.LogError("[WeaponManager] Falta referencia: weaponRoot.", this);
                ok = false;
            }

#if ENABLE_INPUT_SYSTEM
            if (inputActionsAsset == null)
            {
                Debug.LogError("[WeaponManager] Falta referencia: inputActionsAsset.", this);
                ok = false;
            }
            if (string.IsNullOrWhiteSpace(gameplayActionMap))
            {
                Debug.LogError("[WeaponManager] Falta valor: gameplayActionMap.", this);
                ok = false;
            }
#endif
            return ok;
        }

    }
}
