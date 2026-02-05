using UnityEngine;

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
    [Tooltip("Porcentaje de control en aire (0-1).")]
    [Range(0f, 1f)]
    [SerializeField] private float airControlPercent = 0.35f;
    [Tooltip("Velocidad mínima base en aire si saltas casi en idle.")]
    [SerializeField] private float minAirSpeed = 2f;

    [Header("Sprint / Stamina")]
    [Tooltip("Usa modo toggle en lugar de mantener la tecla.")]
    [SerializeField] private bool sprintToggleMode = false;
    [Tooltip("Nombre del bot\u00f3n en Input Manager (ej: 'Fire3' o 'Sprint'). Deja vac\u00edo para ignorar.")]
    [SerializeField] private string sprintButton = "Fire3";
    [Tooltip("Tecla directa usada si no hay InputAction configurado.")]
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
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
    [Tooltip("Máscara de capa considerada como suelo")] public LayerMask groundMask = ~0;
    [Tooltip("Longitud del raycast de depuración hacia abajo")] public float groundRayLength = 0.2f;
    public bool isGrounded;

    [Header("Cámara")]
    public float mouseSensitivity = 100f;
    public Transform cameraTransform;

    [Header("Gravedad")]
    public float gravityScale = 1f;

    private Rigidbody rb;

    private bool isSprinting;
    private bool sprintBlockedUntilRelease;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private float yaw;
    private float pitch;

    private bool airVelocityCaptured;
    private Vector3 airVelocityAtJump;
    private float airSpeedAtJump;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
        ClampStamina();
        yaw = transform.eulerAngles.y;
        pitch = cameraTransform != null ? NormalizeAngle(cameraTransform.localEulerAngles.x) : 0f;
        if (groundCheckPoint == null)
        {
            groundCheckPoint = transform;
        }
    }

    void OnValidate()
    {
        ClampStamina();
    }

    void Update()
    {
        ReadInput();
        HandleSprintInput();
        HandleJumpInput();
        UpdateStamina(Time.deltaTime);
        UpdateLook();
    }

    void LateUpdate()
    {
        ApplyCameraPitch();
    }

    void FixedUpdate()
    {
        CheckGround();
        HandleMovement();
        HandleJump();
        ApplyGravity();
        ApplyYawRotation();
    }

    void ReadInput()
    {
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        lookInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
    }

    void UpdateLook()
    {
        float mouseX = lookInput.x * mouseSensitivity * 0.02f;
        float mouseY = lookInput.y * mouseSensitivity * 0.02f;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -90f, 90f);
    }

    void ApplyYawRotation()
    {
        Quaternion target = Quaternion.Euler(0f, yaw, 0f);
        if (rb == null)
        {
            transform.rotation = target;
            return;
        }

        bool freezeYaw = (rb.constraints & RigidbodyConstraints.FreezeRotationY) != 0;
        if (freezeYaw)
            transform.rotation = target;
        else
            rb.MoveRotation(target);
    }

    void ApplyCameraPitch()
    {
        if (cameraTransform == null) return;
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void HandleSprintInput()
    {
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
        if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space))
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
        bool button = !string.IsNullOrEmpty(sprintButton) && Input.GetButtonDown(sprintButton);
        bool key = sprintKey != KeyCode.None && Input.GetKeyDown(sprintKey);
        return button || key;
    }

    bool GetSprintUp()
    {
        bool button = !string.IsNullOrEmpty(sprintButton) && Input.GetButtonUp(sprintButton);
        bool key = sprintKey != KeyCode.None && Input.GetKeyUp(sprintKey);
        return button || key;
    }

    bool GetSprintHeld()
    {
        bool button = !string.IsNullOrEmpty(sprintButton) && Input.GetButton(sprintButton);
        bool key = sprintKey != KeyCode.None && Input.GetKey(sprintKey);
        return button || key;
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

            float inputAmount = Mathf.Clamp01(moveInput.magnitude);
            Vector3 inputVelocity = moveDirection * airSpeedAtJump;
            Vector3 desired = Vector3.Lerp(airVelocityAtJump, inputVelocity, airControlPercent * inputAmount);

            lateralVelocity = Vector3.MoveTowards(lateralVelocity, desired, airAcceleration * Time.fixedDeltaTime);
        }

        rb.linearVelocity = new Vector3(lateralVelocity.x, velocity.y, lateralVelocity.z);
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
        isGrounded = Physics.CheckSphere(origin, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);
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
}
