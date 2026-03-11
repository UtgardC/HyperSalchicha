using System;
using System.Collections.Generic;
using UnityEngine;

namespace HyperSalchicha.Weapons
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HyperSalchicha/Weapons/Weapon Audio Controller")]
    public class WeaponAudioController : MonoBehaviour
    {
        [Serializable]
        public struct WeaponAudioData
        {
            public string eventID;
            public AudioClip clip;
        }

        [Header("Events")]
        [SerializeField] private List<WeaponAudioData> audioEvents = new List<WeaponAudioData>();
        [SerializeField] private AudioClip dryFireClip;

        [Header("Pool")]
        [SerializeField] private AudioSource templateSource;
        [SerializeField] private Transform audioAnchorParent;
        [SerializeField] private int poolSize = 4;
        [SerializeField, Range(0f, 0.2f)] private float pitchRandomness = 0.03f;

        private AudioSource[] audioPool;
        private int poolIndex;
        private GameObject audioAnchor;
        private bool poolInitialized;
        private bool loggedMissingAnchorWarning;

        private void Awake()
        {
            TryInitializePool();
        }

        public void SetAudioAnchorParent(Transform parent)
        {
            audioAnchorParent = parent;
            loggedMissingAnchorWarning = false;

            if (audioAnchor != null && parent != null)
            {
                audioAnchor.transform.SetParent(parent, false);
                audioAnchor.transform.localPosition = Vector3.zero;
                audioAnchor.transform.localRotation = Quaternion.identity;
                return;
            }

            if (!poolInitialized)
                TryInitializePool();
        }

        private bool TryInitializePool()
        {
            if (poolInitialized)
                return true;

            if (templateSource == null)
            {
                Debug.LogError($"[{nameof(WeaponAudioController)}] Missing templateSource reference.", this);
                return false;
            }

            poolSize = Mathf.Max(1, poolSize);
            audioPool = new AudioSource[poolSize];
            poolIndex = 0;

            Transform anchorParent = audioAnchorParent;
            if (anchorParent == null && Camera.main != null)
                anchorParent = Camera.main.transform;
            if (anchorParent == null)
            {
                if (!loggedMissingAnchorWarning)
                {
                    Debug.LogWarning(
                        $"[{nameof(WeaponAudioController)}] Missing audioAnchorParent and no Camera.main found yet.",
                        this);
                    loggedMissingAnchorWarning = true;
                }
                return false;
            }

            audioAnchor = new GameObject(gameObject.name + "_AudioAnchor");
            audioAnchor.transform.SetParent(anchorParent, false);
            audioAnchor.transform.localPosition = Vector3.zero;
            audioAnchor.transform.localRotation = Quaternion.identity;

            for (int i = 0; i < poolSize; i++)
            {
                AudioSource source = audioAnchor.AddComponent<AudioSource>();
                CopySourceSettings(source, templateSource);
                audioPool[i] = source;
            }

            poolInitialized = true;
            loggedMissingAnchorWarning = false;
            return true;
        }

        private void OnDestroy()
        {
            if (audioAnchor != null)
                Destroy(audioAnchor);

            poolInitialized = false;
            audioPool = null;
        }

        public void PlayAnimSound(string eventID)
        {
            if (string.IsNullOrWhiteSpace(eventID) || audioEvents == null)
                return;

            for (int i = 0; i < audioEvents.Count; i++)
            {
                WeaponAudioData data = audioEvents[i];
                if (!string.Equals(data.eventID, eventID, StringComparison.Ordinal))
                    continue;

                PlayClip(data.clip);
                return;
            }
        }

        public void PlayDryFire()
        {
            PlayClip(dryFireClip);
        }

        public void StopAllSounds()
        {
            if (audioPool == null)
                return;

            for (int i = 0; i < audioPool.Length; i++)
            {
                AudioSource source = audioPool[i];
                if (source != null)
                    source.Stop();
            }
        }

        private void PlayClip(AudioClip clip)
        {
            if (clip == null)
                return;
            if (!poolInitialized && !TryInitializePool())
                return;
            if (audioPool == null || audioPool.Length == 0)
                return;

            int selectedIndex = -1;
            for (int i = 0; i < audioPool.Length; i++)
            {
                int candidateIndex = (poolIndex + i) % audioPool.Length;
                AudioSource candidate = audioPool[candidateIndex];
                if (candidate != null && !candidate.isPlaying)
                {
                    selectedIndex = candidateIndex;
                    break;
                }
            }

            if (selectedIndex < 0)
                selectedIndex = poolIndex; // voice stealing when all voices are busy

            AudioSource source = audioPool[selectedIndex];
            if (source == null)
                return;

            source.clip = clip;
            float randomPitch = 1f + UnityEngine.Random.Range(-pitchRandomness, pitchRandomness);
            source.pitch = randomPitch;
            source.Play();

            poolIndex = (selectedIndex + 1) % audioPool.Length;
        }

        private static void CopySourceSettings(AudioSource target, AudioSource template)
        {
            target.clip = template.clip;
            target.volume = template.volume;
            target.pitch = template.pitch;
            target.spatialBlend = template.spatialBlend;
            target.minDistance = template.minDistance;
            target.maxDistance = template.maxDistance;
            target.rolloffMode = template.rolloffMode;

            target.outputAudioMixerGroup = template.outputAudioMixerGroup;
            target.priority = template.priority;
            target.dopplerLevel = template.dopplerLevel;
            target.spread = template.spread;
            target.reverbZoneMix = template.reverbZoneMix;
            target.panStereo = template.panStereo;
            target.playOnAwake = false;
            target.loop = false;
        }
    }
}
