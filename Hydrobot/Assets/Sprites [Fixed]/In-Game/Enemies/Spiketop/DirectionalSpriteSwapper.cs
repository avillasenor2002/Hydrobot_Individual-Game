using UnityEngine;

/// <summary>
/// DirectionalSpriteSwapper
///
/// • 4 sprites: idle (out of range), level, above, below
/// • Detection range: only tracks the player when close enough; shows idleSprite otherwise
/// • Smooth Z-axis tilt that never reads back from localEulerAngles (fixes the 0/360 wrap jump)
/// • Tilt sign is baked BEFORE flipping so the visual always tilts the right way
/// • flipX faces the sprite toward the player with a dead-zone to prevent jitter
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class DirectionalSpriteSwapper : MonoBehaviour
{
    // ──────────────────────────────────────────────────────────
    //  Inspector – Sprites
    // ──────────────────────────────────────────────────────────

    [Header("Sprites")]
    [Tooltip("Shown when the player is outside detectionRange. Object 'at rest'.")]
    public Sprite idleSprite;

    [Tooltip("Shown when the player is within ±angleThreshold degrees of this object's level.")]
    public Sprite levelSprite;

    [Tooltip("Shown when the player is MORE than angleThreshold degrees ABOVE this object.")]
    public Sprite aboveSprite;

    [Tooltip("Shown when the player is MORE than angleThreshold degrees BELOW this object.")]
    public Sprite belowSprite;

    // ──────────────────────────────────────────────────────────
    //  Inspector – Target & Range
    // ──────────────────────────────────────────────────────────

    [Header("Target & Detection Range")]
    [Tooltip("Transform to track. Leave blank to auto-find the GameObject tagged 'Player'.")]
    public Transform playerTransform;

    [Tooltip("World-unit radius within which the object tracks the player. " +
             "Outside this range the idleSprite is shown and tilt returns to 0.")]
    public float detectionRange = 8f;

    [Tooltip("If true, the detection range is visualised as a wire sphere/circle in the Scene view.")]
    public bool showRangeGizmo = true;

    // ──────────────────────────────────────────────────────────
    //  Inspector – Angle Settings
    // ──────────────────────────────────────────────────────────

    [Header("Angle Settings")]
    [Tooltip("Half-angle of the 'level' zone in degrees. 45° gives three equal 90° bands.")]
    [Range(1f, 89f)]
    public float angleThreshold = 45f;

    [Tooltip("Measure from the SpriteRenderer bounds centre instead of the object pivot.")]
    public bool useSpriteCentre = false;

    // ──────────────────────────────────────────────────────────
    //  Inspector – Tilt
    // ──────────────────────────────────────────────────────────

    [Header("Tilt / Rotation")]
    [Tooltip("Enable Z-axis tilt that reacts to the player's vertical position.")]
    public bool enableTilt = true;

    [Tooltip("Maximum tilt angle (degrees) when the player is at full overshoot past the threshold.")]
    [Range(0f, 45f)]
    public float maxTiltAngle = 15f;

    [Tooltip("Degrees of overshoot past the threshold required to reach maxTiltAngle.")]
    [Range(1f, 90f)]
    public float tiltRampRange = 30f;

    [Tooltip("Degrees per second the tilt smoothly moves toward its target. " +
             "Higher = snappier. Set to 0 for instant.")]
    [Range(0f, 720f)]
    public float tiltSmoothSpeed = 200f;

    [Tooltip("Gently tilt toward the player's vertical position even inside the level zone.")]
    public bool tiltInLevelZone = true;

    [Tooltip("Maximum tilt (degrees) inside the level zone.")]
    [Range(0f, 44f)]
    public float maxLevelZoneTilt = 8f;

    // ──────────────────────────────────────────────────────────
    //  Inspector – Flip
    // ──────────────────────────────────────────────────────────

    [Header("Horizontal Flip")]
    [Tooltip("Flip the sprite so it always faces the player horizontally.")]
    public bool enableFlip = true;

    [Tooltip("Which direction the sprite art faces before any flipping.")]
    public enum DefaultFacing { Right, Left }
    public DefaultFacing defaultFacing = DefaultFacing.Right;

    [Tooltip("Horizontal dead-zone in world units. Prevents flip jitter when the player " +
             "is nearly directly above or below.")]
    [Range(0f, 2f)]
    public float flipDeadZone = 0.1f;

    // ──────────────────────────────────────────────────────────
    //  Inspector – Update Mode
    // ──────────────────────────────────────────────────────────

    public enum UpdateFrequency { EveryFrame, EveryFixedUpdate, Manual }
    [Tooltip("When to run the evaluation logic.")]
    public UpdateFrequency updateMode = UpdateFrequency.EveryFrame;

    // ──────────────────────────────────────────────────────────
    //  Inspector – Debug
    // ──────────────────────────────────────────────────────────

    [Header("Debug")]
    public bool showDebugRay = true;

    // ──────────────────────────────────────────────────────────
    //  Private state
    // ──────────────────────────────────────────────────────────

    private SpriteRenderer _sr;

    private enum SpriteZone { Unset, Idle, Level, Above, Below }
    private SpriteZone _currentZone = SpriteZone.Unset;

    // The single source-of-truth for tilt — never read back from the transform.
    // Stores the UNSIGNED target and current values; sign is applied at write-time
    // based on the flip state so the visual direction is always correct.
    private float _targetTiltMagnitude = 0f;   // target abs value, 0..maxTiltAngle
    private float _targetTiltSign = 0f;   // +1 up, -1 down, 0 flat
    private float _smoothedTilt = 0f;   // actual signed value written to Z

    // ──────────────────────────────────────────────────────────
    //  Unity lifecycle
    // ──────────────────────────────────────────────────────────

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        FindPlayer();
        Evaluate();
    }

    /// <summary>
    /// Locates the player transform. Priority order:
    ///   1. Already assigned (e.g. set externally at runtime)
    ///   2. GameObject tagged "Player"
    ///   3. First PlayerRotation component found in the scene
    /// </summary>
    private void FindPlayer()
    {
        if (playerTransform) return;

        // Tag lookup – fastest path
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go)
        {
            playerTransform = go.transform;
            return;
        }

        // Component lookup – works even when the tag is missing
        var pr = FindFirstObjectByType<PlayerRotation>();
        if (pr)
        {
            playerTransform = pr.transform;
            return;
        }

        Debug.LogWarning($"[DirectionalSpriteSwapper] No player found. ({gameObject.name})", this);
    }

    void Update()
    {
        if (updateMode == UpdateFrequency.EveryFrame)
            Evaluate();
    }

    void FixedUpdate()
    {
        if (updateMode == UpdateFrequency.EveryFixedUpdate)
            Evaluate();
    }

    // ──────────────────────────────────────────────────────────
    //  Public entry point
    // ──────────────────────────────────────────────────────────

    public void Evaluate()
    {
        // Re-find the player if the reference has gone stale (e.g. after respawn/death)
        if (!playerTransform)
            FindPlayer();

        if (!playerTransform) return;

        Vector3 origin = useSpriteCentre ? (Vector3)_sr.bounds.center : transform.position;
        Vector3 toPlayer = playerTransform.position - origin;
        float distance = toPlayer.magnitude;

        // ── Range check ───────────────────────────────────────
        if (distance > detectionRange)
        {
            SetZone(SpriteZone.Idle);
            SmoothTiltToward(0f, 0f);   // return to flat
            WriteTilt();
            return;
        }

        // ── Elevation angle ───────────────────────────────────
        float hDist = new Vector2(toPlayer.x, toPlayer.z).magnitude;
        float elevationAngle = Mathf.Atan2(toPlayer.y, hDist) * Mathf.Rad2Deg;

        // ── Zone ──────────────────────────────────────────────
        SpriteZone zone;
        if (elevationAngle > angleThreshold) zone = SpriteZone.Above;
        else if (elevationAngle < -angleThreshold) zone = SpriteZone.Below;
        else zone = SpriteZone.Level;

        SetZone(zone);

        // ── Flip (do this BEFORE tilt so WriteTilt can read the current flipX) ──
        if (enableFlip)
            ApplyFlip(toPlayer.x);

        // ── Tilt target ───────────────────────────────────────
        if (enableTilt)
            ComputeTiltTarget(zone, elevationAngle);
        else
            SmoothTiltToward(0f, 0f);

        WriteTilt();

        // ── Debug ─────────────────────────────────────────────
        if (showDebugRay)
        {
            Color c = zone == SpriteZone.Above ? Color.green
                    : zone == SpriteZone.Below ? Color.red
                    : Color.yellow;
            Debug.DrawRay(origin, toPlayer.normalized * 2f, c);
        }
    }

    // ──────────────────────────────────────────────────────────
    //  Zone / sprite
    // ──────────────────────────────────────────────────────────

    private void SetZone(SpriteZone zone)
    {
        if (zone == _currentZone) return;
        _currentZone = zone;

        Sprite target = zone switch
        {
            SpriteZone.Idle => idleSprite,
            SpriteZone.Above => aboveSprite,
            SpriteZone.Below => belowSprite,
            _ => levelSprite,
        };

        if (!target)
        {
            Debug.LogWarning($"[DirectionalSpriteSwapper] Sprite for zone '{zone}' is not assigned. ({gameObject.name})", this);
            return;
        }

        _sr.sprite = target;
    }

    // ──────────────────────────────────────────────────────────
    //  Flip
    // ──────────────────────────────────────────────────────────

    private void ApplyFlip(float horizontalDelta)
    {
        if (Mathf.Abs(horizontalDelta) < flipDeadZone) return;

        bool playerRight = horizontalDelta > 0f;
        _sr.flipX = defaultFacing == DefaultFacing.Right ? !playerRight : playerRight;
    }

    // ──────────────────────────────────────────────────────────
    //  Tilt – compute target
    // ──────────────────────────────────────────────────────────

    private void ComputeTiltTarget(SpriteZone zone, float elevationAngle)
    {
        switch (zone)
        {
            case SpriteZone.Above:
                {
                    float t = Mathf.Clamp01((elevationAngle - angleThreshold) / tiltRampRange);
                    SmoothTiltToward(t * maxTiltAngle, -1f);   // nose up (negative Z = clockwise = tilts top toward player)
                    break;
                }
            case SpriteZone.Below:
                {
                    float t = Mathf.Clamp01((-angleThreshold - elevationAngle) / tiltRampRange);
                    SmoothTiltToward(t * maxTiltAngle, +1f);   // nose down (positive Z = counter-clockwise = tilts top away from player)
                    break;
                }
            case SpriteZone.Level:
                {
                    if (tiltInLevelZone)
                    {
                        float n = elevationAngle / angleThreshold;   // -1..+1
                        float sign = n >= 0f ? -1f : 1f;
                        SmoothTiltToward(Mathf.Abs(n) * maxLevelZoneTilt, sign);
                    }
                    else
                    {
                        SmoothTiltToward(0f, 0f);
                    }
                    break;
                }
            default:
                SmoothTiltToward(0f, 0f);
                break;
        }
    }

    /// <summary>
    /// Moves _smoothedTilt toward (magnitude * sign) without ever reading localEulerAngles.
    /// Using a single signed float avoids the 0/360 Euler wrap that caused the jump.
    /// </summary>
    private void SmoothTiltToward(float magnitude, float sign)
    {
        float target = magnitude * sign;

        _smoothedTilt = tiltSmoothSpeed > 0f
            ? Mathf.MoveTowards(_smoothedTilt, target, tiltSmoothSpeed * Time.deltaTime)
            : target;
    }

    // ──────────────────────────────────────────────────────────
    //  Tilt – write to transform
    // ──────────────────────────────────────────────────────────

    private void WriteTilt()
    {
        // When flipX is active the local X-axis is mirrored, which reverses the
        // visual direction of any Z rotation. Negate to compensate.
        float visualTilt = (enableFlip && _sr.flipX) ? -_smoothedTilt : _smoothedTilt;

        // Build a clean quaternion instead of writing to localEulerAngles.
        // This avoids Unity re-decomposing the angles and keeps X/Y at zero.
        transform.localRotation = Quaternion.Euler(0f, 0f, visualTilt);
    }

    // ──────────────────────────────────────────────────────────
    //  Public API
    // ──────────────────────────────────────────────────────────

    /// <summary>Instantly snaps tilt to zero without waiting for smoothing.</summary>
    public void ResetTilt()
    {
        _smoothedTilt = 0f;
        transform.localRotation = Quaternion.identity;
    }

    // ──────────────────────────────────────────────────────────
    //  Gizmos
    // ──────────────────────────────────────────────────────────

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position;

        // Detection range circle
        if (showRangeGizmo)
        {
            UnityEditor.Handles.color = new Color(1f, 1f, 1f, 0.2f);
            UnityEditor.Handles.DrawWireDisc(origin, Vector3.forward, detectionRange);
        }

        float len = Mathf.Min(1.5f, detectionRange * 0.4f);
        float threshRad = angleThreshold * Mathf.Deg2Rad;

        Vector3 aboveDir = new Vector3(Mathf.Cos(threshRad), Mathf.Sin(threshRad), 0f);
        Vector3 belowDir = new Vector3(Mathf.Cos(-threshRad), Mathf.Sin(-threshRad), 0f);

        UnityEditor.Handles.color = new Color(0f, 1f, 0f, 0.5f);
        UnityEditor.Handles.DrawLine(origin, origin + aboveDir * len);
        UnityEditor.Handles.DrawLine(origin, origin - aboveDir * len);

        UnityEditor.Handles.color = new Color(1f, 0f, 0f, 0.5f);
        UnityEditor.Handles.DrawLine(origin, origin + belowDir * len);
        UnityEditor.Handles.DrawLine(origin, origin - belowDir * len);

        UnityEditor.Handles.color = new Color(1f, 1f, 0f, 0.15f);
        UnityEditor.Handles.DrawSolidArc(origin, Vector3.forward,
            Quaternion.AngleAxis(-angleThreshold, Vector3.forward) * Vector3.right,
            angleThreshold * 2f, len);

        if (enableTilt)
        {
            UnityEditor.Handles.color = new Color(0f, 1f, 0.5f, 0.07f);
            UnityEditor.Handles.DrawSolidArc(origin, Vector3.forward,
                Quaternion.AngleAxis(angleThreshold, Vector3.forward) * Vector3.right,
                tiltRampRange, len);

            UnityEditor.Handles.color = new Color(1f, 0.3f, 0f, 0.07f);
            UnityEditor.Handles.DrawSolidArc(origin, Vector3.forward,
                Quaternion.AngleAxis(-(angleThreshold + tiltRampRange), Vector3.forward) * Vector3.right,
                tiltRampRange, len);
        }

        if (Application.isPlaying && playerTransform)
        {
            string flipStr = enableFlip && _sr ? $"  flip={_sr.flipX}" : "";
            UnityEditor.Handles.Label(origin + Vector3.up * 0.7f,
                $"Zone: {_currentZone}  tilt={_smoothedTilt:F1}°{flipStr}");
        }
    }
#endif
}