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

    [Header("HP Visual Tiers")]
    [Tooltip("If HP is below this value, the low-HP visual is used.")]
    public int lowHPThreshold = 2;

    [Tooltip("If HP is below this value, the critical visual is used instead. Must be lower than lowHPThreshold.")]
    public int criticalHPThreshold = 1;

    [Tooltip("Shown when HP is at or above lowHPThreshold. Usually the healthy body.")]
    public GameObject normalVisual;

    [Tooltip("Shown when HP is below lowHPThreshold but at or above criticalHPThreshold.")]
    public GameObject lowHPVisual;

    [Tooltip("Shown when HP is below criticalHPThreshold. Usually the most damaged variant.")]
    public GameObject criticalVisual;

    [Tooltip("If true, ApplyHPVisual is called on Start using the enemy's current HP. " +
             "Useful for level-placed enemies that begin with reduced HP.")]
    public bool applyVisualOnStart = true;

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

    private void Start()
    {
        // Make sure a level-placed enemy shows the right art for its starting HP.
        if (applyVisualOnStart && enemy != null)
            ApplyHPVisual(enemy.health);
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

                    // Swap the spawned enemy's visual to match its starting HP.
                    splitScript.ApplyHPVisual(currentHP);

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

    // ------------------------------------------------------------------
    // HP VISUAL
    // ------------------------------------------------------------------

    /// <summary>
    /// Swaps the visual tier based on HP. Picks exactly one of the three:
    /// critical (lowest), low (middle), or normal. Any slot left empty is
    /// simply skipped.
    /// </summary>
    public void ApplyHPVisual(int hp)
    {
        bool isCritical = hp < criticalHPThreshold;
        bool isLow = !isCritical && hp < lowHPThreshold;

        if (normalVisual != null)
            normalVisual.SetActive(!isCritical && !isLow);

        if (lowHPVisual != null)
            lowHPVisual.SetActive(isLow);

        if (criticalVisual != null)
            criticalVisual.SetActive(isCritical);
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