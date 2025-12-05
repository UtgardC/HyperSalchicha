using UnityEngine;

public class MenuCameraMouseLook : MonoBehaviour
{
    [Header("Límites de Rotación")]
    public float maxYaw = 10f;    
    public float maxPitch = 10f;  // He igualado esto a 10 por defecto para probar la simetría

    [Header("Ajustes de Sensibilidad")]
    public float sensitivity = 1f;
    
    [Tooltip("Si es true, multiplica el eje Y por el Aspect Ratio (1.77 en 16:9) para que la mano viaje la misma distancia física.")]
    public bool compensateAspectRatio = true;

    [Header("Calidad de Movimiento")]
    [Tooltip("1.0 = Lineal (Suave). Valores altos (>1.5) hacen que el centro sea zona muerta y los bordes rápidos (Brusco).")]
    public float curveFalloff = 1.0f; // Recomendado: 1.0 para suavidad máxima
    public float smoothTime = 0.2f;   // Aumentado levemente para quitar temblores

    private Quaternion _baseRotation;
    private float _yaw, _pitch;
    private float _yawVel, _pitchVel;

    void Start()
    {
        _baseRotation = transform.localRotation;
    }

    void Update()
    {
        Vector3 mouse = Input.mousePosition;

        // 1. Calculamos el centro
        float halfW = Screen.width * 0.5f;
        float halfH = Screen.height * 0.5f;

        // 2. Normalizamos AMBOS ejes respecto al ANCHO (Width).
        // Esto hace que "1 unidad" de movimiento del mouse sean los mismos píxeles 
        // en horizontal que en vertical.
        float nx = (mouse.x - halfW) / halfW; // Rango -1 a 1
        float ny = (mouse.y - halfH) / halfW; // Rango aprox -0.56 a 0.56 (en 16:9)

        // 3. Compensación Visual (Tu intuición del 16/9)
        // Si no hacemos esto, el eje vertical se siente "corto" porque la pantalla se acaba antes.
        // Al multiplicar por el AspectRatio, "estiramos" la lógica vertical para que coincida con la horizontal.
        if (compensateAspectRatio)
        {
            float aspectRatio = (float)Screen.width / Screen.height;
            ny *= aspectRatio; 
        }

        // 4. Limitamos para que no se vuelva loco si sales de la ventana
        nx = Mathf.Clamp(nx, -1f, 1f);
        ny = Mathf.Clamp(ny, -1f, 1f);

        // 5. Aplicamos la curva (Opcional, si curveFalloff = 1, esto es lineal y super suave)
        float curvedX = Mathf.Sign(nx) * Mathf.Pow(Mathf.Abs(nx), curveFalloff);
        float curvedY = Mathf.Sign(ny) * Mathf.Pow(Mathf.Abs(ny), curveFalloff);

        // 6. Calculamos destino
        float targetYaw = curvedX * maxYaw * sensitivity;
        float targetPitch = -curvedY * maxPitch * sensitivity;

        // 7. SmoothDamp para movimiento de "aceite" (sin tirones)
        _yaw = Mathf.SmoothDamp(_yaw, targetYaw, ref _yawVel, smoothTime);
        _pitch = Mathf.SmoothDamp(_pitch, targetPitch, ref _pitchVel, smoothTime);

        // 8. Aplicar
        transform.localRotation = _baseRotation * Quaternion.Euler(_pitch, _yaw, 0f);
    }
}