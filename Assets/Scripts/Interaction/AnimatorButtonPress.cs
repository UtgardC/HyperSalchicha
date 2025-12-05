using UnityEngine;

public class AnimatorButtonPress : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] string trigger = "ButtonPress";

    public void Press() => animator.SetTrigger(trigger);
}
