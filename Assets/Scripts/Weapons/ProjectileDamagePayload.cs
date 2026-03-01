using UnityEngine;

namespace HyperManzana.Weapons
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HyperManzana/Weapons/Projectile Damage Payload")]
    public class ProjectileDamagePayload : MonoBehaviour
    {
        [SerializeField] private float damage;

        public float Damage => damage;

        public void SetDamage(float value)
        {
            damage = value;
        }
    }
}
