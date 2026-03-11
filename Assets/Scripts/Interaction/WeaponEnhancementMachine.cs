using HyperSalchicha.Weapons;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[RequireComponent(typeof(Interactable))]
[AddComponentMenu("HyperSalchicha/Interaction/Weapon Enhancement Machine")]
public class WeaponEnhancementMachine : MonoBehaviour
{
    private enum MachineState
    {
        Idle = 0,
        Processing = 1,
        PickupReady = 2
    }

    [Header("Enhancement")]
    [SerializeField] private WeaponEnhancementType grantedEnhancement = WeaponEnhancementType.Quantum;
    [SerializeField] private int enhancementCost = 20000;
    [SerializeField] private bool startOperational = true;

    [Header("Display")]
    [SerializeField] private Transform displayAnchor;

    [Header("Animation")]
    [SerializeField] private Animator machineAnimator;
    [SerializeField] private string processTrigger = "Process";

    [Header("Prompt")]
    [SerializeField] private string availablePromptFormat = "Pulsa E para volver tu arma {0} [Coste: {1} cuajos]";
    [SerializeField] private string alreadyEnhancedPromptFormat = "Tu arma ya tiene {0}";
    [SerializeField] private string noWeaponPromptText = "Necesitas un arma equipada";
    [SerializeField] private string insufficientFundsPromptFormat = "No tienes suficientes cuajos para {0} [Coste: {1} cuajos]";
    [SerializeField] private string processingPromptText = "Procesando arma...";
    [SerializeField] private string pickupPromptText = "Pulsa E para recoger tu arma";
    [SerializeField] private string unavailablePromptText = "Fuera de servicio";

    [Header("Events")]
    [SerializeField] private UnityEvent onProcessStarted = new UnityEvent();
    [SerializeField] private UnityEvent onDisplayWeaponEnhanced = new UnityEvent();
    [SerializeField] private UnityEvent onProcessingFinished = new UnityEvent();
    [SerializeField] private UnityEvent onWeaponCollected = new UnityEvent();

    [SerializeField] private WeaponManager weaponManager;

    private Interactable interactable;
    private MachineState state = MachineState.Idle;
    private bool isOperational;
    private WeaponDefinition storedWeaponDefinition;
    private WeaponEnhancementFlags storedEnhancements;
    private bool displayEnhancementApplied;
    private GameObject displayedWeaponInstance;
    private WeaponEnhancementVisuals displayedWeaponVisuals;

    private void Awake()
    {
        interactable = GetComponent<Interactable>();
        isOperational = startOperational;
        RefreshPrompt();
    }

    private void Update()
    {
        RefreshPrompt();
    }

    public void TryInteract()
    {
        if (state == MachineState.Processing)
            return;

        if (state == MachineState.PickupReady)
        {
            TryCollectEnhancedWeapon();
            return;
        }

        if (!isOperational)
            return;

        TryStartProcessing();
    }

    public void ApplyEnhancementToDisplayedWeapon()
    {
        if (state != MachineState.Processing || displayEnhancementApplied)
            return;

        displayEnhancementApplied = true;
        ApplyEnhancementsToDisplay(GetCompletedEnhancements());
        onDisplayWeaponEnhanced.Invoke();
    }

    public void CompleteEnhancementProcess()
    {
        if (state != MachineState.Processing)
            return;

        if (!displayEnhancementApplied)
            ApplyEnhancementsToDisplay(GetCompletedEnhancements());

        displayEnhancementApplied = true;
        state = MachineState.PickupReady;
        onProcessingFinished.Invoke();
        RefreshPrompt();
    }

    public void SetOperational(bool operational)
    {
        isOperational = operational;
        RefreshPrompt();
    }

    public void ActivateMachine()
    {
        SetOperational(true);
    }

    public void DeactivateMachine()
    {
        SetOperational(false);
    }

    private void TryStartProcessing()
    {
        WeaponManager manager = ResolveWeaponManager();
        WeaponController currentWeapon = manager != null ? manager.CurrentWeapon : null;
        if (currentWeapon == null || currentWeapon.Definition == null)
            return;

        WeaponEnhancementFlags grantedEnhancementFlag = GetGrantedEnhancementFlag();
        if (currentWeapon.HasEnhancement(grantedEnhancementFlag))
            return;

        int cost = Mathf.Max(0, enhancementCost);
        if (cost > 0)
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("[WeaponEnhancementMachine] GameManager.Instance es null.", this);
                return;
            }

