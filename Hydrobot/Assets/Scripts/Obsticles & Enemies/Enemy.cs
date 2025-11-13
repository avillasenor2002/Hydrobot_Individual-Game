using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Enemy : MonoBehaviour
{
    public static event System.Action OnEnemyDestroyed;

    [SerializeField] private AudioClip waterImpactSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private int hp = 3;
    [SerializeField] private float flickerDuration = 0.1f;
    [SerializeField] private ParticleSystem deathEffect;
    public bool isObject = false;  // Set this flag in the Inspector for "object" enemies.
    private PlayerDamage player;
    public float waterLoss = 10f;  // The amount of water to lose when colliding with the player

    private bool isInvincible = false;
    private float invincibilityTimer = 0f;


    private void Start()
    {
        player = FindObjectOfType<PlayerDamage>();

        // Auto-assign audioSource to an object named "SFX" if not manually assigned
        if (audioSource == null)
        {
            GameObject sfxObject = GameObject.Find("SFX");
            if (sfxObject != null)
            {
                audioSource = sfxObject.GetComponent<AudioSource>();
            }
        }

        // Lock object to tile grid if marked as an object
        if (isObject)
        {
            GameObject tilemapObject = GameObject.Find("Main Tilemap");
            if (tilemapObject != null)
            {
                Tilemap tilemap = tilemapObject.GetComponent<Tilemap>();
                if (tilemap != null)
                {
                    Vector3 worldPos = transform.position;
                    Vector3Int cellPos = tilemap.WorldToCell(worldPos);
                    Vector3 snappedPos = tilemap.GetCellCenterWorld(cellPos);
                    transform.position = snappedPos;
                }
            }
        }
    }

    private void Update()
    {
        if (isInvincible)
        {
            invincibilityTimer -= Time.deltaTime;
            if (invincibilityTimer <= 0f)
            {
                isInvincible = false;
            }
        }
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isInvincible) return;  // Ignore damage if invincible

        if (collision.gameObject.GetComponent<WaterProjectile>())
        {
            {
                if (audioSource != null && waterImpactSound != null)
                {
                    audioSource.PlayOneShot(waterImpactSound);
                }

                StartCoroutine(FlickerWhite());
                hp--;

                if (hp <= 0)
                {
                    if (audioSource != null && deathSound != null)
                    {
                        audioSource.PlayOneShot(deathSound);
                    }

                    if (deathEffect != null)
                    {
                        Instantiate(deathEffect, transform.position, Quaternion.identity);
                    }

                    if (!isObject)
                    {
                        OnEnemyDestroyed?.Invoke();
                    }

                    Destroy(gameObject);
                }
            }
        }
    }
        private IEnumerator FlickerWhite()
        {
            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();

            // Disable renderers
            foreach (var renderer in renderers)
            {
                renderer.enabled = false;
            }

            yield return new WaitForSeconds(flickerDuration);

            // Re-enable renderers
            foreach (var renderer in renderers)
            {
                renderer.enabled = true;
            }
        }

        public void MakeInvincible(float duration)
        {
            isInvincible = true;
            invincibilityTimer = duration;
        }
}
