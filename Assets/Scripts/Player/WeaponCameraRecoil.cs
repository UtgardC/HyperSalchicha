using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("HyperManzana/Player/Weapon Camera Recoil")]
[DefaultExecutionOrder(200)]
public class WeaponCameraRecoil : MonoBehaviour
{
    [System.Serializable]
    public struct RecoilKickPreset
    {
        public string name;
        public Vector3 position;
        public Vector3 rotation;
        [Tooltip("Tiempo (segundos) que tarda en volver a cero. Permite estirar el kick.")]
        public float returnSeconds;
    }

    [Header("References")]
    [SerializeField] private Transform recoilPivot;

    [Header("Presets")]
    [SerializeField] private RecoilKickPreset[] kickPresets;

    [Header("Defaults")]
    [SerializeField] private float defaultReturnSeconds = 0.12f;
    [SerializeField] private float minReturnSeconds = 0.02f;

    private Vector3 baseLocalPos;
    private Quaternion baseLocalRot;

    private Vector3 recoilPos;
    private Vector3 recoilRot;
    private Vector3 recoilPosVelocity;
    private Vector3 recoilRotVelocity;
    private float activeReturnSeconds;

    private void Awake()
    {
        if (recoilPivot == null)
            recoilPivot = transform;

        baseLocalPos = recoilPivot.localPosition;
        baseLocalRot = recoilPivot.localRotation;
        activeReturnSeconds = Mathf.Max(minReturnSeconds, defaultReturnSeconds);
    }

    private void LateUpdate()
    {
        if (recoilPivot == null)
            return;
        if (Time.timeScale <= 0f)
            return;

        float returnTime = Mathf.Max(minReturnSeconds, activeReturnSeconds);
        recoilPos = Vector3.SmoothDamp(recoilPos, Vector3.zero, ref recoilPosVelocity, returnTime);
        recoilRot = Vector3.SmoothDamp(recoilRot, Vector3.zero, ref recoilRotVelocity, returnTime);

        recoilPivot.localPosition = baseLocalPos + recoilPos;
        recoilPivot.localRotation = baseLocalRot * Quaternion.Euler(recoilRot);
    }

    public void Event_PlayKick(int presetIndex)
    {
        Event_PlayKickScaled(presetIndex, 1f, 1f, 1f);
    }

    public void Event_PlayKickScaled(int presetIndex, float positionMultiplier, float rotationMultiplier, float durationMultiplier)
    {
        if (kickPresets == null)
            return;
        if (presetIndex < 0 || presetIndex >= kickPresets.Length)
            return;

        var preset = kickPresets[presetIndex];
        float posMul = Mathf.Max(0f, positionMultiplier);
        float rotMul = Mathf.Max(0f, rotationMultiplier);
        float durMul = Mathf.Max(0.01f, durationMultiplier);

        // Aditivo: cada disparo se suma al residuo del anterior.
        recoilPos += preset.position * posMul;
        recoilRot += preset.rotation * rotMul;

        float presetReturn = preset.returnSeconds > 0f ? preset.returnSeconds : defaultReturnSeconds;
        activeReturnSeconds = Mathf.Max(activeReturnSeconds, Mathf.Max(minReturnSeconds, presetReturn * durMul));
    }

    public void ResetRecoilImmediate()
    {
        recoilPos = Vector3.zero;
        recoilRot = Vector3.zero;
        recoilPosVelocity = Vector3.zero;
        recoilRotVelocity = Vector3.zero;

        if (recoilPivot != null)
        {
            recoilPivot.localPosition = baseLocalPos;
            recoilPivot.localRotation = baseLocalRot;
        }
    }
}
