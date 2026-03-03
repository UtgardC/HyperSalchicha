using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("HyperManzana/Player/First Person Camera Rig")]
public class FirstPersonCameraRig : MonoBehaviour
{
    [System.Serializable]
    public struct CameraKickPreset
    {
        public string name;
        public Vector3 position;
        public Vector3 rotation;
    }

    [Header("Referencias")]
    [SerializeField] private Transform wobblePivot;
    [SerializeField] private PlayerControllerAlt controller;

    [Header("Head Bob")]
    [SerializeField] private bool enableHeadBob = true;
    [SerializeField] private float walkFrequency = 1.6f;
    [SerializeField] private float walkAmplitude = 0.035f;
    [Tooltip("Multiplicador de frecuencia al esprintar.")]
    [SerializeField] private float sprintFrequencyMultiplier = 1.4f;
    [Tooltip("Multiplicador de amplitud al esprintar.")]
    [SerializeField] private float sprintAmplitudeMultiplier = 1.6f;
    [SerializeField] private float bobYaw = 0.5f;
    [SerializeField] private float bobPitch = 1.1f;
    [SerializeField] private float bobRoll = 1.4f;
    [SerializeField] private float bobSmoothing = 12f;
    [Header("Idle")]
    [SerializeField] private float idleFrequency = 0.6f;
    [SerializeField] private float idleAmplitude = 0.01f;
    [SerializeField] private float idleYaw = 0.2f;
    [SerializeField] private float idlePitch = 0.3f;
    [SerializeField] private float idleRoll = 0.2f;

    [Header("Jump / Land")]
    [SerializeField] private Vector3 jumpPositionKick = new Vector3(0f, 0.02f, -0.03f);
    [SerializeField] private Vector3 jumpRotationKick = new Vector3(-2f, 0f, 0f);
    [SerializeField] private Vector3 landPositionKick = new Vector3(0f, -0.04f, 0.02f);
    [SerializeField] private Vector3 landRotationKick = new Vector3(3f, 0f, 0f);

    [Header("Impulse")]
    [SerializeField] private float impulseReturnSpeed = 18f;
    [SerializeField] private float maxImpulsePosition = 0.2f;
    [SerializeField] private float maxImpulseRotation = 10f;

    [Header("Strafe Roll")]
    [SerializeField] private float strafeRollStrength = 4f;
    [SerializeField] private float strafeRollMax = 8f;
    [SerializeField] private float strafeRollSmoothing = 12f;
    [SerializeField] private float sprintStrafeRollMultiplier = 1.6f;

    [Header("Kick Presets")]
    [SerializeField] private CameraKickPreset[] kickPresets;

    private Vector3 baseLocalPos;
    private Quaternion baseLocalRot;
    private float bobTimer;
    private bool wasGrounded;

    private Vector3 impulsePos;
    private Vector3 impulseRot;
    private Vector3 bobPos;
    private Vector3 bobRot;
    private float strafeRoll;

    private void Awake()
    {
        if (controller == null)
            controller = GetComponentInParent<PlayerControllerAlt>();

        if (wobblePivot == null)
            wobblePivot = transform;

        baseLocalPos = wobblePivot.localPosition;
        baseLocalRot = wobblePivot.localRotation;
        wasGrounded = controller != null && controller.IsGrounded;
    }

    private void LateUpdate()
    {
        if (controller == null || wobblePivot == null) return;
        if (Time.timeScale <= 0f) return;

        HandleJumpLand();
        ApplyHeadBob(Time.deltaTime);
        DecayImpulse(Time.deltaTime);

        Vector3 targetPos = baseLocalPos + bobPos + impulsePos;
        Quaternion targetRot = baseLocalRot * Quaternion.Euler(bobRot + impulseRot);

        float t = 1f - Mathf.Exp(-bobSmoothing * Time.deltaTime);
        wobblePivot.localPosition = Vector3.Lerp(wobblePivot.localPosition, targetPos, t);
        wobblePivot.localRotation = Quaternion.Slerp(wobblePivot.localRotation, targetRot, t);
    }

    private void HandleJumpLand()
    {
        bool grounded = controller.IsGrounded;
        if (wasGrounded && !grounded)
            AddImpulse(jumpPositionKick, jumpRotationKick);
        else if (!wasGrounded && grounded)
            AddImpulse(landPositionKick, landRotationKick);
        wasGrounded = grounded;
    }

