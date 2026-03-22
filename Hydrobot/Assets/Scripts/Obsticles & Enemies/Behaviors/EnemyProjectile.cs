using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileBehavior", menuName = "Enemy Behaviors/Projectile")]
public class ProjectileBehavior : EnemyBehavior
{
    [Header("Movement")]
    public float speed = 6f;
    public float rotationSpeed = 180f;
    public float shrinkAmount = 0.15f;
    public float minimumScaleBeforeDeath = 0.25f;

    [Header("Collision")]
    public string groundTag = "Ground"; // Must match the tag used on ground objects

    // Called by ProjectileMono in FixedUpdate
    public void Execute(ProjectileMono projectile)
    {
        projectile.transform.position += (Vector3)projectile.GetDirection() * speed * Time.fixedDeltaTime;
        projectile.transform.Rotate(0f, 0f, rotationSpeed * Time.fixedDeltaTime);
    }

    // Called by ProjectileMono from OnCollisionEnter2D / OnTriggerEnter2D
    public void OnGroundHit(ProjectileMono projectile, GameObject other)
    {
        if (other.CompareTag(groundTag))
            Object.Destroy(projectile.gameObject);
    }

    // Called when hit by water projectile
    public void OnProjectileHit(Enemy enemy)
    {
        Vector3 scale = enemy.transform.localScale;
        scale -= Vector3.one * shrinkAmount;
        if (scale.x <= minimumScaleBeforeDeath || scale.y <= minimumScaleBeforeDeath)
            enemy.TakeDamage(9999);
        else
            enemy.transform.localScale = scale;
    }

    // Implement abstract Execute(Enemy) to satisfy compiler
    public override void Execute(Enemy enemy)
    {
        // Nothing needed here; handled by ProjectileMono
    }
}