using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 10f;  // Speed of the projectile
    [SerializeField] private float specialSpeed = 20f;  // Speed of the projectile
    [SerializeField] private Rigidbody2D rb;     // Reference to the Rigidbody2D component
    [SerializeField] private GameObject splashPrefab; // Prefab to instantiate on collision
    [SerializeField] private AudioClip destroySound; // Assign sound effect in Inspector
    [SerializeField] private AudioSource audioSourcePrefab; // Prefab with AudioSource to play the sound

    private void Start()
    {
        // Set the projectile's velocity in the direction it is facing
        PlayerRotation player = FindObjectOfType<PlayerRotation>();

        if (player != null)
        {
            if (player.isSpecial == false)
            {
                rb.velocity = transform.up * speed;  // Move in the direction the projectile is facing
            }
            else
            {
                rb.velocity = transform.up * specialSpeed;
            }
        }
        else
        {
            Debug.Log("PlayerRotation script not found in the scene.");
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Instantiate the splash prefab at the collision point
        Instantiate(splashPrefab, transform.position, Quaternion.identity);

        // Play sound before destroying
        PlayDestroySound();

        // Destroy the projectile when it collides with any object
        Kill();
    }

    private void PlayDestroySound()
    {
        if (destroySound != null && audioSourcePrefab != null)
        {
            AudioSource tempAudioSource = Instantiate(audioSourcePrefab, transform.position, Quaternion.identity);
            tempAudioSource.clip = destroySound;
            tempAudioSource.Play();
            Destroy(tempAudioSource.gameObject, destroySound.length); // Destroy the AudioSource object after the sound plays
        }
    }

    public void Kill()
    {
        Destroy(gameObject);
    }
}
