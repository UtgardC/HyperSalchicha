using UnityEngine;

namespace HyperManzana.Weapons
{
    [AddComponentMenu("")]
    public class SMB_WeaponHolstered : StateMachineBehaviour
    {
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (animator == null)
                return;

            WeaponController controller = animator.GetComponentInParent<WeaponController>();
            if (controller != null)
                controller.OnHolsteredStateEntered();
        }
    }
}