    private void ApplyHeadBob(float dt)
    {
        if (!enableHeadBob)
        {
            bobPos = Vector3.zero;
            bobRot = Vector3.zero;
            return;
        }
        if (!controller.IsGrounded)
        {
            bobPos = Vector3.zero;
            bobRot = Vector3.zero;
            strafeRoll = 0f;
            return;
        }

        float speed = controller.PlanarSpeed;
        float inputAmount = Mathf.Clamp01(controller.MoveInput.magnitude);
        if (speed < 0.1f || inputAmount <= 0f)
        {
            bobTimer += dt * idleFrequency;
            float idleAmp = idleAmplitude;
            bobPos = new Vector3(
                0f,
                Mathf.Sin(bobTimer * 2f) * idleAmp,
                0f
            );
            bobRot = new Vector3(
                Mathf.Sin(bobTimer * 2f) * idlePitch,
                Mathf.Sin(bobTimer) * idleYaw,
                Mathf.Sin(bobTimer) * idleRoll
            );
            UpdateStrafeRoll(dt);
            return;
        }

        float sprintFrequencyFactor = controller.IsSprinting ? sprintFrequencyMultiplier : 1f;
        float sprintAmplitudeFactor = controller.IsSprinting ? sprintAmplitudeMultiplier : 1f;
        bobTimer += dt * walkFrequency * sprintFrequencyFactor;

        float walkAmp = walkAmplitude * sprintAmplitudeFactor * inputAmount;
        bobPos = new Vector3(
            Mathf.Sin(bobTimer * 2f) * walkAmp * 0.5f,
            Mathf.Cos(bobTimer * 4f) * walkAmp,
            0f
        );

        bobRot = new Vector3(
            Mathf.Sin(bobTimer * 4f) * bobPitch,
            Mathf.Sin(bobTimer * 2f) * bobYaw,
            Mathf.Sin(bobTimer * 2f) * bobRoll
        ) * (inputAmount * sprintAmplitudeFactor);

        UpdateStrafeRoll(dt);
    }

    private void UpdateStrafeRoll(float dt)
    {
        if (controller == null)
        {
            strafeRoll = 0f;
            return;
        }

        Vector3 planarVelocity = controller.Velocity;
        planarVelocity.y = 0f;
        float lateralSpeed = Vector3.Dot(controller.transform.right, planarVelocity);

        float maxSpeed = controller.BaseMoveSpeed;
        if (controller.IsSprinting)
            maxSpeed *= controller.SprintSpeedMultiplier;
        maxSpeed = Mathf.Max(0.01f, maxSpeed);

        float lateralNormalized = Mathf.Clamp(lateralSpeed / maxSpeed, -1f, 1f);
        float sprintFactor = controller.IsSprinting ? sprintStrafeRollMultiplier : 1f;
        float target = -lateralNormalized * strafeRollStrength * sprintFactor;
        target = Mathf.Clamp(target, -strafeRollMax, strafeRollMax);

        float t = 1f - Mathf.Exp(-strafeRollSmoothing * dt);
        strafeRoll = Mathf.Lerp(strafeRoll, target, t);
        bobRot.z += strafeRoll;
    }

    private void DecayImpulse(float dt)
    {
        float t = 1f - Mathf.Exp(-impulseReturnSpeed * dt);
        impulsePos = Vector3.Lerp(impulsePos, Vector3.zero, t);
        impulseRot = Vector3.Lerp(impulseRot, Vector3.zero, t);
    }

    public void AddImpulse(Vector3 position, Vector3 rotation)
    {
        impulsePos += position;
        impulseRot += rotation;

        impulsePos = Vector3.ClampMagnitude(impulsePos, maxImpulsePosition);
        impulseRot = Vector3.ClampMagnitude(impulseRot, maxImpulseRotation);
    }

    public void Event_PlayKick(int presetIndex)
    {
        Event_PlayKickScaled(presetIndex, 1f, 1f);
    }

    public void Event_PlayKickScaled(int presetIndex, float positionMultiplier, float rotationMultiplier)
    {
        if (kickPresets == null) return;
        if (presetIndex < 0 || presetIndex >= kickPresets.Length) return;
        var preset = kickPresets[presetIndex];
        AddImpulse(
            preset.position * Mathf.Max(0f, positionMultiplier),
            preset.rotation * Mathf.Max(0f, rotationMultiplier));
    }
}
