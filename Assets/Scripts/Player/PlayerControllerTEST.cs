using UnityEngine;

// Este es un script de prueba mínimo para aislar problemas de movimiento y rotación.
//
// Cómo usarlo:
// 1. En tu escena, crea un nuevo objeto primitivo (Ej: una Cápsula) para que sea tu jugador de prueba.
// 2. Arrastra este script (PlayerControllerTEST) a ese objeto Cápsula.
// 3. Añade un componente Rigidbody a la Cápsula (Add Component -> Physics -> Rigidbody).
// 4. Crea una nueva Cámara (GameObject -> Camera) y hazla HIJA de la Cápsula en la jerarquía.
//    - Asegúrate de desactivar tu cámara de jugador original para que no haya conflictos.
//    - Posiciona la nueva cámara hija a la altura de los ojos dentro de la cápsula.
// 5. Selecciona la Cápsula y, en el Inspector, arrastra la cámara hija al campo "Camera Transform" de este script.
// 6. Dale a Play y prueba el movimiento (W,A,S,D) y la rotación con el ratón.

public class PlayerControllerTEST : MonoBehaviour
{
    [Header("Configuración")]
    public float moveSpeed = 8f;
    public float mouseSensitivity = 150f;

    [Header("Asignaciones")]
    public Transform cameraTransform;

    private Rigidbody rb;
    private float xRotation = 0f;

    void Start()
    {
        // Obtener el componente Rigidbody
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("PlayerControllerTEST necesita un componente Rigidbody.");
            enabled = false; // Desactivar el script si no se encuentra el Rigidbody
            return;
        }

        // Bloquear y ocultar el cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // --- Rotación de la Cámara ---
        if (cameraTransform != null)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            // Rotación vertical (Pitch) en la cámara
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            // Rotación horizontal (Yaw) en el cuerpo del jugador
            transform.Rotate(Vector3.up * mouseX);
        }
    }

    void FixedUpdate()
    {
        // --- Movimiento Posicional ---
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // Calcular el vector de movimiento basado en la dirección a la que mira el jugador
        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        Vector3 targetVelocity = move.normalized * moveSpeed;

        // Aplicar la velocidad al Rigidbody, preservando la velocidad vertical (para gravedad, etc.)
        if (rb != null)
        {
            rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
        }
    }
}
