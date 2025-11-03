using UnityEngine;

public class PlayerControllerAlt : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 2f;
    private float currentMoveSpeed;

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

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
        currentMoveSpeed = moveSpeed;
        if (groundCheckPoint == null)
        {
            groundCheckPoint = transform;
        }
    }

    void Update()
    {
        HandleInput();
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

    void HandleInput()
    {
        // Sprint
        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentMoveSpeed = moveSpeed * sprintMultiplier;
        }
        else
        {
            currentMoveSpeed = moveSpeed;
        }

        // Buffer de salto (se registra en Update)
        if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space))
        {
            lastJumpPressedTime = Time.time;
        }
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