using UnityEngine;

[RequireComponent(typeof(Enemy))]
[RequireComponent(typeof(Collider2D))]
public class EnemySplitOnDeath : MonoBehaviour
{
    [Header("Split Settings")]
    public GameObject splitPrefab;
    public int minSpawnCount = 2;
    public int maxSpawnCount = 3;
    public float scaleMultiplier = 0.6f;

    [Header("Spawned Enemy HP & Speed")]
    public int spawnedEnemyStartingHP = 3;      // Base HP for first spawned enemies
    public int hpReductionPerSpawn = 1;         // Amount to reduce after each spawn group
    public float speedIncreasePerSpawn = 0.25f; // Amount to increase moveSpeed for each spawn group

    [Header("Movement")]
    public float moveSpeed = 3f;
    public LayerMask bounceLayers;

    [Header("Rotation")]
    public float rotationSpeed = 180f;          // degrees per second (constant spin)

    private Enemy enemy;
    private bool hasSplit = false;

    private Rigidbody2D rb;
    private Vector2 moveDirection;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();

        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        moveDirection = GetRandomDiagonal();
        rb.velocity = moveDirection * moveSpeed;
    }

    private void OnEnable()
    {
        if (enemy != null)
            enemy.OnEnemyDeath += HandleEnemyDeath;
    }

    private void OnDisable()
    {
        if (enemy != null)
            enemy.OnEnemyDeath -= HandleEnemyDeath;
    }

    private void FixedUpdate()
    {
        rb.velocity = moveDirection * moveSpeed;
    }

    private void Update()
    {
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if ((bounceLayers.value & (1 << collision.gameObject.layer)) == 0)
            return;

        Vector2 normal = collision.contacts[0].normal;
        moveDirection = Vector2.Reflect(moveDirection, normal);
        moveDirection = SnapToDiagonal(moveDirection);
        rb.velocity = moveDirection * moveSpeed;
    }

    private void HandleEnemyDeath(Enemy deadEnemy)
    {
        if (hasSplit) return;
        hasSplit = true;

        if (splitPrefab == null) return;
        if (spawnedEnemyStartingHP <= 0) return; // Do not spawn if HP <= 0

        int spawnCount = Random.Range(minSpawnCount, maxSpawnCount + 1);
        int currentHP = spawnedEnemyStartingHP;
        float currentSpeed = moveSpeed;

        for (int i = 0; i < spawnCount; i++)
        {
            GameObject spawned = Instantiate(splitPrefab, deadEnemy.transform.position, Quaternion.identity);
            spawned.transform.localScale *= scaleMultiplier;

            Enemy spawnedEnemy = spawned.GetComponent<Enemy>();
            float finalMoveSpeed = currentSpeed; // local default

            if (spawnedEnemy != null)
            {
                spawnedEnemy.isDead = false;
                spawnedEnemy.isInvincible = false;
                spawnedEnemy.invincibilityTimer = 0f;

                spawnedEnemy.health = currentHP;

                EnemySplitOnDeath splitScript = spawned.GetComponent<EnemySplitOnDeath>();
                if (splitScript != null)
                {
                    splitScript.spawnedEnemyStartingHP = currentHP - hpReductionPerSpawn;
                    if (splitScript.spawnedEnemyStartingHP < 0)
                        splitScript.spawnedEnemyStartingHP = 0;

                    splitScript.hpReductionPerSpawn = hpReductionPerSpawn;
                    splitScript.moveSpeed = currentSpeed + speedIncreasePerSpawn;

                    finalMoveSpeed = splitScript.moveSpeed;
                }
            }

            SpriteRenderer[] renderers = spawned.GetComponentsInChildren<SpriteRenderer>();
            foreach (var r in renderers)
                r.enabled = true;

            Rigidbody2D spawnedRb = spawned.GetComponent<Rigidbody2D>();
            if (spawnedRb == null)
                spawnedRb = spawned.AddComponent<Rigidbody2D>();
            spawnedRb.gravityScale = 0f;
            spawnedRb.freezeRotation = true;
            spawnedRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            Vector2 dir = GetRandomDiagonal();
            spawnedRb.velocity = dir * finalMoveSpeed;

            EnemySplitRotation rot = spawned.GetComponent<EnemySplitRotation>();
            if (rot == null)
                rot = spawned.AddComponent<EnemySplitRotation>();
            rot.rotationSpeed = rotationSpeed;

            EnemySplitBounce bounce = spawned.GetComponent<EnemySplitBounce>();
            if (bounce == null)
            {
                bounce = spawned.AddComponent<EnemySplitBounce>();
                bounce.moveSpeed = finalMoveSpeed;
                bounce.bounceLayers = bounceLayers;
            }
        }

        spawnedEnemyStartingHP -= hpReductionPerSpawn;
        if (spawnedEnemyStartingHP < 0)
            spawnedEnemyStartingHP = 0;

        moveSpeed += speedIncreasePerSpawn;
    }

    private Vector2 GetRandomDiagonal()
    {
        Vector2[] diagonals =
        {
            new Vector2( 1,  1),
            new Vector2(-1,  1),
            new Vector2( 1, -1),
            new Vector2(-1, -1)
        };

        return diagonals[Random.Range(0, diagonals.Length)].normalized;
    }

    private Vector2 SnapToDiagonal(Vector2 input)
    {
        input.Normalize();
        float x = Mathf.Sign(input.x);
        float y = Mathf.Sign(input.y);

        if (Mathf.Abs(input.x) < 0.1f)
            x = Random.value > 0.5f ? 1f : -1f;
        if (Mathf.Abs(input.y) < 0.1f)
            y = Random.value > 0.5f ? 1f : -1f;

        return new Vector2(x, y).normalized;
    }
}

// =========================
// SPAWNED ENEMY HELPER SCRIPTS
// =========================
public class EnemySplitRotation : MonoBehaviour
{
    public float rotationSpeed = 180f;
    private void Update()
    {
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }
}

[RequireComponent(typeof(Rigidbody2D))]
public class EnemySplitBounce : MonoBehaviour
{
    public float moveSpeed = 3f;
    public LayerMask bounceLayers;

    private Rigidbody2D rb;
    private Vector2 moveDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        moveDirection = rb.velocity.normalized;
    }

    private void FixedUpdate()
    {
        rb.velocity = moveDirection * moveSpeed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if ((bounceLayers.value & (1 << collision.gameObject.layer)) == 0) return;

        Vector2 normal = collision.contacts[0].normal;
        moveDirection = Vector2.Reflect(moveDirection, normal).normalized;
    }
}
