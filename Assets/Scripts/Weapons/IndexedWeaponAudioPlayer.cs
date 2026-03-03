using UnityEngine;

namespace HyperManzana.Weapons
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HyperManzana/Weapons/Indexed Weapon Audio Player")]
    public class IndexedWeaponAudioPlayer : MonoBehaviour
    {
        [System.Serializable]
        public struct IndexedAudioEntry
        {
            public string label;
            public AudioSource source;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;
            public Vector2 pitchRange;
        }

        [SerializeField] private IndexedAudioEntry[] entries;
        [SerializeField] private bool logWarnings = true;

        public void PlayByIndex(int index)
        {
            if (entries == null || index < 0 || index >= entries.Length)
                return;

            IndexedAudioEntry entry = entries[index];
            if (entry.source == null || entry.clip == null)
            {
                if (logWarnings)
                    Debug.LogWarning($"[{nameof(IndexedWeaponAudioPlayer)}] Missing source/clip on index {index}.", this);
                return;
            }

            float minPitch = Mathf.Min(entry.pitchRange.x, entry.pitchRange.y);
            float maxPitch = Mathf.Max(entry.pitchRange.x, entry.pitchRange.y);
            entry.source.pitch = Mathf.Approximately(minPitch, maxPitch)
                ? minPitch
                : Random.Range(minPitch, maxPitch);
            entry.source.PlayOneShot(entry.clip, Mathf.Clamp01(entry.volume));
        }
    }
}
