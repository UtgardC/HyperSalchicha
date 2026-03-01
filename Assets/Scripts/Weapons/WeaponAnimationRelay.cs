using UnityEngine;

namespace HyperManzana.Player
{
    [System.Obsolete("Legacy relay. Use direct animation events on WeaponController and SMB scripts.")]
    [AddComponentMenu("")]
    public sealed class WeaponAnimationRelay : MonoBehaviour
    {
        public void Event_SwapOutFinished() { }
        public void Event_Equipped() { }
        public void Event_ReloadStep() { }
        public void Event_ReloadAnimFinished() { }
        public void Event_UnequipComplete() { }
        public void Event_EquipComplete() { }
        public void Event_ReloadComplete() { }
    }
}
