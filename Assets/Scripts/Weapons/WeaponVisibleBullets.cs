using UnityEngine;

namespace HyperManzana.Weapons
{
    // Coloca este script en el GameObject del arma que tiene las 6 balas visibles.
    // Asigna en el Inspector los 6 transforms de las balas.
    // Oculta/mostrar por escala local (0 -> oculto, escala original -> visible).
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(10000)]
    [AddComponentMenu("HyperManzana/Weapons/Weapon Visible Bullets (Scale Hide)")]
    public class WeaponVisibleBullets : MonoBehaviour
    {
        [Tooltip("Lista (ordenada) de las balas visibles en el modelo. El índice 0 suele ser la primera en dispararse.")]
        public Transform[] bulletPieces = new Transform[6];

        [Tooltip("Invertir el orden si tu modelo dispara en sentido inverso.")]
        public bool reverseOrder = false;

        [Header("Estado simple (Inspector)")]
        [Tooltip("Capacidad visual de balas en el arma (para mostrar/ocultar).")]
        public int capacity = 6;
        [Tooltip("Munición actual para el visual. Puedes actualizar con SetAmmo().")]
        public int ammo = 6;

        public enum Mode { FollowAmmo, Hidden, Full }

        [Header("Modo visual")]
        public Mode mode = Mode.FollowAmmo;

        private Vector3[] originalScales;

        private void Awake()
        {
            CacheOriginalScales();
        }

        private void OnValidate()
        {
            // Mantener arrays en sincronía al editar
            if (bulletPieces != null)
            {
                if (originalScales == null || originalScales.Length != bulletPieces.Length)
                {
                    CacheOriginalScales();
                }
            }
        }

        private void CacheOriginalScales()
        {
            int n = bulletPieces != null ? bulletPieces.Length : 0;
            originalScales = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                originalScales[i] = bulletPieces[i] != null ? bulletPieces[i].localScale : Vector3.one;
            }
        }

        // Aplica la visibilidad según munición: muestra 'ammo' primeras, oculta el resto.
        public void ApplyCount(int ammo, int capacity)
        {
            if (bulletPieces == null) return;
            int n = bulletPieces.Length;
            if (n == 0) return;

            // Por seguridad, clamp a rango
            ammo = Mathf.Clamp(ammo, 0, capacity);
            int visible = Mathf.Clamp(ammo, 0, n);

            for (int i = 0; i < n; i++)
            {
                int idx = reverseOrder ? (n - 1 - i) : i;
                var t = bulletPieces[idx];
                if (t == null) continue;

                bool show = i < visible;
                t.localScale = show ? originalScales[idx] : Vector3.zero;
            }
        }

        private void LateUpdate()
        {
            switch (mode)
            {
                case Mode.Hidden:
                    HideAll();
                    break;
                case Mode.Full:
                    ApplyCount(capacity, capacity);
                    break;
                default:
                    ApplyCount(ammo, capacity);
                    break;
            }
        }

        // Métodos simples para wiring directo
        public void SetAmmo(int value)
        {
            ammo = Mathf.Clamp(value, 0, Mathf.Max(0, capacity));
            ApplyCount(ammo, capacity);
        }

        public void SetCapacity(int value)
        {
            capacity = Mathf.Max(0, value);
            ammo = Mathf.Clamp(ammo, 0, capacity);
            ApplyCount(ammo, capacity);
        }

        public void FireOne()
        {
            if (ammo <= 0) return;
            SetAmmo(ammo - 1);
        }

        public void HideAll()
        {
            if (bulletPieces == null) return;
            for (int i = 0; i < bulletPieces.Length; i++)
            {
                var t = bulletPieces[i];
                if (t != null) t.localScale = Vector3.zero;
            }
        }

        public void ShowAll()
        {
            if (bulletPieces == null) return;
            for (int i = 0; i < bulletPieces.Length; i++)
            {
                var t = bulletPieces[i];
                if (t != null) t.localScale = originalScales[i];
            }
        }

        // Eventos para Animator (recarga)
        // Llamar al inicio de la recarga, cuando el arma se ve vacía
        public void Event_BulletsHide()
        {
            mode = Mode.Hidden;
        }

        // Llamar en el frame donde aparecen las balas (solo visual)
        public void Event_BulletsShowFull()
        {
            mode = Mode.Full;
        }

        public void Event_BulletsFollowAmmo()
        {
            mode = Mode.FollowAmmo;
        }

        public void FollowAmmoMode()
        {
            mode = Mode.FollowAmmo;
        }
    }
}
