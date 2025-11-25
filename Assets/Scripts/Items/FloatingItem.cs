using UnityEngine;

public class FloatingItem : MonoBehaviour
{
    [Header("Configuración de Rotación")]
    [Tooltip("Velocidad de rotación en los ejes X, Y, Z.")]
    public Vector3 rotationSpeed = new Vector3(0, 50f, 0); // Por defecto rota en el eje Y

    [Header("Configuración de Flotación")]
    [Tooltip("Distancia máxima que se moverá hacia arriba y abajo (Amplitud).")]
    public float floatAmplitude = 0.25f; 

    [Tooltip("Velocidad del ciclo de subida y bajada (Frecuencia).")]
    public float floatFrequency = 1f;

    // Guardamos la posición inicial para flotar alrededor de ella
    private Vector3 startPos;

    void Start()
    {
        // Guardar la posición donde colocaste el objeto en la escena
        startPos = transform.position;
    }

    void Update()
    {
        // 1. Rotación Constante
        // Usamos Space.World o Space.Self dependiendo de si quieres que rote sobre su eje local o global.
        // Por defecto, Rotate usa Space.Self (su propio eje).
        transform.Rotate(rotationSpeed * Time.deltaTime);

        // 2. Movimiento de Flotación (Onda Senoidal)
        // Mathf.Sin crea una onda suave que va de -1 a 1 repetidamente.
        float newY = startPos.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;

        // Actualizamos la posición manteniendo X y Z, cambiando solo Y
        transform.position = new Vector3(startPos.x, newY, startPos.z);
    }
}