using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Collider2D))]
public class TrackingEnemy : MonoBehaviour
{
    public static event System.Action OnTrackingEnemyDestroyed;

    [Header("Stats")]
    public int health = 3;
    public AudioClip waterImpactSound;
    public AudioClip deathSound;
    public ParticleSystem deathEffect;
    public AudioSource audioSource;
    public float flickerDuration = 0.1f;

    [Header("Tilemap")]
    public Tilemap targetTilemap;
    public bool snapToTilemap = true;

    [Header("Tracking")]
    public float detectionRange = 6f;
    public float detectionAngle = 45f;
    public float rotationSpeed = 10f;
    public float returnSpeed = 3f;
    public LayerMask obstacleMask;

    [Header("Notice")]
    public AudioClip noticeSound;
    public float scalePulseAmount = 0.2f;
    public float scalePulseDuration = 0.2f;

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public ProjectileBehavior projectileBehavior;
    public float chargeDuration = 1.5f;
    public float chargeScaleAmount = 0.3f;
    public float fireRate = 2f;

    // runtime state
    [SerializeField] private Transform playerTransform;
    private bool isDead = false;
    private bool hasNoticedPlayer = false;
    private float fireCooldown = 0f;
    private float initialZRotation;
    private bool isInvincible = false;
    private float invincibilityTimer = 0f;

    private int initialHealth;

    private void Awake()
    {
        initialZRotation = transform.eulerAngles.z;
        initialHealth = health;

        if (targetTilemap == null) FindMainTilemap();
        if (audioSource == null)
        {
            GameObject sfxObj = GameObject.Find("SFX");
            if (sfxObj != null) audioSource = sfxObj.GetComponent<AudioSource>();
        }

        ResetForScene();
    }

    private void OnEnable()
    {
        // Ensure proper initialization whenever the enemy becomes active
        StartCoroutine(InitializeEnemy());
    }

    private IEnumerator InitializeEnemy()
    {
        while (playerTransform == null)
        {
            // Step 1: Try to find by tag
            GameObject taggedPlayer = GameObject.FindWithTag("Player");
            if (taggedPlayer != null)
            {
                playerTransform = taggedPlayer.transform;
                break;
            }

            // Step 2: Try to find by name
            GameObject namedPlayer = GameObject.Find("Player");
            if (namedPlayer != null)
            {
                if (namedPlayer.name == "Hydrobot")
                {
                    // Step 3: Look for a child with tag "Player"
                    Transform childPlayer = null;
                    foreach (Transform child in namedPlayer.transform)
                    {
                        if (child.CompareTag("Player"))
                        {
                            childPlayer = child;
                            break;
                        }
                    }

                    if (childPlayer != null)
                    {
                        playerTransform = childPlayer;
                        break;
                    }
                }
                else
                {
                    playerTransform = namedPlayer.transform;
                    break;
                }
            }

            yield return null;
        }

        ResetForScene();
    }


    private void FixedUpdate()
    {
        if (isDead) return;
        if (playerTransform == null) return;

        bool detected = CanSeePlayer(out Vector2 toPlayer);

        HandleNotice(detected);
        HandleRotation(detected, toPlayer);
        HandleFiring(detected);

        if (isInvincible)
        {
            invincibilityTimer -= Time.fixedDeltaTime;
            if (invincibilityTimer <= 0) isInvincible = false;
        }
    }

    private bool CanSeePlayer(out Vector2 toPlayer)
    {
        toPlayer = playerTransform.position - transform.position;
        float distance = toPlayer.magnitude;

        if (distance > detectionRange) return false;

        Vector2 forward = transform.up;
        float angleToPlayer = Vector2.Angle(forward, toPlayer);
        if (angleToPlayer > detectionAngle) return false;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, toPlayer.normalized, distance, obstacleMask);
        if (hit.collider != null) return false;

