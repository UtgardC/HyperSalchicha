using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("HyperSalchicha/Interaction/Machine Repair Animation Event Relay")]
public class MachineRepairAnimationEventRelay : MonoBehaviour
{
    [SerializeField] private MachineRepairController target;

    // Call this from an Animation Event on the same GameObject as the Animator.
    public void CompleteRepairSequence()
    {
        if (target != null)
            target.CompleteRepairSequence();
    }
}

