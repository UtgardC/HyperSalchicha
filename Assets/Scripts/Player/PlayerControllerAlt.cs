using UnityEngine;
using HyperManzana.Weapons;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerControllerAlt : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 2f;
    [Tooltip("Aceleración en suelo (m/s²).")]
    [SerializeField] private float groundAcceleration = 40f;
    [Tooltip("Desaceleración en suelo cuando no hay input (m/s²).")]
    [SerializeField] private float groundDeceleration = 60f;
    [Tooltip("Aceleración en aire (m/s²).")]
    [SerializeField] private float airAcceleration = 15f;
    [Tooltip("Velocidad mínima base en aire si saltas casi en idle.")]
    [SerializeField] private float minAirSpeed = 2f;

    [Header("Sprint / Stamina")]
    [Tooltip("Usa modo toggle en lugar de mantener la tecla.")]
    [SerializeField] private bool sprintToggleMode = false;
    [Space]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float stamina = 100f;
    [Tooltip("Consumo de estamina por segundo al esprintar.")]
    [SerializeField] private float sprintDrainPerSecond = 25f;
    [Tooltip("Regeneraci\u00f3n de estamina por segundo cuando no se esprinta.")]
    [SerializeField] private float staminaRegenPerSecond = 20f;
    [Tooltip("Estamina m\u00ednima requerida para poder comenzar a esprintar.")]
    [SerializeField] private float sprintStartThreshold = 10f;
    [Header("Salto")]
    public float jumpForce = 7f;
    [Tooltip("Tiempo que se recuerda el input de salto (segundos)")] public float jumpBufferTime = 0.15f;
    private float lastJumpPressedTime = -999f;

    [Header("Ground Check")]
    [Tooltip("Punto desde el cual se hace el chequeo esférico")] public Transform groundCheckPoint;
    [Tooltip("Radio de la esfera para detectar suelo")] public float groundCheckRadius = 0.3f;
    [Tooltip("Longitud del raycast de depuración hacia abajo")] public float groundRayLength = 0.2f;
    public bool isGrounded;

    [Header("Cámara")]
    public float mouseSensitivity = 100f;
    public Transform cameraTransform;
    [Tooltip("Pivot para el pitch. Si está vacío, se usa cameraTransform.")]
    [SerializeField] private Transform cameraPitchPivot;

    [Header("Gravedad")]
    public float gravityScale = 1f;
    [Header("Input Asset (required)")]
    [SerializeField] private InputActionAsset inputActionsAsset;
    [SerializeField] private string gameplayActionMap = "Gameplay";
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string lookActionName = "Look";
    [SerializeField] private string jumpActionName = "Jump";
    [SerializeField] private string sprintActionName = "Sprint";
    [Header("Dependencias")]
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private Collider playerCollider;

    [Header("Friccion Dinamica")]
    [SerializeField] private PhysicsMaterial movingPhysicMaterial;
    [SerializeField] private PhysicsMaterial restBrakingPhysicMaterial;
    [SerializeField, Range(0f, 0.25f)] private float restInputThreshold = 0.05f;

    private Rigidbody rb;

    private bool isSprinting;
    private bool sprintBlockedUntilRelease;
    private bool usingRestFriction;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private float yaw;
    private float pitch;

    private bool airVelocityCaptured;
    private Vector3 airVelocityAtJump;
    private float airSpeedAtJump;
    private int groundLayerMask;

#if ENABLE_INPUT_SYSTEM
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
#endif

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!TryCacheGroundLayerMask())
        {
            enabled = false;
            return;
        }
        if (!ValidateWiring())
        {
            enabled = false;
            return;
        }
#if ENABLE_INPUT_SYSTEM
        if (!ResolveInputActions())
        {
            enabled = false;
            return;
        }
#else
        Debug.LogError("[PlayerControllerAlt] ENABLE_INPUT_SYSTEM está deshabilitado.", this);
        enabled = false;
        return;
