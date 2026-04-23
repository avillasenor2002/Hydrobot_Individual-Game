using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyLookEffect : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The player transform. If left empty, will attempt to find by 'Player' tag.")]
    public Transform player;

    [Header("Rotation Settings")]
    [Tooltip("Speed of rotation smoothing (degrees per second).")]
    public float rotationSpeed = 180f;
    [Tooltip("Minimum angle change before rotating (prevents jitter).")]
    public float angleDeadZone = 0.5f;
    [Tooltip("Should rotation be smoothed over time? If false, instant snap.")]
    public bool smoothRotation = true;

    [Header("Floating Effect (Optional)")]
    [Tooltip("Amplitude of vertical hover motion.")]
    public float floatAmplitude = 0.1f;
    [Tooltip("Frequency of hover motion.")]
    public float floatFrequency = 1f;

    [Header("Sprite Direction Mapping")]
    [Tooltip("Sprites for different facing directions. Index must match angleRanges order.")]
    public Sprite[] directionSprites;
    [Tooltip("Angle thresholds (in degrees) for each sprite. Angle 0 = facing right.")]
    public float[] angleRanges = { 0f, 90f, 180f, 270f }; // Default: 4-way (Right, Up, Left, Down)

    // Internal
    private SpriteRenderer spriteRenderer;
    private Vector3 startPosition;
    private float currentAngle;
    private int lastSpriteIndex = -1;

    private void Start()
    {
        // Find player if not assigned
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
            else
                Debug.LogWarning($"{name}: No Player assigned and none found with tag 'Player'.");
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        startPosition = transform.position;
        currentAngle = transform.eulerAngles.z;

        // Ensure arrays match
        if (directionSprites.Length != angleRanges.Length)
            Debug.LogError($"{name}: directionSprites and angleRanges must have the same length!");

        // Set initial sprite
        UpdateSpriteBasedOnAngle();
    }

    private void Update()
    {
        if (player == null) return;

        // Optional hover effect
        ApplyHoverEffect();

        // Calculate direction to player
        Vector2 direction = (player.position - transform.position).normalized;
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Apply rotation
        RotateTowards(targetAngle);

        // Update sprite based on current rotation
        UpdateSpriteBasedOnAngle();
    }

    /// <summary>
    /// Smoothly rotates the enemy towards the target angle.
    /// </summary>
    private void RotateTowards(float targetAngle)
    {
        // Normalize angles for comparison
        float currentZ = transform.eulerAngles.z;
        float delta = Mathf.DeltaAngle(currentZ, targetAngle);

        // Skip if change is tiny
        if (Mathf.Abs(delta) <= angleDeadZone)
            return;

        if (smoothRotation)
        {
            float step = rotationSpeed * Time.deltaTime;
            float newAngle = Mathf.MoveTowardsAngle(currentZ, targetAngle, step);
            transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
        }
        else
        {
            transform.rotation = Quaternion.Euler(0f, 0f, targetAngle);
        }

        currentAngle = transform.eulerAngles.z;
    }

    /// <summary>
    /// Applies a gentle floating motion using a sine wave.
    /// </summary>
    private void ApplyHoverEffect()
    {
        Vector3 pos = startPosition;
        pos.y += Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = pos;
    }

    /// <summary>
    /// Selects the correct sprite based on the current rotation angle.
    /// </summary>
    private void UpdateSpriteBasedOnAngle()
    {
        if (directionSprites == null || directionSprites.Length == 0)
            return;

        float angle = transform.eulerAngles.z;
        angle = (angle + 360f) % 360f; // Ensure 0-360

        // Find the closest angle range
        int bestIndex = 0;
        float minDifference = Mathf.Abs(Mathf.DeltaAngle(angle, angleRanges[0]));

        for (int i = 1; i < angleRanges.Length; i++)
        {
            float diff = Mathf.Abs(Mathf.DeltaAngle(angle, angleRanges[i]));
            if (diff < minDifference)
            {
                minDifference = diff;
                bestIndex = i;
            }
        }

        // Only change sprite if index changed to avoid flickering
        if (bestIndex != lastSpriteIndex && bestIndex < directionSprites.Length)
        {
            spriteRenderer.sprite = directionSprites[bestIndex];
            lastSpriteIndex = bestIndex;
        }
    }

    // Optional: Draw debug info in the Scene view
    private void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, player.position);
        }

        // Show facing direction
        Gizmos.color = Color.green;
        Vector3 dir = Quaternion.Euler(0, 0, transform.eulerAngles.z) * Vector3.right;
        Gizmos.DrawRay(transform.position, dir * 0.5f);
    }
}