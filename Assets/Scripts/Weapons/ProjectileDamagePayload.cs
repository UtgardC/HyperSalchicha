using UnityEngine;

namespace HyperSalchicha.Weapons
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HyperSalchicha/Weapons/Projectile Damage Payload")]
    public class ProjectileDamagePayload : MonoBehaviour
    {
        [SerializeField] private float damage;
        [SerializeField] private int hitReward;

        public float Damage => damage;
        public int HitReward => hitReward;

        public void SetDamage(float value)
        {
            damage = value;
        }

        public void SetHitReward(int value)
        {
            hitReward = Mathf.Max(0, value);
        }
    }
}