#endif
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        ClampStamina();
        yaw = transform.eulerAngles.y;
        Transform pitchTarget = GetPitchTarget();
        pitch = pitchTarget != null ? NormalizeAngle(pitchTarget.localEulerAngles.x) : 0f;
        if (groundCheckPoint == null)
        {
            groundCheckPoint = transform;
        }

        ApplyDynamicFrictionState(forceRefresh: true);
    }

    void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        EnableInputActions();
#endif
    }

    void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        DisableInputActions();
#endif
    }

    void OnValidate()
    {
        ClampStamina();
    }

    void Update()
    {
        if (Time.timeScale <= 0f)
        {
            moveInput = Vector2.zero;
            lookInput = Vector2.zero;
            return;
        }

        ReadInput();
        HandleSprintInput();
        HandleJumpInput();
        UpdateStamina(Time.deltaTime);
        UpdateLookAndApplyRotation();
    }

    void FixedUpdate()
    {
        CheckGround();
        ApplyDynamicFrictionState(forceRefresh: false);
        HandleMovement();
        HandleJump();
        ApplyGravity();
    }

    void ReadInput()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        lookInput = lookAction.ReadValue<Vector2>();
    }

    void UpdateLookAndApplyRotation()
    {
        float lookScale = mouseSensitivity * 0.02f;
        Vector2 lookDelta = lookInput * lookScale;

        yaw += lookDelta.x;
        pitch -= lookDelta.y;
        pitch = Mathf.Clamp(pitch, -90f, 90f);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        Transform pitchTarget = GetPitchTarget();
        if (pitchTarget != null)
            pitchTarget.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void HandleSprintInput()
    {
        if (weaponManager != null && weaponManager.BlocksSprint)
        {
            isSprinting = false;
            return;
        }

        bool sprintDown = GetSprintDown();
        bool sprintUp = GetSprintUp();
        bool sprintHeld = GetSprintHeld();

        if (sprintBlockedUntilRelease && !sprintHeld)
            sprintBlockedUntilRelease = false;

        if (sprintToggleMode)
        {
            if (sprintDown && !sprintBlockedUntilRelease && stamina >= sprintStartThreshold)
                isSprinting = !isSprinting;
        }
        else
        {
            if (!isSprinting && sprintHeld && !sprintBlockedUntilRelease && stamina >= sprintStartThreshold)
                isSprinting = true;

            if (!sprintHeld)
                isSprinting = false;
        }

        if (sprintUp && sprintBlockedUntilRelease)
            sprintBlockedUntilRelease = false;

    }

    void HandleJumpInput()
    {
        if (GetJumpDown())
        {
            lastJumpPressedTime = Time.time;
        }
    }

    void UpdateStamina(float dt)
    {
        if (isSprinting)
        {
            stamina -= sprintDrainPerSecond * dt;
            if (stamina <= 0f)
            {
                stamina = 0f;
                StopSprintFromExhaustion(GetSprintHeld());
            }
        }
        else
        {
            stamina += staminaRegenPerSecond * dt;
        }

        ClampStamina();
    }

    void StopSprintFromExhaustion(bool sprintHeld)
    {
        if (!isSprinting) return;

        isSprinting = false;

        if (sprintHeld)
        {
            // Evita que el jugador reanude el sprint automÃ¡ticamente hasta soltar la tecla.
            sprintBlockedUntilRelease = true;
        }
    }

    bool GetSprintDown()
    {
        return sprintAction.WasPressedThisFrame();
    }

    bool GetSprintUp()
    {
        return sprintAction.WasReleasedThisFrame();
    }

    bool GetSprintHeld()
    {
        return sprintAction.IsPressed();
    }

    bool GetJumpDown()
    {
        return jumpAction.WasPressedThisFrame();
    }

    Vector3 GetMoveDirection()
    {
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        if (move.sqrMagnitude > 1f) move.Normalize();
        return move;
    }

    void CaptureAirVelocity(Vector3 lateralVelocity, Vector3 moveDirection)
    {
        airVelocityAtJump = lateralVelocity;
        airSpeedAtJump = airVelocityAtJump.magnitude;

        if (airSpeedAtJump < 0.01f)
        {
            float targetSpeed = isSprinting ? moveSpeed * sprintMultiplier : moveSpeed;
            if (moveDirection.sqrMagnitude > 0f)
            {
                airVelocityAtJump = moveDirection * targetSpeed;
                airSpeedAtJump = targetSpeed;
            }
        }

        airSpeedAtJump = Mathf.Max(airSpeedAtJump, minAirSpeed);
        airVelocityCaptured = true;
    }

    Transform GetPitchTarget()
    {
        return cameraPitchPivot != null ? cameraPitchPivot : cameraTransform;
    }

    void HandleMovement()
    {
        Vector3 moveDirection = GetMoveDirection();
        Vector3 velocity = rb.linearVelocity;
        Vector3 lateralVelocity = new Vector3(velocity.x, 0f, velocity.z);

        if (isGrounded)
        {
            airVelocityCaptured = false;
            float targetSpeed = isSprinting ? moveSpeed * sprintMultiplier : moveSpeed;
            Vector3 desired = moveDirection * targetSpeed;

            if (moveDirection.sqrMagnitude > 0f)
                lateralVelocity = Vector3.MoveTowards(lateralVelocity, desired, groundAcceleration * Time.fixedDeltaTime);
            else
                lateralVelocity = Vector3.MoveTowards(lateralVelocity, Vector3.zero, groundDeceleration * Time.fixedDeltaTime);

        }
        else
        {
            if (!airVelocityCaptured)
                CaptureAirVelocity(lateralVelocity, moveDirection);

            if (moveDirection.sqrMagnitude > 0f)
            {
                Vector3 desired = moveDirection * airSpeedAtJump;
                lateralVelocity = Vector3.MoveTowards(lateralVelocity, desired, airAcceleration * Time.fixedDeltaTime);
            }
        }

        rb.linearVelocity = new Vector3(lateralVelocity.x, velocity.y, lateralVelocity.z);
    }

    public bool IsSprinting => isSprinting;
    public bool IsGrounded => isGrounded;
    public Vector2 MoveInput => moveInput;
    public Vector3 Velocity => rb != null ? rb.linearVelocity : Vector3.zero;
    public float BaseMoveSpeed => moveSpeed;
    public float SprintSpeedMultiplier => sprintMultiplier;
    public float PlanarSpeed
    {
        get
        {
            Vector3 v = Velocity;
            return new Vector3(v.x, 0f, v.z).magnitude;
        }
    }

    void ApplyGravity()
    {
        rb.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration);
    }

    void HandleJump()
    {
        // Usa input buffer: si se presionó salto recientemente y hay suelo, salta
        bool jumpBuffered = (Time.time - lastJumpPressedTime) <= jumpBufferTime;
        if (jumpBuffered && isGrounded)
        {
            Vector3 moveDirection = GetMoveDirection();
            Vector3 lateralVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            CaptureAirVelocity(lateralVelocity, moveDirection);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            // consumir el buffer
            lastJumpPressedTime = -999f;
        }
    }

    float NormalizeAngle(float angle)
    {
        if (angle > 180f) angle -= 360f;
        return angle;
    }

    void CheckGround()
    {
        Vector3 origin = groundCheckPoint != null ? groundCheckPoint.position : transform.position;
        // Chequeo esférico contra capas de suelo
        isGrounded = Physics.CheckSphere(origin, groundCheckRadius, groundLayerMask, QueryTriggerInteraction.Ignore);
    }

    bool TryCacheGroundLayerMask()
    {
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer < 0)
        {
            Debug.LogError("[PlayerControllerAlt] No existe la layer 'Ground'.", this);
            return false;
        }

        groundLayerMask = 1 << groundLayer;
        return true;
    }

    void ApplyDynamicFrictionState(bool forceRefresh)
    {
        bool isTryingToMove = moveInput.sqrMagnitude > (restInputThreshold * restInputThreshold);
        bool shouldUseRestFriction = isGrounded && !isTryingToMove;

        if (!forceRefresh && shouldUseRestFriction == usingRestFriction)
            return;

        usingRestFriction = shouldUseRestFriction;
        playerCollider.sharedMaterial = usingRestFriction ? restBrakingPhysicMaterial : movingPhysicMaterial;
    }

    void ClampStamina()
    {
        if (maxStamina < 1f) maxStamina = 1f;
        stamina = Mathf.Clamp(stamina, 0f, maxStamina);
    }

    void OnDrawGizmosSelected()
    {
        // Dibujar ayuda visual de ground check y raycast
        Transform point = groundCheckPoint != null ? groundCheckPoint : transform;
        Vector3 origin = point.position;

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(origin, groundCheckRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + Vector3.down * groundRayLength);
    }

    private bool ValidateWiring()
    {
        bool ok = true;
        if (rb == null)
        {
            Debug.LogError("[PlayerControllerAlt] Falta Rigidbody en el player.", this);
            ok = false;
        }
        if (playerCollider == null)
        {
            Debug.LogError("[PlayerControllerAlt] Falta referencia: playerCollider.", this);
            ok = false;
        }
        if (cameraTransform == null)
        {
            Debug.LogError("[PlayerControllerAlt] Falta referencia: cameraTransform.", this);
            ok = false;
        }
        if (inputActionsAsset == null)
        {
            Debug.LogError("[PlayerControllerAlt] Falta referencia: inputActionsAsset.", this);
            ok = false;
        }
        if (string.IsNullOrWhiteSpace(gameplayActionMap))
        {
            Debug.LogError("[PlayerControllerAlt] Falta valor: gameplayActionMap.", this);
            ok = false;
        }
        if (movingPhysicMaterial == null)
        {
            Debug.LogError("[PlayerControllerAlt] Falta referencia: movingPhysicMaterial.", this);
            ok = false;
        }
        if (restBrakingPhysicMaterial == null)
        {
            Debug.LogError("[PlayerControllerAlt] Falta referencia: restBrakingPhysicMaterial.", this);
            ok = false;
        }
        return ok;
    }

