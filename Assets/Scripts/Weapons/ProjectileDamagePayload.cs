using UnityEngine;

namespace HyperSalchicha.Weapons
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HyperSalchicha/Weapons/Projectile Damage Payload")]
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
