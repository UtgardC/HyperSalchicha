using UnityEngine;

public class BulletScript : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float damage = 10f;

    [Header("Hit VFX (optional)")]
    [Tooltip("ParticleSystem hijo para reproducir al impactar un enemigo. Si no se asigna, se buscará en los hijos.")]
    [SerializeField] private ParticleSystem hitParticles;
    [Tooltip("Separar las partículas del proyectil antes de reproducir, para que sigan vivas tras destruir la bala.")]
    [SerializeField] private bool detachParticlesOnHit = true;

    void OnCollisionEnter(Collision collision)
    {
        // Check if the collided object has the "Enemy" tag
        if (collision.gameObject.CompareTag("Enemy"))
        {
            // Deal damage
            var enemy = collision.gameObject.GetComponent<EnemyScript>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }

            // Play hit particles safely (detached), positioned at contact point
            PlayHitParticlesAtCollision(collision);

            // Destroy the bullet after hitting an enemy
            Destroy(gameObject);
            return;
        }

        // If desired, you could also destroy on any collision (uncomment):
        // Destroy(gameObject);
    }

    private void PlayHitParticlesAtCollision(Collision collision)
    {
        var ps = hitParticles != null ? hitParticles : GetComponentInChildren<ParticleSystem>(true);
        if (ps == null) return;

        // Determine impact point and orientation
        Vector3 pos = transform.position;
        Quaternion rot = transform.rotation;
        if (collision.contactCount > 0)
        {
            var contact = collision.GetContact(0);
            pos = contact.point;
            // Orient particles along the surface normal (optional)
            if (contact.normal != Vector3.zero)
                rot = Quaternion.LookRotation(contact.normal);
        }

        if (detachParticlesOnHit)
        {
            ps.transform.SetParent(null, true);
        }

        ps.transform.SetPositionAndRotation(pos, rot);
        ps.gameObject.SetActive(true);
        ps.Play(true);

        // Ensure particles clean up after finishing
        var main = ps.main;
        float maxLifetime = main.startLifetime.constantMax;
        float total = main.duration + maxLifetime + 0.1f;
        Destroy(ps.gameObject, total);
    }
}
