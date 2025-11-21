#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteAlways]
[RequireComponent(typeof(Collider2D))]
public class Enemy : MonoBehaviour
{
    public static event System.Action OnEnemyDestroyed;

    [Header("Enemy Stats")]
    public int health = 3;
    public int waterLoss = 10;
    [SerializeField] public AudioClip waterImpactSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private ParticleSystem deathEffect;
    [SerializeField] public AudioSource audioSource;
    [SerializeField] private float flickerDuration = 0.1f;

    [Header("Tilemap Settings")]
    public Tilemap targetTilemap;
    public bool snapToTilemap = true;
    public bool isObject = false;

    [Header("Behavior")]
    public EnemyBehavior behavior;

    [HideInInspector] public bool isBeingInhaled = false;
    public bool isDead = false;

    public Collider2D col;
    public bool isInvincible = false;
    public float invincibilityTimer = 0f;

    //Used for TrackingBehavior and other behaviors
    [HideInInspector] public bool IsFiring = false;
    [HideInInspector] public bool HasFiredThisFrame = false;
    [HideInInspector] public bool HasNoticedPlayer = false;
    [HideInInspector] public float initialZRotation;
    public float noticeCooldown = 0f;
    public bool isProtected = false; // set by protection enemy


    private void Awake()
    {
        col = GetComponent<Collider2D>();


        // Store initial Z rotation
        initialZRotation = transform.eulerAngles.z;

        // Auto-assign tilemap if missing
        if (targetTilemap == null)
            FindMainTilemap();

        // Auto-assign audioSource if missing
        if (audioSource == null)
        {
            GameObject sfxObject = GameObject.Find("SFX");
            if (sfxObject != null)
                audioSource = sfxObject.GetComponent<AudioSource>();
        }
    }

    public void Start()
    {
        SnapToTilemapIfNeeded();
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (!Application.isPlaying)
            SnapAllEnemiesToTilemap();
    }
#endif

    private void FixedUpdate()
    {
        if (Application.isPlaying && behavior != null && !isDead && !isBeingInhaled)
        {
            behavior.Execute(this);
        }

        if (isInvincible)
        {
            invincibilityTimer -= Time.fixedDeltaTime;
            if (invincibilityTimer <= 0f)
                isInvincible = false;
        }
    }

    private void SnapToTilemapIfNeeded()
    {
        if (snapToTilemap)
        {
            if (targetTilemap == null)
                FindMainTilemap();

            if (targetTilemap != null)
                SnapToTilemap();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead || isInvincible) return;

        bool hitProjectile = false;

        // Check for WaterProjectile component
        if (other.GetComponent<WaterProjectile>() != null)
            hitProjectile = true;

        // Check if object is on Projectile layer AND has tag "Projectile"
        if (other.gameObject.CompareTag("Projectile"))
            hitProjectile = true;

        if (hitProjectile)
        {
            if (audioSource != null && waterImpactSound != null)
                audioSource.PlayOneShot(waterImpactSound);

            StartCoroutine(FlickerWhite());
            TakeDamage(1);

            ///*// NEW: If this is a projectile enemy, shrink it
            //if (behavior is ProjectileBehavior proj)
            //    proj.OnProjectileHit(this*/);
        }


        // Player collision (optional)
        if (other.CompareTag("Player"))
        {
            PlayerDamage player = other.GetComponent<PlayerDamage>();
            if (player != null)
            {
                // player.LoseWater(waterLoss); // optional
            }
        }

        // If this Enemy uses ProjectileBehavior, shrink or destroy
        if (behavior is ProjectileBehavior proj)
        {
            proj.OnProjectileHit(this);

            // Always destroy the projectile GameObject
            Destroy(other.gameObject);
        }
    }




    public void TakeDamage(int amount)
    {
        if (isDead) return;

        health -= amount;
        MakeInvincible(0.2f);

        if (health <= 0)
            Die();
    }
    
    public event System.Action<Enemy> OnEnemyDeath;
    public void Die()
    {
        if (isDead) return;
        isDead = true;

        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        if (audioSource != null && deathSound != null)
            audioSource.PlayOneShot(deathSound);

        OnEnemyDeath?.Invoke(this); // notify manager

        Destroy(gameObject);
    }


    public IEnumerator FlickerWhite()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var r in renderers) r.enabled = false;
        yield return new WaitForSeconds(flickerDuration);
        foreach (var r in renderers) r.enabled = true;
    }

    public void MakeInvincible(float duration)
    {
        isInvincible = true;
        invincibilityTimer = duration;
    }

    // ---------------- Tilemap Helpers ----------------
    private void SnapToTilemap()
    {
        if (targetTilemap == null) return;
        Vector3Int cell = targetTilemap.WorldToCell(transform.position);
        transform.position = targetTilemap.GetCellCenterWorld(cell);
    }

    private void FindMainTilemap()
    {
        Tilemap[] maps = FindObjectsOfType<Tilemap>();
        foreach (Tilemap map in maps)
        {
            if (map.gameObject.name == "Main Tilemap")
            {
                targetTilemap = map;
                return;
            }
        }
    }

    public Transform GetPlayerTransform()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        return playerObj != null ? playerObj.transform : null;
    }


#if UNITY_EDITOR
    private void SnapAllEnemiesToTilemap()
    {
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        foreach (var enemy in enemies)
        {
            if (enemy.snapToTilemap)
            {
                if (enemy.targetTilemap == null)
                    enemy.FindMainTilemap();

                if (enemy.targetTilemap != null)
                    enemy.SnapToTilemap();
            }
        }

         if (behavior is TrackingBehavior tracking)
    {
       /* Gizmos.color = new Color(1f, 0f, 0f, 0.25f);

        // Draw a semi-transparent cone
        Vector3 forward = transform.up;
        Vector3 position = transform.position;

        Handles.color = new Color(1f, 0f, 0f, 0.2f);

        float angle = tracking.detectionAngle;
        float range = tracking.detectionRange;

        Vector3 rightDir = Quaternion.Euler(0, 0, angle) * forward;
        Vector3 leftDir = Quaternion.Euler(0, 0, -angle) * forward;

        // Draw lines
        Gizmos.DrawLine(position, position + rightDir * range);
        Gizmos.DrawLine(position, position + leftDir * range);

        // Draw arc for the cone
        Handles.DrawSolidArc(position, Vector3.forward, leftDir, angle * 2, range);*/
    }
    }
#endif


}