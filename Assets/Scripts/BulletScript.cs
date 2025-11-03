using UnityEngine;

public class BulletScript : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float damage = 10f;

    void OnCollisionEnter(Collision collision)
    {
        // Check if the collided object has the "Enemy" tag
        if (collision.gameObject.CompareTag("Enemy"))
        {
            // Get the EnemyScript component from the collided object
            EnemyScript enemy = collision.gameObject.GetComponent<EnemyScript>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
            
        }
        
        // Destroy the bullet on any collision
    }
}
