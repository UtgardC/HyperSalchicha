using UnityEngine;

namespace HyperManzana.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HyperManzana/UI/UI Bar Fill (Right Offset)")]
    public class UIBarFill : MonoBehaviour
    {
        [Tooltip("RectTransform contenedor que define el ancho total de la barra.")]
        [SerializeField] private RectTransform container;

        [Tooltip("RectTransform del fill cuyo 'Right' (offsetMax.x) se ajustará.")]
        [SerializeField] private RectTransform fill;

        // current/max -> t en [0,1]
        public void Set(float current, float max)
        {
            float t = max > 0f ? current / max : 0f;
            SetNormalized(t);
        }

        // t=1 => barra llena, t=0 => vacía
        public void SetNormalized(float t)
        {
            t = Mathf.Clamp01(t);
            float width = container.rect.width;
            float right = width * (1f - t);

            // En el Inspector, "Right" = -offsetMax.x. Para ver valores positivos en Inspector:
            var offMax = fill.offsetMax;
            offMax.x = -right; // signo corregido
            fill.offsetMax = offMax;
        }
    }
}
