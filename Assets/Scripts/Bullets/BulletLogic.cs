using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class BulletLogic : MonoBehaviour
{
    [SerializeField] float speedThreshold = 10f;   // m/s
    [SerializeField] float slowDestroyDelay = 5f;  // s
    [SerializeField] float fallbackLifetime = 30f; // s

    Rigidbody rb;
    bool done;

    void Awake() => rb = GetComponent<Rigidbody>();

    void OnEnable()
    {
        // Fallback: si NUNCA baja del umbral, destruir a los 30 s
        Invoke(nameof(DestroyNow), fallbackLifetime);
        StartCoroutine(WatchSpeed());
    }

    IEnumerator WatchSpeed()
    {
        float sq = speedThreshold * speedThreshold;

        // Espera hasta que alguna vez baje del umbral
        while (!done && rb && rb.linearVelocity.sqrMagnitude >= sq)
            yield return null;

        if (done) yield break;

        // Empezó el conteo de 5 s desde que bajó del umbral
        yield return new WaitForSeconds(slowDestroyDelay);
        if (done) yield break;

        // Gana la condición "bajó del umbral": cancelamos el fallback de 30 s
        CancelInvoke(nameof(DestroyNow));
        DestroyNow();
    }

    void DestroyNow()
    {
        if (done) return;
        done = true;
        Destroy(gameObject);
    }
}
