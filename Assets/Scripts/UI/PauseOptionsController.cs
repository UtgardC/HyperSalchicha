using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

[DisallowMultipleComponent]
[AddComponentMenu("HyperManzana/UI/Pause Options Controller")]
public class PauseOptionsController : MonoBehaviour
{
    private const string SensitivityPrefKey = "pause_options_camera_sensitivity";
    private const string VolumePrefKey = "pause_options_master_volume_linear";

    [Header("UI")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private Slider masterVolumeSlider;

    [Header("Gameplay")]
    [SerializeField] private PlayerControllerAlt playerController;

    [Header("Audio")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string masterVolumeParameter = "MasterVolume";

    [Header("Defaults")]
    [SerializeField] private float defaultSensitivity = 100f;
    [SerializeField] private float defaultMasterVolumeLinear = 1f;
    [SerializeField] private bool loadSavedValues = true;

    private void Awake()
    {
        if (playerController == null)
            playerController = FindAnyObjectByType<PlayerControllerAlt>();

        InitializeSliderValues();
    }

    public void ApplySettings()
    {
        if (playerController == null)
            playerController = FindAnyObjectByType<PlayerControllerAlt>();

        if (sensitivitySlider != null && playerController != null)
        {
            playerController.mouseSensitivity = sensitivitySlider.value;
            PlayerPrefs.SetFloat(SensitivityPrefKey, sensitivitySlider.value);
        }

        if (masterVolumeSlider != null)
        {
            float linear = Mathf.Clamp(masterVolumeSlider.value, 0.0001f, 1f);
            if (audioMixer != null && !string.IsNullOrEmpty(masterVolumeParameter))
            {
                float db = Mathf.Log10(linear) * 20f;
                audioMixer.SetFloat(masterVolumeParameter, db);
            }

            PlayerPrefs.SetFloat(VolumePrefKey, linear);
        }

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

        if (sensitivitySlider != null)
            sensitivitySlider.SetValueWithoutNotify(sensitivity);
        if (masterVolumeSlider != null)
            masterVolumeSlider.SetValueWithoutNotify(linearVolume);
    }
}
