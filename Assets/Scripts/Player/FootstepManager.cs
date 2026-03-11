using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("HyperSalchicha/Player/Footstep Manager")]
public class FootstepManager : MonoBehaviour
{
    [Serializable]
    public struct SurfaceAudio
    {
        public string surfaceTag;
        public AudioClip[] footstepClips;
    }

    [Header("References")]
    [SerializeField] private PlayerControllerAlt playerController;
    [SerializeField] private Rigidbody playerRigidbody;
    [SerializeField] private AudioSource templateSource;

    [Header("Surface Library")]
    [SerializeField] private List<SurfaceAudio> surfaceLibrary = new List<SurfaceAudio>();
    [SerializeField] private AudioClip[] defaultFootstepClips;

    [Header("Step Timing")]
    [SerializeField] private float baseStepInterval = 0.6f;
    [SerializeField] private float sprintStepInterval = 0.35f;
    [SerializeField] private float minPlanarSpeed = 0.15f;

    [Header("Configuración de Detección (CheckSphere)")]
    [SerializeField] private float castOffset = -0.6f;
    [SerializeField] private float sphereRadius = 0.35f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Playback")]
    [SerializeField] private int poolSize = 6;
    [SerializeField] private float sprintVolumeMultiplier = 1.15f;
    [SerializeField, Range(0f, 0.2f)] private float pitchRandomness = 0.04f;

    [Header("Landing Sound")]
    [SerializeField] private float minAirTimeForLanding = 0.2f;
    [SerializeField] private float landVolumeMult = 1.2f;
    [SerializeField] private float landPitchMult = 0.95f;

    [Header("Jump Sound")]
    [SerializeField] private float minJumpVerticalVelocity = 0.1f;
    [SerializeField] private float jumpVolumeMult = 1f;
    [SerializeField] private float jumpPitchMult = 1f;

    private AudioSource[] audioPool;
    private int poolIndex;
    private float stepTimer;
    private AudioClip lastClipPlayed;
    private bool wasGroundedLastFrame;
    private float airborneTimer;
    private AudioClip[] cachedGroundFootstepClips;
    private readonly Collider[] groundHitsBuffer = new Collider[16];

    private void Awake()
    {
        if (!ValidateWiring())
        {
            enabled = false;
            return;
        }

        BuildAudioPool();
        wasGroundedLastFrame = IsGroundedBySphere();
        airborneTimer = 0f;
    }

    private void OnValidate()
    {
        baseStepInterval = Mathf.Max(0.01f, baseStepInterval);
        sprintStepInterval = Mathf.Max(0.01f, sprintStepInterval);
        sphereRadius = Mathf.Max(0.05f, sphereRadius);
        minPlanarSpeed = Mathf.Max(0f, minPlanarSpeed);
        poolSize = Mathf.Max(1, poolSize);
        sprintVolumeMultiplier = Mathf.Max(0f, sprintVolumeMultiplier);
        minAirTimeForLanding = Mathf.Max(0f, minAirTimeForLanding);
        landVolumeMult = Mathf.Max(0f, landVolumeMult);
        landPitchMult = Mathf.Max(0.01f, landPitchMult);
        minJumpVerticalVelocity = Mathf.Max(0f, minJumpVerticalVelocity);
        jumpVolumeMult = Mathf.Max(0f, jumpVolumeMult);
        jumpPitchMult = Mathf.Max(0.01f, jumpPitchMult);
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
            return;

        bool isGrounded = IsGroundedBySphere();
        UpdateLandingState(isGrounded, Time.deltaTime);
        TickFootsteps(Time.deltaTime, isGrounded);
    }

    private void TickFootsteps(float deltaTime, bool isGrounded)
    {
        if (!isGrounded)
        {
            stepTimer = 0f;
            return;
        }

        CacheGroundSurfaceJumpClip();

        float planarSpeed = GetPlanarSpeed();
        if (planarSpeed < minPlanarSpeed)
        {
            stepTimer = 0f;
            return;
        }

        float stepInterval = playerController.IsSprinting ? sprintStepInterval : baseStepInterval;
        stepTimer += deltaTime;
        if (stepTimer < stepInterval)
            return;

        stepTimer -= stepInterval;
        TriggerStep(playerController.IsSprinting);
    }

    private void UpdateLandingState(bool isGrounded, float deltaTime)
    {
        if (!isGrounded)
        {
        if (wasGroundedLastFrame && playerRigidbody.linearVelocity.y > minJumpVerticalVelocity)
            TriggerJump();

            airborneTimer += deltaTime;
            wasGroundedLastFrame = false;
            return;
        }

        if (!wasGroundedLastFrame && airborneTimer >= minAirTimeForLanding)
            TriggerLanding();

        airborneTimer = 0f;
        wasGroundedLastFrame = true;
    }

    private void TriggerStep(bool isSprinting)
    {
        AudioClip[] clipSet = ResolveSurfaceFootstepClips();
        if (clipSet == null || clipSet.Length == 0)
            clipSet = defaultFootstepClips;
        if (clipSet == null || clipSet.Length == 0)
            return;

        AudioClip clip = PickStepClip(clipSet);
        if (clip == null)
            return;

        PlayClip(clip, isSprinting);
    }

