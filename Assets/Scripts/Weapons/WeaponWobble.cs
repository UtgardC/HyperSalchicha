using UnityEngine;

public class WeaponWooble : MonoBehaviour
{
    [Header("Sway Settings")]
    [SerializeField] private float swayAmount = 0.05f;
    [SerializeField] private float smoothAmount = 5f;
    [SerializeField] private float rotationSwayMultiplier = 30f;
    [Header("Sensitivity Link")]
    [SerializeField] private PlayerControllerAlt playerController;
    [SerializeField] private float sensitivityReference = 100f;
    [SerializeField] private float sensitivityMultiplier = 1f;

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
        float sensitivityFactor = 1f;
        if (playerController != null)
        {
            float reference = Mathf.Max(0.0001f, sensitivityReference);
            float baseFactor = playerController.mouseSensitivity / reference;
            sensitivityFactor = baseFactor * sensitivityMultiplier;
        }

        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * swayAmount * sensitivityFactor;
        float mouseY = Input.GetAxis("Mouse Y") * swayAmount * sensitivityFactor;

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
