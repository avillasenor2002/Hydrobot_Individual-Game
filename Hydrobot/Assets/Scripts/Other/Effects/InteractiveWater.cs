using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

/// <summary>
/// InteractiveWater — 2D Platformer Water with Sprite Shape Ripples
///
/// SETUP INSTRUCTIONS:
/// ──────────────────────────────────────────────────────────────────
/// 1. Draw your water shape with the SpriteShapeController as normal in
///    the editor. Closed profile, solid fill. Shape and scale are yours —
///    this script never touches them.
///
/// 2. The script identifies "surface points" as any spline point whose Y
///    is at or above the midpoint of the shape's bounding box. Make sure
///    your top edge points are clearly higher than your bottom corners.
///    Arrange the surface points left-to-right in the spline.
///
/// 3. A PolygonCollider2D on the same GameObject is set to Trigger
///    automatically and shaped to the bounding box of the spline at
///    startup. Objects with a Rigidbody2D entering it create splashes.
///
/// 4. (Optional) Assign splashParticles / splashSound for audio-visual FX.
/// ──────────────────────────────────────────────────────────────────
/// </summary>
[RequireComponent(typeof(SpriteShapeController))]
[RequireComponent(typeof(PolygonCollider2D))]
public class InteractiveWater : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────

    [Header("Spring Physics")]
    [Tooltip("How quickly each spring returns to rest. Higher = faster oscillation.")]
    [Range(1f, 200f)]
    public float springStiffness = 40f;

    [Tooltip("Velocity damping coefficient. Low values let waves travel far; high values kill them quickly.")]
    [Range(0f, 8f)]
    public float damping = 1.2f;

    [Tooltip("Wave propagation speed in local units per second. Controls how fast ripples travel across the surface.")]
    [Range(0.5f, 20f)]
    public float waveSpeed = 6f;

    [Header("Idle Ripple")]
    [Tooltip("Gentle ambient wave always present on the surface.")]
    public bool idleRipple = true;

    [Tooltip("Peak height of the idle wave in local units.")]
    [Range(0f, 0.5f)]
    public float idleAmplitude = 0.04f;

    [Tooltip("Speed the idle wave travels across the surface.")]
    [Range(0f, 5f)]
    public float idleSpeed = 0.8f;

    [Tooltip("Spatial frequency of the idle wave.")]
    [Range(0f, 10f)]
    public float idleFrequency = 2.0f;

    [Header("Collision Response")]
    [Tooltip("Multiplies the entering object's downward speed into splash force.")]
    [Range(0f, 10f)]
    public float velocityImpactScale = 2f;

    [Tooltip("Minimum splash force regardless of object speed.")]
    [Range(0f, 20f)]
    public float minimumImpulse = 2f;

    [Tooltip("Number of springs disturbed either side of the impact point.")]
    [Range(1, 20)]
    public int impactRadius = 4;

    [Header("Effects")]
    public ParticleSystem splashParticles;
    public AudioClip splashSound;
    [Range(0f, 1f)]
    public float splashVolume = 0.7f;

    // ── Private ──────────────────────────────────────────────────────

    private SpriteShapeController _shape;
    private Spline _spline;
    private PolygonCollider2D _poly;

    // Surface spring state
    private int _surfaceCount;       // how many spline points are on the surface
    private int[] _surfaceIndices;     // their indices in the spline
    private float[] _restY;             // original local Y of each surface point
    private float[] _xLocal;            // original local X of each surface point
    private float[] _vel;               // spring velocity
    private float[] _disp;              // displacement from rest

    // Derived from spline bounds at startup — used for collider & entry check
    private float _boundsMinX, _boundsMaxX, _boundsMinY, _boundsMaxY;

    private readonly HashSet<int> _bodiesInside = new();

    // ── Lifecycle ────────────────────────────────────────────────────

    private void Awake()
    {
        _shape = GetComponent<SpriteShapeController>();
        _poly = GetComponent<PolygonCollider2D>();

        ReadSpline();
        BuildCollider();
    }

    private void FixedUpdate()
    {
        StepSprings(Time.fixedDeltaTime);
        SpreadWaves(Time.fixedDeltaTime);
        WriteToSpline();
    }

    // ── Read existing spline ─────────────────────────────────────────

    /// <summary>
    /// Reads the SpriteShapeController's existing spline.
    /// Surface points are those whose resting Y is above the shape's vertical midpoint.
    /// No points are added, removed, or reordered.
    /// </summary>
    private void ReadSpline()
    {
        _spline = _shape.spline;
        int total = _spline.GetPointCount();

        if (total < 3)
        {
            Debug.LogError($"[InteractiveWater] Spline on '{name}' has fewer than 3 points. " +
                           "Please draw your water shape in the SpriteShapeController first.");
            enabled = false;
            return;
        }

        // First pass: compute bounding box of all spline points
        _boundsMinX = float.MaxValue; _boundsMaxX = float.MinValue;
        _boundsMinY = float.MaxValue; _boundsMaxY = float.MinValue;

        for (int i = 0; i < total; i++)
        {
            Vector3 p = _spline.GetPosition(i);
            if (p.x < _boundsMinX) _boundsMinX = p.x;
            if (p.x > _boundsMaxX) _boundsMaxX = p.x;
            if (p.y < _boundsMinY) _boundsMinY = p.y;
            if (p.y > _boundsMaxY) _boundsMaxY = p.y;
        }

        float midY = (_boundsMinY + _boundsMaxY) * 0.5f;

        // Second pass: identify surface points (above the vertical midpoint)
        var surfaceIdxList = new List<int>();
        for (int i = 0; i < total; i++)
        {
            if (_spline.GetPosition(i).y >= midY)
                surfaceIdxList.Add(i);
        }

        if (surfaceIdxList.Count < 2)
        {
            Debug.LogError($"[InteractiveWater] Could not find at least 2 surface points on '{name}'. " +
                           "Make sure the top edge of your spline is above the vertical midpoint of the shape.");
            enabled = false;
            return;
        }

        // Sort left-to-right by X so springs are in spatial order
        surfaceIdxList.Sort((a, b) =>
            _spline.GetPosition(a).x.CompareTo(_spline.GetPosition(b).x));

        _surfaceCount = surfaceIdxList.Count;
        _surfaceIndices = surfaceIdxList.ToArray();
        _restY = new float[_surfaceCount];
        _xLocal = new float[_surfaceCount];
        _vel = new float[_surfaceCount];
        _disp = new float[_surfaceCount];

        for (int i = 0; i < _surfaceCount; i++)
        {
            Vector3 p = _spline.GetPosition(_surfaceIndices[i]);
            _xLocal[i] = p.x;
            _restY[i] = p.y;
        }
    }

    // ── Collider ─────────────────────────────────────────────────────

    /// <summary>
    /// Sizes the PolygonCollider2D to a rectangle matching the spline's
    /// bounding box. Purely in local space — scale is never touched.
    /// </summary>
    private void BuildCollider()
    {
        if (_surfaceIndices == null) return; // ReadSpline failed

        _poly.isTrigger = true;
        float top = _boundsMaxY + 0.15f; // slight overshoot to catch objects landing on surface

        _poly.SetPath(0, new Vector2[]
        {
            new(_boundsMinX, top        ),
            new(_boundsMaxX, top        ),
            new(_boundsMaxX, _boundsMinY),
            new(_boundsMinX, _boundsMinY),
        });
    }

    // ── Wave simulation ──────────────────────────────────────────────
    //
    // Uses the 1-D discrete wave equation:
    //
    //   acc[i] = waveSpeed² × (disp[i-1] - 2·disp[i] + disp[i+1]) / dx²
    //            - springStiffness × disp[i]    ← restoring force
    //            - damping × vel[i]             ← viscous drag
    //
    // This gives true travelling waves with a finite propagation speed,
    // rather than the instantaneous jello-coupling of the old spread pass.

    private void StepSprings(float dt)
    {
        // Average spacing between surface springs in local units
        float dx = (_surfaceCount > 1)
            ? (_boundsMaxX - _boundsMinX) / (_surfaceCount - 1)
            : 1f;

        float c2 = waveSpeed * waveSpeed;        // wave speed squared
        float dx2 = dx * dx;

        for (int i = 0; i < _surfaceCount; i++)
        {
            // Laplacian: second spatial derivative (finite difference)
            // Boundary springs treat the edge as a free end (zero-gradient).
            float dLeft = (i > 0) ? _disp[i - 1] : _disp[i];
            float dRight = (i < _surfaceCount - 1) ? _disp[i + 1] : _disp[i];
            float laplacian = (dLeft - 2f * _disp[i] + dRight) / dx2;

            float acc = c2 * laplacian                  // wave propagation
                      - springStiffness * _disp[i]      // restoring spring
                      - damping * _vel[i];              // viscous damping

            _vel[i] += acc * dt;
            _disp[i] += _vel[i] * dt;
        }
    }

    // SpreadWaves is no longer needed — propagation is handled in StepSprings
    // via the Laplacian. Kept as a no-op so call-sites don't need changing.
    private void SpreadWaves(float dt) { }

    // ── Spline write-back ────────────────────────────────────────────

    private void WriteToSpline()
    {
        float time = Time.time;
        float spanX = _boundsMaxX - _boundsMinX;

        for (int i = 0; i < _surfaceCount; i++)
        {
            float idle = 0f;
            if (idleRipple && spanX > 0f)
            {
                float phase = ((_xLocal[i] - _boundsMinX) / spanX)
                              * idleFrequency * Mathf.PI * 2f;
                idle = Mathf.Sin(phase - time * idleSpeed) * idleAmplitude;
            }

            float y = _restY[i] + _disp[i] + idle;
            _spline.SetPosition(_surfaceIndices[i], new Vector3(_xLocal[i], y, 0f));
        }

        _shape.BakeMesh();
    }

    // ── Public splash API ────────────────────────────────────────────

    /// <summary>
    /// Disturb the surface at a world-space X. Positive impulse = push down.
    /// Uses a narrow Gaussian so the impact point gets the full force and
    /// neighbours get an exponentially smaller kick — real water behaviour.
    /// </summary>
    public void Splash(float worldX, float impulse)
    {
        float localX = transform.InverseTransformPoint(new Vector3(worldX, 0f, 0f)).x;
        int center = LocalXToSurfaceIndex(localX);
        if (center < 0) return;

        // Gaussian width: 1 spring-spacing unit gives a sharp, localised dip.
        // impactRadius controls how many indices we bother computing.
        float sigma2 = impactRadius * impactRadius * 0.18f; // tighter than before

        int lo = Mathf.Max(0, center - impactRadius * 2);
        int hi = Mathf.Min(_surfaceCount - 1, center + impactRadius * 2);

        for (int i = lo; i <= hi; i++)
        {
            float dist = i - center;
            float weight = Mathf.Exp(-(dist * dist) / sigma2);
            _vel[i] -= impulse * weight;
        }

        SpawnFX(worldX);
    }

    // ── Trigger detection ────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        Rigidbody2D rb = other.attachedRigidbody;
        if (rb == null) return;

        int id = rb.GetInstanceID();
        if (_bodiesInside.Contains(id)) return;
        _bodiesInside.Add(id);

        // Ignore objects that are already deep inside (entered from below / sides)
        float localY = transform.InverseTransformPoint(rb.position).y;
        float midY = (_boundsMinY + _boundsMaxY) * 0.5f;
        if (localY < midY) return;

        float downSpeed = Mathf.Max(0f, -rb.velocity.y);
        float impulse = Mathf.Max(minimumImpulse, downSpeed * velocityImpactScale);
        Splash(rb.position.x, impulse);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Rigidbody2D rb = other.attachedRigidbody;
        if (rb == null) return;

        _bodiesInside.Remove(rb.GetInstanceID());
        Splash(rb.position.x, -minimumImpulse * 0.4f);
    }

    // ── Helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns the surface spring index closest to a given local-space X,
    /// or -1 if outside the surface span.
    /// </summary>
    private int LocalXToSurfaceIndex(float localX)
    {
        if (localX < _boundsMinX || localX > _boundsMaxX) return -1;

        int best = 0;
        float bestDist = float.MaxValue;
        for (int i = 0; i < _surfaceCount; i++)
        {
            float d = Mathf.Abs(_xLocal[i] - localX);
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return best;
    }

    private void SpawnFX(float worldX)
    {
        // Surface Y in world space: use the world position of the surface
        float worldY = transform.TransformPoint(new Vector3(0f, _boundsMaxY, 0f)).y;
        Vector3 pos = new(worldX, worldY, 0f);

        if (splashParticles != null)
        {
            ParticleSystem ps = Instantiate(splashParticles, pos, Quaternion.identity);
            ps.Play();
            Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
        }

        if (splashSound != null)
            AudioSource.PlayClipAtPoint(splashSound, pos, splashVolume);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Only draw if we've already read the spline
        if (_surfaceIndices == null) return;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.35f);

        float midX = (_boundsMinX + _boundsMaxX) * 0.5f;
        float midY = (_boundsMinY + _boundsMaxY) * 0.5f;
        Gizmos.DrawWireCube(new Vector3(midX, midY, 0f),
                            new Vector3(_boundsMaxX - _boundsMinX,
                                        _boundsMaxY - _boundsMinY, 0f));

        // Mark each surface spring
        Gizmos.color = new Color(1f, 1f, 0f, 0.8f);
        for (int i = 0; i < _surfaceCount; i++)
            Gizmos.DrawSphere(new Vector3(_xLocal[i], _restY[i], 0f), 0.05f);
    }
#endif
}