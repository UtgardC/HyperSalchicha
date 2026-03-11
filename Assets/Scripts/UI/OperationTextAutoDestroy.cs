using UnityEngine;

[AddComponentMenu("HyperSalchicha/UI/Operation Text Auto Destroy")]
public class OperationTextAutoDestroy : MonoBehaviour
{
    [SerializeField] private float lifetime = 2f;

    private void OnEnable()
    {
        Destroy(gameObject, lifetime);
    }
}
