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
    [SerializeField] private AudioClip protectedHitSound; // NEW: sound for protected enemies
    [SerializeField] private AudioSource audioSourcePrefab;
    [SerializeField] private int damage = 1;

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
        Enemy enemy = collision.gameObject.GetComponent<Enemy>();
        if (enemy != null)
        {
            if (!enemy.isProtected)
            {
                // Enemy is not protected, perform normal hit actions
                if (enemy.audioSource != null && enemy.waterImpactSound != null)
                    enemy.audioSource.PlayOneShot(enemy.waterImpactSound);

                enemy.StartCoroutine(enemy.FlickerWhite());
                enemy.TakeDamage(damage);

                // Trigger shrink behavior if this enemy has a ProjectileBehavior
                if (enemy.behavior is ProjectileBehavior projectileBehavior)
                    projectileBehavior.OnProjectileHit(enemy);
            }
            else
            {
                // Enemy is protected, play special protected sound
                if (protectedHitSound != null && audioSourcePrefab != null)
                {
                    AudioSource tempAudioSource = Instantiate(audioSourcePrefab, transform.position, Quaternion.identity);
                    tempAudioSource.clip = protectedHitSound;
                    tempAudioSource.Play();
                    Destroy(tempAudioSource.gameObject, protectedHitSound.length);
                }
            }
        }

        // Instantiate splash effect
        if (splashPrefab != null)
            Instantiate(splashPrefab, transform.position, Quaternion.identity);

        // Play destroy sound
        PlayDestroySound();

        // Destroy projectile
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
}
