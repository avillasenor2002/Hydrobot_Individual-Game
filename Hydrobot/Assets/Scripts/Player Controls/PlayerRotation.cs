using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerRotation : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb2D;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float forceMagnitude = 10f;
    [SerializeField] public float maxSpeed = 5f;
    [SerializeField] private float gravityReturnSpeed = 0.5f;
    [SerializeField] private float bobbingFrequency = 2f;    // How fast the bobbing happens
    [SerializeField] private float bobbingAmplitude = 0.1f;  // How high/low the bobbing is
    private Vector2 moveInputValue;
    public bool isFreeze = false;
    public bool isSpecial = false;
    private float originalGravityScale;
    private float bobbingTime;

    public ProjectileShoot shoot;
    public WaterTank waterTotal;

    public float shootCooldown = 1f;
    private float lastShootTime = 0f;

    [SerializeField] private float dashForceMagnitude = 30f;
    [SerializeField] public float maxDashSpeed = 5f;
    [SerializeField] private float freezeWaterLoss = 0.1f;
    [SerializeField] public float dashShootCooldown = 0.5f;
    [SerializeField] public ParticleSystem freezePart;

    [SerializeField] private GameObject playerVisual; // The separate visual object to destroy
    [SerializeField] private ParticleSystem deathEffect; // The death particle effect
    [SerializeField] private LevelEndManager levelEndManager; // Reference to LevelEndManager
    [SerializeField] private string groundTag = "Ground"; // Tag for the ground
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioSource sfxSource; // Typically the SFX AudioSource in scene


    private void Start()
    {
        originalGravityScale = rb2D.gravityScale;
    }

    private void OnMovementFiring(InputValue value)
    {
        moveInputValue = value.Get<Vector2>();

        // Rumble ONLY when moving AND special mode is active
        if (Gamepad.current != null)
        {
            if (moveInputValue.sqrMagnitude > 0.01f && isSpecial)
            {
                Gamepad.current.SetMotorSpeeds(0.25f, 0.75f);
                Invoke("StopVibration", 0.01f);
            }
            else
            {
                // Ensure vibration turns off when not moving or not special
                Gamepad.current.SetMotorSpeeds(0f, 0f);
            }
        }
    }

    private void StopVibration()
    {
        if (Gamepad.current != null)
        {
            Gamepad.current.SetMotorSpeeds(0f, 0f);  // Stop vibration
        }
    }

    private void OnFreeze()
    {
        isFreeze = !isFreeze;

        if (isFreeze)
        {
            rb2D.velocity = Vector2.zero;
            rb2D.angularVelocity = 0f;
            rb2D.gravityScale = 0f;
            bobbingTime = 0f;  // Reset the bobbing time when freezing starts
        }
        else
        {
            StartCoroutine(ReturnGravity());
        }
    }

    public void OnSpecialAction() => isSpecial = !isSpecial;

    private void Update()
    {
        RotateLogicMethod();

        if (isFreeze)
        {
            // Bobbing up and down effect when frozen
            BobbingEffect();
            freezePart.Play();
        }
        else
        {
            freezePart.Stop();
        }
    }

    private void FixedUpdate()
    {
        if (waterTotal.currentWater <= 0)
        {
            isFreeze = false;
            StartCoroutine(ReturnGravity());
        }

        ApplyForce();

        if (!isFreeze)
        {
            LimitSpeed();
        }
        else
        {
            waterTotal.currentWater = waterTotal.currentWater - freezeWaterLoss;
        }
    }

    private void RotateLogicMethod()
    {
        if (moveInputValue.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(-moveInputValue.x, moveInputValue.y) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        else
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
    }

    private void ApplyForce()
    {
        if (moveInputValue.sqrMagnitude > 0.01f)
        {
            if (waterTotal.currentWater > 0)
            {
                if (!isFreeze)
                {
                    if (isSpecial == false)
                    {
                        rb2D.AddForce(-transform.up * forceMagnitude, ForceMode2D.Force);
                    }
                    else if (isSpecial == true)
                    {
                        rb2D.AddForce(-transform.up * dashForceMagnitude, ForceMode2D.Force);
                    }

                    if (isSpecial == false && Time.time >= lastShootTime + shootCooldown)
                    {
                        shoot.Shoot();
                        lastShootTime = Time.time;
                    }
                    else if (isSpecial == true && Time.time >= lastShootTime + dashShootCooldown)
                    {
                        shoot.Shoot();
                        lastShootTime = Time.time;
                    }
                }
            }
        }
    }

    private void LimitSpeed()
    {
        if (isSpecial == false && rb2D.velocity.magnitude > maxSpeed)
        {
            rb2D.velocity = rb2D.velocity.normalized * maxSpeed;
        }
        else if (isSpecial == true && rb2D.velocity.magnitude > maxDashSpeed)
        {
            rb2D.velocity = rb2D.velocity.normalized * maxDashSpeed;
        }
    }

    private IEnumerator ReturnGravity()
    {
        while (rb2D.gravityScale < originalGravityScale)
        {
            rb2D.gravityScale += gravityReturnSpeed * Time.deltaTime;
            yield return null;
        }

        rb2D.gravityScale = originalGravityScale;
    }

    private void BobbingEffect()
    {
        // Increment the bobbing time
        bobbingTime += Time.deltaTime * bobbingFrequency;

        // Apply bobbing effect to the position using a sine wave
        float newY = Mathf.Sin(bobbingTime) * bobbingAmplitude;
        transform.position = new Vector3(transform.position.x, transform.position.y + newY, transform.position.z);

        if (moveInputValue.sqrMagnitude > 0.01f)
        {
            if (waterTotal.currentWater > 0)
            {
                if (isFreeze)
                {
                    if (isSpecial == false && Time.time >= lastShootTime + shootCooldown)
                    {
                        shoot.Shoot();
                        lastShootTime = Time.time;
                    }
                    else if (isSpecial == true && Time.time >= lastShootTime + dashShootCooldown)
                    {
                        shoot.Shoot();
                        lastShootTime = Time.time;
                    }
                }
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        bool hitGround = collision.gameObject.CompareTag(groundTag);
        bool hitEnemy = collision.gameObject.GetComponent<Enemy>() != null;

        if ((hitGround || hitEnemy) && waterTotal.currentWater <= 0)
        {
            HandlePlayerDeath();
        }
    }

    private void HandlePlayerDeath()
    {
        // Destroy visual sprite
        if (playerVisual != null)
        {
            Destroy(playerVisual);
        }

        // Play death sound
        if (sfxSource != null && deathSound != null)
        {
            sfxSource.PlayOneShot(deathSound);
        }

        // Play death particle effect
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        // Notify the LevelEndManager
        if (levelEndManager != null)
        {
            levelEndManager.TriggerGameOver();
        }

        Destroy(gameObject);
    }

}
