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
        // Try to damage the enemy first
        Enemy enemy = collision.gameObject.GetComponent<Enemy>();
        if (enemy != null)
        {
            // Apply damage
            enemy.TakeDamage(damage);

            // Trigger shrink behavior if this enemy has a ProjectileBehavior
            if (enemy.behavior is ProjectileBehavior projectileBehavior)
            {
                projectileBehavior.OnProjectileHit(enemy);
            }
        }

        // Instantiate splash if assigned
        if (splashPrefab != null)
            Instantiate(splashPrefab, transform.position, Quaternion.identity);

        // Play sound
        PlayDestroySound();

        // Destroy the projectile after dealing damage
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