    private void TriggerLanding()
    {
        AudioClip[] clipSet = ResolveSurfaceFootstepClips();
        if (clipSet == null || clipSet.Length == 0)
            clipSet = defaultFootstepClips;
        if (clipSet == null || clipSet.Length == 0)
            return;

        AudioClip clip = PickStepClip(clipSet);
        if (clip == null)
            return;

        PlayClip(clip, false, landVolumeMult, landPitchMult);
    }

    private void TriggerJump()
    {
        AudioClip[] clipSet = cachedGroundFootstepClips;
        if (clipSet == null || clipSet.Length == 0)
            clipSet = defaultFootstepClips;
        if (clipSet == null || clipSet.Length == 0)
            return;

        AudioClip clip = PickStepClip(clipSet);
        if (clip == null)
            return;

        PlayClip(clip, false, jumpVolumeMult, jumpPitchMult);
    }

    private void CacheGroundSurfaceJumpClip()
    {
        if (TryGetGroundSurface(out SurfaceAudio surface))
            cachedGroundFootstepClips = surface.footstepClips;
    }

    private AudioClip[] ResolveSurfaceFootstepClips()
    {
        if (!TryGetGroundSurface(out SurfaceAudio surface))
            return null;

        return surface.footstepClips;
    }

    private bool TryGetGroundSurface(out SurfaceAudio surface)
    {
        surface = default;

        int hitCount = Physics.OverlapSphereNonAlloc(
            GetSphereCenter(),
            sphereRadius,
            groundHitsBuffer,
            groundMask,
            QueryTriggerInteraction.Ignore);
        if (hitCount <= 0)
            return false;

        for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
        {
            Collider hitCollider = groundHitsBuffer[hitIndex];
            if (hitCollider == null)
                continue;

            for (int i = 0; i < surfaceLibrary.Count; i++)
            {
                SurfaceAudio entry = surfaceLibrary[i];
                if (string.IsNullOrWhiteSpace(entry.surfaceTag))
                    continue;
                if (!hitCollider.CompareTag(entry.surfaceTag))
                    continue;
                surface = entry;
                return true;
            }
        }

        return false;
    }

    private AudioClip PickStepClip(AudioClip[] clips)
    {
        int count = clips.Length;
        if (count == 0)
            return null;
        if (count == 1)
        {
            lastClipPlayed = clips[0];
            return clips[0];
        }

        int index = UnityEngine.Random.Range(0, count);
        if (clips[index] == lastClipPlayed)
            index = (index + 1) % count;

        lastClipPlayed = clips[index];
        return clips[index];
    }

    private void PlayClip(
        AudioClip clip,
        bool isSprinting,
        float volumeMultiplier = 1f,
        float pitchMultiplier = 1f)
    {
        AudioSource source = GetNextSource();
        if (source == null)
            return;

        float pitch = (1f + UnityEngine.Random.Range(-pitchRandomness, pitchRandomness)) * pitchMultiplier;
        float sprintFactor = isSprinting ? sprintVolumeMultiplier : 1f;

        source.clip = clip;
        source.pitch = pitch;
        source.volume = templateSource.volume * sprintFactor * volumeMultiplier;
        source.Play();
    }

    private AudioSource GetNextSource()
    {
        if (audioPool == null || audioPool.Length == 0)
            return null;

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
            selectedIndex = poolIndex;

        poolIndex = (selectedIndex + 1) % audioPool.Length;
        return audioPool[selectedIndex];
    }

    private float GetPlanarSpeed()
    {
        Vector3 v = playerRigidbody.linearVelocity;
        return new Vector2(v.x, v.z).magnitude;
    }

    private void BuildAudioPool()
    {
        audioPool = new AudioSource[poolSize];
        poolIndex = 0;

        for (int i = 0; i < poolSize; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            CopySourceSettings(source, templateSource);
            audioPool[i] = source;
        }
    }

    private static void CopySourceSettings(AudioSource target, AudioSource template)
    {
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

    private bool ValidateWiring()
    {
        bool ok = true;

        if (playerController == null)
        {
            Debug.LogError("[FootstepManager] Falta referencia: playerController.", this);
            ok = false;
        }
        if (playerRigidbody == null)
        {
            Debug.LogError("[FootstepManager] Falta referencia: playerRigidbody.", this);
            ok = false;
        }
        if (templateSource == null)
        {
            Debug.LogError("[FootstepManager] Falta referencia: templateSource.", this);
            ok = false;
        }
        if (groundMask == 0)
        {
            Debug.LogError("[FootstepManager] groundMask está vacío.", this);
            ok = false;
        }

        return ok;
    }

    private Vector3 GetSphereCenter()
    {
        return transform.position + Vector3.up * castOffset;
    }

    private bool IsGroundedBySphere()
    {
        return Physics.CheckSphere(
            GetSphereCenter(),
            sphereRadius,
            groundMask,
            QueryTriggerInteraction.Ignore);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(GetSphereCenter(), sphereRadius);
    }
}
