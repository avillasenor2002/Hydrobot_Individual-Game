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
    [SerializeField] private float bobbingFrequency = 2f;
    [SerializeField] private float bobbingAmplitude = 0.1f;
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

    [SerializeField] private GameObject playerVisual;
    [SerializeField] private ParticleSystem deathEffect;
    [SerializeField] private LevelEndManager levelEndManager;
    [SerializeField] private string groundTag = "Ground";
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioSource sfxSource;

    // Track the active ReturnGravity coroutine so we can stop it before
    // starting a new one, preventing stacked coroutines from compounding gravityScale.
    private Coroutine returnGravityRoutine;

    private void Start()
    {
        originalGravityScale = rb2D.gravityScale;

        // Find every DirectionalSpriteSwapper in the scene and hand each one
        // a reference to this transform so they can track the player.
        RegisterWithSpriteSwappers();
    }

    // ??????????????????????????????????????????????????????????
    //  DirectionalSpriteSwapper registration
    // ??????????????????????????????????????????????????????????

    /// <summary>
    /// Finds all DirectionalSpriteSwapper components in the scene and sets their
    /// playerTransform to this object's transform, overwriting any stale reference
    /// (e.g. left over from a previous run or set to null by tag-find failure).
    /// </summary>
    private void RegisterWithSpriteSwappers()
    {
        DirectionalSpriteSwapper[] swappers =
            FindObjectsByType<DirectionalSpriteSwapper>(FindObjectsSortMode.None);

        if (swappers.Length == 0)
        {
            Debug.Log("[PlayerRotation] No DirectionalSpriteSwapper found in scene.", this);
            return;
        }

        foreach (DirectionalSpriteSwapper swapper in swappers)
            swapper.playerTransform = transform;

        Debug.Log($"[PlayerRotation] Registered with {swappers.Length} DirectionalSpriteSwapper(s).", this);
    }

    // ??????????????????????????????????????????????????????????
    //  Input
    // ??????????????????????????????????????????????????????????

    private void OnMovementFiring(InputValue value)
    {
        moveInputValue = value.Get<Vector2>();

        if (PlayerSettingsManager.Instance != null)
        {
            bool invert = PlayerSettingsManager.Instance.invertStick;
            if (!invert)
                moveInputValue = -moveInputValue;
        }

        if (Gamepad.current != null)
        {
            if (moveInputValue.sqrMagnitude > 0.01f && isSpecial)
            {
                Gamepad.current.SetMotorSpeeds(0.25f, 0.75f);
                Invoke("StopVibration", 0.01f);
            }
            else
            {
                Gamepad.current.SetMotorSpeeds(0f, 0f);
            }
        }
    }

    private void StopVibration()
    {
        if (Gamepad.current != null)
            Gamepad.current.SetMotorSpeeds(0f, 0f);
    }

    private void OnFreeze()
    {
        isFreeze = !isFreeze;

        if (isFreeze)
        {
            // Cancel any in-progress gravity return before zeroing velocity.
            // Without this the coroutine keeps running after freeze re-engages and
            // fights with gravityScale = 0, causing erratic launches on unfreeze.
            if (returnGravityRoutine != null)
            {
                StopCoroutine(returnGravityRoutine);
                returnGravityRoutine = null;
            }

            rb2D.velocity = Vector2.zero;
            rb2D.angularVelocity = 0f;
            rb2D.gravityScale = 0f;
            bobbingTime = 0f;
        }
        else
        {
            returnGravityRoutine = StartCoroutine(ReturnGravity());
        }
    }

    public void OnSpecialAction() => isSpecial = !isSpecial;
    public bool IsMoving => moveInputValue.sqrMagnitude > 0.01f;
    public Vector2 MoveInput => -moveInputValue;
    public Vector2 GetMoveInput() => moveInputValue;

    // ??????????????????????????????????????????????????????????
    //  Update / FixedUpdate
    // ??????????????????????????????????????????????????????????

    private void Update()
    {
        RotateLogicMethod();

        if (isFreeze)
        {
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
            // Same guard — if water drains to zero mid-freeze, stop any existing
            // coroutine before starting a fresh one.
            if (returnGravityRoutine != null)
            {
                StopCoroutine(returnGravityRoutine);
                returnGravityRoutine = null;
            }
            returnGravityRoutine = StartCoroutine(ReturnGravity());
        }

        ApplyForce();

        if (!isFreeze)
            LimitSpeed();
        else
            waterTotal.currentWater -= freezeWaterLoss;
    }

    // ??????????????????????????????????????????????????????????
    //  Movement helpers
    // ??????????????????????????????????????????????????????????

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
                        rb2D.AddForce(-transform.up * forceMagnitude, ForceMode2D.Force);
                    else if (isSpecial == true)
                        rb2D.AddForce(-transform.up * dashForceMagnitude, ForceMode2D.Force);

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
            rb2D.velocity = rb2D.velocity.normalized * maxSpeed;
        else if (isSpecial == true && rb2D.velocity.magnitude > maxDashSpeed)
            rb2D.velocity = rb2D.velocity.normalized * maxDashSpeed;
    }

    private IEnumerator ReturnGravity()
    {
        while (rb2D.gravityScale < originalGravityScale)
        {
            rb2D.gravityScale += gravityReturnSpeed * Time.deltaTime;
            yield return null;
        }

        rb2D.gravityScale = originalGravityScale;
        returnGravityRoutine = null;
    }

    private void BobbingEffect()
    {
        bobbingTime += Time.deltaTime * bobbingFrequency;

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

    // ??????????????????????????????????????????????????????????
    //  Collision
    // ??????????????????????????????????????????????????????????

    private void OnCollisionEnter2D(Collision2D collision)
    {
        bool hitGround = collision.gameObject.CompareTag(groundTag);
        bool hitEnemy = collision.gameObject.GetComponent<Enemy>() != null;

        // If frozen when an enemy makes contact, cancel the freeze so gravity
        // and velocity are restored before the impulse is applied — prevents the
        // player being launched by a collision against a stationary or moving enemy.
        if (hitEnemy && isFreeze)
        {
            isFreeze = false;
            rb2D.velocity = Vector2.zero;
            rb2D.angularVelocity = 0f;
            if (returnGravityRoutine != null)
            {
                StopCoroutine(returnGravityRoutine);
                returnGravityRoutine = null;
            }
            returnGravityRoutine = StartCoroutine(ReturnGravity());
        }

        if ((hitGround || hitEnemy) && waterTotal.currentWater <= 0)
        {
            rb2D.velocity = Vector2.zero;
            rb2D.angularVelocity = 0f;
            HandlePlayerDeath();
        }
    }

    private void HandlePlayerDeath()
    {
        if (playerVisual != null)
            Destroy(playerVisual);

        if (sfxSource != null && deathSound != null)
            sfxSource.PlayOneShot(deathSound);

        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        if (levelEndManager != null)
            levelEndManager.TriggerGameOver();

        Destroy(gameObject);
    }
}