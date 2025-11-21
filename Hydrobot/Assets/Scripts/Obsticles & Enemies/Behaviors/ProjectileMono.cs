using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ProjectileMono : MonoBehaviour
{
    private Vector2 direction;
    public ProjectileBehavior behavior;
    public MonoBehaviour owner; // Accept any MonoBehaviour as owner

    public void Initialize(Vector2 dir, ProjectileBehavior beh, MonoBehaviour projectileOwner)
    {
        direction = dir.normalized;
        behavior = beh;
        owner = projectileOwner;
    }

    public Vector2 GetDirection() => direction;

    private void FixedUpdate()
    {
        if (behavior != null)
            behavior.Execute(this);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Hit detection for enemies (skip the owner)
        Enemy enemy = collision.gameObject.GetComponent<Enemy>();
        if (enemy != null && owner != null && owner.gameObject != enemy.gameObject)
        {
            behavior?.OnProjectileHit(enemy);
        }

        // Destroy projectile on collision with anything (except other projectiles)
        if (!collision.gameObject.CompareTag("Projectile"))
            Destroy(gameObject);
    }
}
