using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float specialSpeed = 20f;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private GameObject splashPrefab;
    [SerializeField] private AudioClip destroySound;
    [SerializeField] private AudioClip UnderwaterSound;
    [SerializeField] private AudioClip protectedHitSound;
    [SerializeField] private AudioSource audioSourcePrefab;
    [SerializeField] private int damage = 1;
    [SerializeField] private int maxBounces = 3;

    private int bounceCount = 0;

    private void Start()
    {
        PlayerRotation player = FindObjectOfType<PlayerRotation>();
        if (player != null)
            rb.velocity = transform.up * (player.isSpecial ? specialSpeed : speed);
        else
            Debug.Log("PlayerRotation script not found in the scene.");
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Dampen velocity on bouncy collision instead of destroying
        if (collision.gameObject.CompareTag("Bounce"))
        {
            bounceCount++;
            rb.velocity *= 0.25f;
            if (bounceCount < maxBounces) return;
            // Fallen through — treat as a normal hit after max bounces
        }

        Enemy enemy = collision.gameObject.GetComponent<Enemy>();
        if (enemy != null)
        {
            if (!enemy.isProtected)
            {
                if (enemy.audioSource != null && enemy.waterImpactSound != null)
                    enemy.audioSource.PlayOneShot(enemy.waterImpactSound);
                enemy.StartCoroutine(enemy.FlickerWhite());
                enemy.TakeDamage(damage);
                if (enemy.behavior is ProjectileBehavior projectileBehavior)
                    projectileBehavior.OnProjectileHit(enemy);
            }
            else
            {
                if (protectedHitSound != null && audioSourcePrefab != null)
                {
                    AudioSource tempAudioSource = Instantiate(audioSourcePrefab, transform.position, Quaternion.identity);
                    tempAudioSource.clip = protectedHitSound;
                    tempAudioSource.Play();
                    Destroy(tempAudioSource.gameObject, protectedHitSound.length);
                }
            }
        }

        if (splashPrefab != null)
            Instantiate(splashPrefab, transform.position, Quaternion.identity);

        PlayDestroySound();
        Destroy(gameObject);
    }

    public void Death()
    {
        if (splashPrefab != null)
            Instantiate(splashPrefab, transform.position, Quaternion.identity);

        PlayUWDestroySound();
        Destroy(gameObject);
    }

    private void PlayDestroySound()
    {
        if (destroySound != null && audioSourcePrefab != null)
        {
            AudioSource tempAudioSource = Instantiate(audioSourcePrefab, transform.position, Quaternion.identity);
            tempAudioSource.clip = destroySound;
            tempAudioSource.Play();
            Destroy(tempAudioSource.gameObject, destroySound.length);
        }
    }

    private void PlayUWDestroySound()
    {
        if (destroySound != null && audioSourcePrefab != null)
        {
            AudioSource tempAudioSource = Instantiate(audioSourcePrefab, transform.position, Quaternion.identity);
            tempAudioSource.clip = UnderwaterSound;
            tempAudioSource.Play();
            Destroy(tempAudioSource.gameObject, destroySound.length);
        }
    }
}