        return true;
    }

    private void HandleNotice(bool detected)
    {
        if (detected && !hasNoticedPlayer)
        {
            hasNoticedPlayer = true;
            if (noticeSound != null && audioSource != null) audioSource.PlayOneShot(noticeSound);
            StartCoroutine(PulseScale(transform));
        }
        else if (!detected)
        {
            hasNoticedPlayer = false;
        }
    }

    private void HandleRotation(bool detected, Vector2 toPlayer)
    {
        float targetAngle = detected ? Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg - 90f : initialZRotation;
        float speed = detected ? rotationSpeed : returnSpeed;
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, targetAngle), speed * Time.fixedDeltaTime);
    }

    private void HandleFiring(bool detected)
    {
        fireCooldown -= Time.fixedDeltaTime;
        if (detected && fireCooldown <= 0f)
        {
            fireCooldown = fireRate;
            StartCoroutine(FireProjectile());
        }
    }

    private IEnumerator FireProjectile()
    {
        Transform chargeParticles = transform.Find("Charge Particles");
        Transform fireParticles = transform.Find("Dirt Fired");

        if (chargeParticles != null) chargeParticles.gameObject.SetActive(true);

        Vector3 origScale = transform.localScale;
        Vector3 bigScale = origScale * (1f + chargeScaleAmount);

        float t = 0f;
        while (t < chargeDuration)
        {
            transform.localScale = Vector3.Lerp(origScale, bigScale, t / chargeDuration);
            t += Time.deltaTime;
            yield return null;
        }

        transform.localScale = origScale;
        if (chargeParticles != null) chargeParticles.gameObject.SetActive(false);

        if (projectilePrefab != null)
        {
            GameObject projGO = Instantiate(projectilePrefab, transform.position, transform.rotation);
            ProjectileMono proj = projGO.GetComponent<ProjectileMono>();
            if (proj != null)
                proj.Initialize(transform.up, projectileBehavior, null);
        }

        if (fireParticles != null)
        {
            fireParticles.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.5f);
            fireParticles.gameObject.SetActive(false);
        }
    }

    private IEnumerator PulseScale(Transform t)
    {
        Vector3 orig = t.localScale;
        Vector3 big = orig * (1f + scalePulseAmount);
        float half = scalePulseDuration / 2f;
        float timer = 0f;

        while (timer < half)
        {
            t.localScale = Vector3.Lerp(orig, big, timer / half);
            timer += Time.deltaTime;
            yield return null;
        }

        timer = 0f;
        while (timer < half)
        {
            t.localScale = Vector3.Lerp(big, orig, timer / half);
            timer += Time.deltaTime;
            yield return null;
        }

        t.localScale = orig;
    }

    private void SnapToTilemap()
    {
        if (!snapToTilemap) return;
        if (targetTilemap == null) FindMainTilemap();
        if (targetTilemap != null)
        {
            Vector3Int cell = targetTilemap.WorldToCell(transform.position);
            transform.position = targetTilemap.GetCellCenterWorld(cell);
        }
    }

    private void FindMainTilemap()
    {
        Tilemap[] maps = FindObjectsOfType<Tilemap>();
        foreach (var map in maps)
        {
            if (map.gameObject.name == "Main Tilemap")
            {
                targetTilemap = map;
                return;
            }
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;
        health -= amount;
        MakeInvincible(0.2f);
        if (health <= 0) Die();
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        if (deathEffect != null) Instantiate(deathEffect, transform.position, Quaternion.identity);
        if (audioSource != null && deathSound != null) audioSource.PlayOneShot(deathSound);

        OnTrackingEnemyDestroyed?.Invoke();
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead || isInvincible) return;

        bool hitProjectile = other.GetComponent<WaterProjectile>() != null || other.CompareTag("Projectile");
        if (hitProjectile)
        {
            if (audioSource != null && waterImpactSound != null) audioSource.PlayOneShot(waterImpactSound);
            StartCoroutine(FlickerWhite());
            TakeDamage(1);
        }
    }

    private IEnumerator FlickerWhite()
    {
        SpriteRenderer[] rends = GetComponentsInChildren<SpriteRenderer>();
        foreach (var r in rends) r.enabled = false;

        yield return new WaitForSeconds(flickerDuration);

        foreach (var r in rends) r.enabled = true;
    }

    public void MakeInvincible(float duration)
    {
        isInvincible = true;
        invincibilityTimer = duration;
    }

    /// <summary>
    /// Reset enemy state as if freshly spawned
    /// </summary>
    public void ResetForScene()
    {
        StopAllCoroutines();
        gameObject.SetActive(true);

        isDead = false;
        hasNoticedPlayer = false;
        isInvincible = false;
        invincibilityTimer = 0f;
        fireCooldown = 0f;

        health = initialHealth;

        transform.rotation = Quaternion.Euler(0f, 0f, initialZRotation);
        SnapToTilemap();

        playerTransform = GameObject.FindWithTag("Player")?.transform;
    }

    public static void ClearStaticEvents()
    {
        OnTrackingEnemyDestroyed = null;
    }
}
