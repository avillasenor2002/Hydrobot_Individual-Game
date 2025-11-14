using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ProjectileMono : MonoBehaviour
{
    private Vector2 direction;
    public ProjectileBehavior behavior;
    public Enemy owner;

    public void Initialize(Vector2 dir, ProjectileBehavior beh, Enemy enemyOwner)
    {
        direction = dir.normalized;
        behavior = beh;
        owner = enemyOwner;
    }

    public Vector2 GetDirection() => direction;

    private void FixedUpdate()
    {
        if (behavior != null)
            behavior.Execute(this);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Enemy enemy = collision.gameObject.GetComponent<Enemy>();

        if (enemy != null && enemy != owner) // don’t hit the enemy that fired it
        {
            behavior?.OnProjectileHit(enemy);
        }

        // Destroy projectile on collision with anything (except other projectiles)
        if (!collision.gameObject.CompareTag("Projectile"))
            Destroy(gameObject);
    }
}
