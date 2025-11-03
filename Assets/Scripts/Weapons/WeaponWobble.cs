using UnityEngine;

public class WeaponWooble : MonoBehaviour
{
    [Header("Sway Settings")]
    [SerializeField] private float swayAmount = 0.05f;
    [SerializeField] private float smoothAmount = 5f;
    [SerializeField] private float rotationSwayMultiplier = 30f;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    void Start()
    {
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
    }

    void Update()
    {
        ApplySway();
    }

    void ApplySway()
    {
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * swayAmount;
        float mouseY = Input.GetAxis("Mouse Y") * swayAmount;

        // Calculate position offset
        Vector3 positionOffset = new Vector3(-mouseX, -mouseY, 0f);

        // Calculate rotation offset
        Quaternion rotationOffset = Quaternion.Euler(
            -mouseY * rotationSwayMultiplier, 
            mouseX * rotationSwayMultiplier, 
            mouseX * rotationSwayMultiplier * 0.5f // Optional tilt effect
        );

        // Smoothly interpolate position
        transform.localPosition = Vector3.Lerp(transform.localPosition, initialPosition + positionOffset, Time.deltaTime * smoothAmount);

        // Smoothly interpolate rotation
        transform.localRotation = Quaternion.Slerp(transform.localRotation, initialRotation * rotationOffset, Time.deltaTime * smoothAmount);
    }
}