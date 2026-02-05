using UnityEngine;

public class PlayerControllerAlt : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 2f;
    private float currentMoveSpeed;

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
    [Header("(Opcional) UI")]
    [SerializeField] private HyperManzana.UI.UIBarFill staminaBar;

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
    private float xRotation = 0f;

    [Header("Gravedad")]
    public float gravityScale = 1f;

    private Rigidbody rb;

    private bool isSprinting;
    private bool sprintBlockedUntilRelease;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
        currentMoveSpeed = moveSpeed;
        ClampStamina();
        UpdateStaminaUI();
        if (groundCheckPoint == null)
        {
            groundCheckPoint = transform;
        }
    }

    void OnValidate()
    {
        ClampStamina();
        UpdateStaminaUI();
    }

    void Update()
    {
        HandleSprintInput();
        HandleJumpInput();
        UpdateStamina(Time.deltaTime);
    }

    void LateUpdate()
    {
        HandleCamera();
    }

    void FixedUpdate()
    {
        CheckGround();
        HandleMovement();
        HandleJump();
        ApplyGravity();
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

        currentMoveSpeed = isSprinting ? moveSpeed * sprintMultiplier : moveSpeed;
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
        UpdateStaminaUI();
    }

    void StopSprintFromExhaustion(bool sprintHeld)
    {
        if (!isSprinting) return;

        isSprinting = false;
        currentMoveSpeed = moveSpeed;

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

    void HandleMovement()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        if (x != 0 || z != 0) Debug.Log($"Input de Movimiento: H={x}, V={z}");

        Vector3 moveDirection = transform.TransformDirection(new Vector3(x, 0, z).normalized);
        rb.linearVelocity = new Vector3(moveDirection.x * currentMoveSpeed, rb.linearVelocity.y, moveDirection.z * currentMoveSpeed);
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
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            // consumir el buffer
            lastJumpPressedTime = -999f;
        }
    }

    void HandleCamera()
    {
        if (cameraTransform == null) return;

        // Rotación "forzada" y directa, menos dependiente de la temporización de frames.
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * 0.02f;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * 0.02f;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
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

    void UpdateStaminaUI()
    {
        if (staminaBar != null)
            staminaBar.Set(stamina, maxStamina);
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