#if ENABLE_INPUT_SYSTEM
    private bool ResolveInputActions()
    {
        InputActionMap map = inputActionsAsset.FindActionMap(gameplayActionMap, false);
        if (map == null)
        {
            Debug.LogError($"[PlayerControllerAlt] No existe ActionMap '{gameplayActionMap}'.", this);
            return false;
        }

        moveAction = map.FindAction(moveActionName, false);
        lookAction = map.FindAction(lookActionName, false);
        jumpAction = map.FindAction(jumpActionName, false);
        sprintAction = map.FindAction(sprintActionName, false);

        if (moveAction == null)
        {
            Debug.LogError($"[PlayerControllerAlt] Falta action '{moveActionName}'.", this);
            return false;
        }
        if (lookAction == null)
        {
            Debug.LogError($"[PlayerControllerAlt] Falta action '{lookActionName}'.", this);
            return false;
        }
        if (jumpAction == null)
        {
            Debug.LogError($"[PlayerControllerAlt] Falta action '{jumpActionName}'.", this);
            return false;
        }
        if (sprintAction == null)
        {
            Debug.LogError($"[PlayerControllerAlt] Falta action '{sprintActionName}'.", this);
            return false;
        }

        return true;
    }

    private void EnableInputActions()
    {
        EnableAction(moveAction);
        EnableAction(lookAction);
        EnableAction(jumpAction);
        EnableAction(sprintAction);
    }

    private void DisableInputActions()
    {
        DisableAction(moveAction);
        DisableAction(lookAction);
        DisableAction(jumpAction);
        DisableAction(sprintAction);
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
}
