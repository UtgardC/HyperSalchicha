using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

[DisallowMultipleComponent]
[AddComponentMenu("HyperSalchicha/UI/Pause Options Controller")]
public class PauseOptionsController : MonoBehaviour
{
    private const string SensitivityPrefKey = "pause_options_camera_sensitivity";
    private const string VolumePrefKey = "pause_options_master_volume_linear";

    [Header("UI")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private Slider masterVolumeSlider;

    [Header("Gameplay")]
    [SerializeField] private PlayerControllerAlt playerController;
    [Tooltip("Sensibilidad máxima cuando el slider está al tope.")]
    [SerializeField] private float sensitivitySliderMultiplier = 200f;

    [Header("Audio")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string masterVolumeParameter = "MasterVolume";

    [Header("Defaults")]
    [SerializeField] private float defaultSensitivity = 100f;
    [SerializeField] private float defaultMasterVolumeLinear = 1f;
    [SerializeField] private bool loadSavedValues = true;
    private void Awake()
    {
        if (!ValidateWiring())
        {
            enabled = false;
            return;
        }
        InitializeSliderValues();
        ApplySettings();
    }

    public void ApplySettings()
    {
        float mappedSensitivity = SliderToSensitivity(sensitivitySlider.value);
        playerController.mouseSensitivity = mappedSensitivity;
        PlayerPrefs.SetFloat(SensitivityPrefKey, mappedSensitivity);

        float linear = Mathf.Clamp(masterVolumeSlider.value, 0.0001f, 1f);
        float db = Mathf.Log10(linear) * 20f;
        bool applied = audioMixer.SetFloat(masterVolumeParameter, db);
        if (!applied)
        {
            Debug.LogError(
                $"[PauseOptionsController] El parámetro expuesto '{masterVolumeParameter}' no existe en el AudioMixer.",
                this);
        }
        PlayerPrefs.SetFloat(VolumePrefKey, linear);

        PlayerPrefs.Save();
    }

    private void InitializeSliderValues()
    {
        float sensitivity = defaultSensitivity;
        if (playerController != null)
            sensitivity = playerController.mouseSensitivity;
        if (loadSavedValues && PlayerPrefs.HasKey(SensitivityPrefKey))
            sensitivity = PlayerPrefs.GetFloat(SensitivityPrefKey, sensitivity);

        float linearVolume = defaultMasterVolumeLinear;
        if (loadSavedValues && PlayerPrefs.HasKey(VolumePrefKey))
            linearVolume = PlayerPrefs.GetFloat(VolumePrefKey, linearVolume);
        linearVolume = Mathf.Clamp(linearVolume, 0.0001f, 1f);

        sensitivitySlider.SetValueWithoutNotify(SensitivityToSlider(sensitivity));
        masterVolumeSlider.SetValueWithoutNotify(linearVolume);
    }

    private float SliderToSensitivity(float sliderValue)
    {
        float min = sensitivitySlider.minValue;
        float max = sensitivitySlider.maxValue;
        if (Mathf.Approximately(min, max))
            return 0f;

        float normalized = Mathf.InverseLerp(min, max, sliderValue);
        return normalized * Mathf.Max(0f, sensitivitySliderMultiplier);
    }

    private float SensitivityToSlider(float sensitivity)
    {
        float maxSensitivity = Mathf.Max(0.0001f, sensitivitySliderMultiplier);
        float normalized = Mathf.Clamp01(sensitivity / maxSensitivity);
        return Mathf.Lerp(sensitivitySlider.minValue, sensitivitySlider.maxValue, normalized);
    }

    private bool ValidateWiring()
    {
        bool ok = true;
        if (sensitivitySlider == null)
        {
            Debug.LogError("[PauseOptionsController] Falta referencia: sensitivitySlider.", this);
            ok = false;
        }
        if (masterVolumeSlider == null)
        {
            Debug.LogError("[PauseOptionsController] Falta referencia: masterVolumeSlider.", this);
            ok = false;
        }
        if (playerController == null)
        {
            Debug.LogError("[PauseOptionsController] Falta referencia: playerController.", this);
            ok = false;
        }
        if (audioMixer == null)
        {
            Debug.LogError("[PauseOptionsController] Falta referencia: audioMixer.", this);
            ok = false;
        }
        if (string.IsNullOrWhiteSpace(masterVolumeParameter))
        {
            Debug.LogError("[PauseOptionsController] Falta referencia: masterVolumeParameter.", this);
            ok = false;
        }
        return ok;
    }
}
