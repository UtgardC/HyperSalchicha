using UnityEngine;

namespace HyperManzana.Player
{

    [AddComponentMenu("HyperManzana/Player/Weapon Animation Relay")]
    public class WeaponAnimationRelay : MonoBehaviour
    {
        private PlayerWeapons playerWeapons;

        private void Awake()
        {
            playerWeapons = GetComponentInParent<PlayerWeapons>();
        }

        public void Event_ReloadComplete()
        {
            if (playerWeapons != null)
                playerWeapons.OnAnimReloadComplete(gameObject);
        }

        public void Event_UnequipComplete()
        {
            if (playerWeapons != null)
                playerWeapons.OnAnimUnequipComplete(gameObject);
        }

        public void Event_EquipComplete()
        {
            if (playerWeapons != null)
                playerWeapons.OnAnimEquipComplete(gameObject);
        }
    }
}

