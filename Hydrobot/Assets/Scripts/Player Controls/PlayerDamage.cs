using System.Collections;
using UnityEngine;

public class PlayerDamage : MonoBehaviour
{
    [SerializeField] private SpriteRenderer playerSpriteRenderer;
    [SerializeField] private AudioSource damageSound;
    [SerializeField] private float flickerDuration = 1f;
    [SerializeField] private float invincibilityDuration = 2f;
    [SerializeField] private float bounceBackForce = 10f;
    [SerializeField] private WaterTank waterTank;
    [SerializeField] private ScreenShake screenShake; // Reference to ScreenShake script
    [SerializeField] private AudioSource sfxPlayer; // Reference to SFX player
    [SerializeField] private AudioClip specificDamageSFX; // The specific sound effect for damage

    private bool isInvincible = false;
    private bool isFlickering = false;
    private SpriteRenderer flickerSpriteRenderer;
    private Rigidbody2D rb2D;

    private void Start()
    {
        flickerSpriteRenderer = GetComponent<SpriteRenderer>();
        rb2D = GetComponent<Rigidbody2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Enemy enemy = collision.gameObject.GetComponent<Enemy>();

        // Trigger damage only if colliding with an enemy where isObject == false
        if (!isInvincible && enemy != null && !enemy.isObject)
        {
            StartCoroutine(HandleDamage(collision));
        }
    }

    private IEnumerator HandleDamage(Collision2D collision)
    {
        isInvincible = true;

        // Play default damage sound
        if (damageSound != null)
        {
            damageSound.Play();
        }

        // Play specific SFX using SFX player
        if (sfxPlayer != null && specificDamageSFX != null)
        {
            sfxPlayer.PlayOneShot(specificDamageSFX);
        }

        // Trigger screen shake effect
        if (screenShake != null)
        {
            screenShake.ShakeCamera();
        }

        // Start flickering effect
        StartCoroutine(FlickerEffect());

        // Apply bounce back effect
        ApplyBounceBack(collision);

        // Reduce player's water
        if (waterTank != null)
        {
            waterTank.currentWater -= 10f;
        }

        yield return new WaitForSeconds(invincibilityDuration);

        isInvincible = false;
    }

    private IEnumerator FlickerEffect()
    {
        isFlickering = true;

        float timePassed = 0f;
        while (timePassed < flickerDuration)
        {
            playerSpriteRenderer.enabled = !playerSpriteRenderer.enabled;
            yield return new WaitForSeconds(0.1f);
            timePassed += 0.1f;
        }

        playerSpriteRenderer.enabled = true;
        isFlickering = false;
    }

    private void ApplyBounceBack(Collision2D collision)
    {
        Vector2 bounceDirection = ((Vector2)transform.position - collision.contacts[0].point).normalized;
        rb2D.AddForce(bounceDirection * bounceBackForce, ForceMode2D.Impulse);
    }
}
