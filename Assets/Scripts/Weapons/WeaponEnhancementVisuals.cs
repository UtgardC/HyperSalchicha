using UnityEngine;

namespace HyperSalchicha.Weapons
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HyperSalchicha/Weapons/Weapon Enhancement Visuals")]
    public class WeaponEnhancementVisuals : MonoBehaviour
    {
        [Header("Material Targets")]
        [SerializeField] private Renderer[] materialTargets;
        [SerializeField] private Material quantumMaterial;
        [SerializeField] private Material redQuantumMaterial;
        [SerializeField] private Material heatedMaterial;

        [Header("Heated")]
        [SerializeField] private GameObject heatedSmokeVfx;

        [Header("Overclocked")]
        [SerializeField] private GameObject overclockDialBase;
        [SerializeField] private GameObject overclockHeatedAddon;
        [SerializeField] private Animator overclockDialAnimator;
        [SerializeField] private string quantumDialBool = "QuantumMode";

        private Material[] originalMaterials;

        private void Awake()
        {
            CacheOriginalMaterials();
            Apply(WeaponEnhancementFlags.None);
        }

        private void OnValidate()
        {
            if (materialTargets == null) return;
            if (originalMaterials == null || originalMaterials.Length != materialTargets.Length)
                CacheOriginalMaterials();
        }

        public void ApplyDefinitionOverrides(WeaponDefinition definition)
        {
            if (definition == null || !definition.useEnhancementVisualOverrides || definition.enhancementVisuals == null)
                return;

            var o = definition.enhancementVisuals;

            if (o.quantumMaterial != null)
                quantumMaterial = o.quantumMaterial;
            if (o.quantumOverheatedMaterial != null)
                redQuantumMaterial = o.quantumOverheatedMaterial;
            if (o.heatedMaterial != null)
                heatedMaterial = o.heatedMaterial;

            if (o.overclockDialBase != null)
                overclockDialBase = o.overclockDialBase;
            if (o.heatedSmokeVfx != null)
                heatedSmokeVfx = o.heatedSmokeVfx;
            if (o.overclockHeatedAddon != null)
                overclockHeatedAddon = o.overclockHeatedAddon;
            if (o.overclockDialAnimator != null)
                overclockDialAnimator = o.overclockDialAnimator;
            if (!string.IsNullOrEmpty(o.quantumDialBool))
                quantumDialBool = o.quantumDialBool;
        }

        public void Apply(WeaponEnhancementFlags enhancements)
        {
            bool hasQuantum = (enhancements & WeaponEnhancementFlags.Quantum) != 0;
            bool hasHeated = (enhancements & WeaponEnhancementFlags.Heated) != 0;
            bool hasOverclock = (enhancements & WeaponEnhancementFlags.Overclocked) != 0;

            Material selectedMaterial = null;
            if (hasQuantum && hasHeated && redQuantumMaterial != null)
                selectedMaterial = redQuantumMaterial;
            else if (hasQuantum && quantumMaterial != null)
                selectedMaterial = quantumMaterial;
            else if (hasHeated && heatedMaterial != null)
                selectedMaterial = heatedMaterial;

            ApplyMaterial(selectedMaterial);

            if (heatedSmokeVfx != null)
                heatedSmokeVfx.SetActive(hasHeated);

            if (overclockDialBase != null)
                overclockDialBase.SetActive(hasOverclock);

            if (overclockHeatedAddon != null)
                overclockHeatedAddon.SetActive(hasOverclock && hasHeated);

            if (overclockDialAnimator != null && !string.IsNullOrEmpty(quantumDialBool))
                overclockDialAnimator.SetBool(quantumDialBool, hasOverclock && hasQuantum);
        }

        private void CacheOriginalMaterials()
        {
            if (materialTargets == null)
            {
                originalMaterials = null;
                return;
            }

            originalMaterials = new Material[materialTargets.Length];
            for (int i = 0; i < materialTargets.Length; i++)
            {
                var renderer = materialTargets[i];
                if (renderer == null) continue;
                originalMaterials[i] = renderer.sharedMaterial;
            }
        }

        private void ApplyMaterial(Material materialOverride)
        {
            if (materialTargets == null) return;

            for (int i = 0; i < materialTargets.Length; i++)
            {
                var renderer = materialTargets[i];
                if (renderer == null) continue;

                if (materialOverride != null)
                    renderer.sharedMaterial = materialOverride;
                else if (originalMaterials != null && i < originalMaterials.Length && originalMaterials[i] != null)
                    renderer.sharedMaterial = originalMaterials[i];
            }
        }
    }
}