            if (GameManager.Instance.cuajosActuales < cost)
                return;
        }

        WeaponEnhancementFlags currentEnhancements = currentWeapon.ActiveEnhancements;
        WeaponDefinition removedWeapon = manager.RemoveCurrentWeapon();
        if (removedWeapon == null)
            return;

        if (cost > 0)
            GameManager.Instance.SubtractCuajos(cost);

        storedWeaponDefinition = removedWeapon;
        storedEnhancements = currentEnhancements;
        displayEnhancementApplied = false;

        SpawnDisplayWeapon(storedWeaponDefinition, storedEnhancements);

        state = MachineState.Processing;
        onProcessStarted.Invoke();

        if (machineAnimator != null && !string.IsNullOrWhiteSpace(processTrigger))
            machineAnimator.SetTrigger(processTrigger);

        RefreshPrompt();
    }

    private void TryCollectEnhancedWeapon()
    {
        if (storedWeaponDefinition == null)
            return;

        WeaponManager manager = ResolveWeaponManager();
        if (manager == null)
        {
            Debug.LogWarning("[WeaponEnhancementMachine] No se encontro WeaponManager.", this);
            return;
        }

        if (!manager.AcquireWeaponWithEnhancements(storedWeaponDefinition, GetCompletedEnhancements(), false, true))
            return;

        ClearDisplayedWeapon();
        storedWeaponDefinition = null;
        storedEnhancements = WeaponEnhancementFlags.None;
        displayEnhancementApplied = false;
        state = MachineState.Idle;

        onWeaponCollected.Invoke();
        RefreshPrompt();
    }

    private void SpawnDisplayWeapon(WeaponDefinition definition, WeaponEnhancementFlags enhancements)
    {
        ClearDisplayedWeapon();

        GameObject displayPrefab = ResolveDisplayPrefab(definition);
        if (displayPrefab == null)
            return;

        Transform parent = displayAnchor != null ? displayAnchor : transform;
        displayedWeaponInstance = Instantiate(displayPrefab, parent);
        displayedWeaponInstance.transform.localPosition = Vector3.zero;
        displayedWeaponInstance.transform.localRotation = Quaternion.identity;
        displayedWeaponInstance.transform.localScale = Vector3.one;

        PrepareDisplayInstance(displayedWeaponInstance);
        ApplyEnhancementsToDisplay(enhancements);
    }

    private void PrepareDisplayInstance(GameObject displayInstance)
    {
        displayedWeaponVisuals = null;
        if (displayInstance == null)
            return;

        WeaponController[] controllers = displayInstance.GetComponentsInChildren<WeaponController>(true);
        for (int i = 0; i < controllers.Length; i++)
            controllers[i].enabled = false;

        Collider[] colliders = displayInstance.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;

        Rigidbody[] rigidbodies = displayInstance.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            rigidbodies[i].linearVelocity = Vector3.zero;
            rigidbodies[i].angularVelocity = Vector3.zero;
            rigidbodies[i].isKinematic = true;
            rigidbodies[i].detectCollisions = false;
        }

        WeaponView view = displayInstance.GetComponentInChildren<WeaponView>(true);
        if (view != null)
        {
            view.CacheMissingReferences();
            displayedWeaponVisuals = view.EnhancementVisuals;
        }

        if (displayedWeaponVisuals == null)
            displayedWeaponVisuals = displayInstance.GetComponentInChildren<WeaponEnhancementVisuals>(true);
    }

    private void ApplyEnhancementsToDisplay(WeaponEnhancementFlags enhancements)
    {
        if (displayedWeaponVisuals == null)
            return;

        displayedWeaponVisuals.ApplyDefinitionOverrides(storedWeaponDefinition);
        displayedWeaponVisuals.Apply(enhancements);
    }

    private void ClearDisplayedWeapon()
    {
        displayedWeaponVisuals = null;
        if (displayedWeaponInstance != null)
            Destroy(displayedWeaponInstance);

        displayedWeaponInstance = null;
    }

    private void RefreshPrompt()
    {
        if (interactable == null)
            return;

        interactable.promptText = BuildPromptText();
    }

    private string BuildPromptText()
    {
        if (state == MachineState.Processing)
            return processingPromptText;

        if (state == MachineState.PickupReady)
            return pickupPromptText;

        if (!isOperational)
            return unavailablePromptText;

        string enhancementName = GetEnhancementLabel();
        WeaponController currentWeapon = ResolveCurrentWeapon();
        if (currentWeapon == null || currentWeapon.Definition == null)
            return noWeaponPromptText;

        if (currentWeapon.HasEnhancement(GetGrantedEnhancementFlag()))
            return string.Format(alreadyEnhancedPromptFormat, enhancementName);

        int cost = Mathf.Max(0, enhancementCost);
        if (GameManager.Instance != null && GameManager.Instance.cuajosActuales < cost)
            return string.Format(insufficientFundsPromptFormat, enhancementName, cost);

        return string.Format(availablePromptFormat, enhancementName, cost);
    }

    private WeaponController ResolveCurrentWeapon()
    {
        WeaponManager manager = ResolveWeaponManager();
        return manager != null ? manager.CurrentWeapon : null;
    }

    private WeaponManager ResolveWeaponManager()
    {
        if (weaponManager == null)
            weaponManager = FindFirstObjectByType<WeaponManager>();

        return weaponManager;
    }

    private WeaponEnhancementFlags GetCompletedEnhancements()
    {
        return storedEnhancements | GetGrantedEnhancementFlag();
    }

    private string GetEnhancementLabel()
    {
        switch (grantedEnhancement)
        {
            case WeaponEnhancementType.Quantum:
                return "QUANTUM";
            case WeaponEnhancementType.Heated:
                return "HEATED";
            case WeaponEnhancementType.Overclocked:
                return "OVERCLOCKED";
            default:
                return grantedEnhancement.ToString().ToUpperInvariant();
        }
    }

    private WeaponEnhancementFlags GetGrantedEnhancementFlag()
    {
        return (WeaponEnhancementFlags)grantedEnhancement;
    }

    private static GameObject ResolveDisplayPrefab(WeaponDefinition definition)
    {
        if (definition == null)
            return null;

        return definition.visualModel != null ? definition.visualModel : definition.weaponPrefab;
    }
}
