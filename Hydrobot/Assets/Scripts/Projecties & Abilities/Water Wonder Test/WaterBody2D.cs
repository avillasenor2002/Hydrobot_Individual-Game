using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// WaterBody2D – attach to a GameObject that has a SpriteRenderer using WonderWater.shader.
/// Handles:
///   • surface ripple / splash particles when objects enter/exit
///   • surface wave displacement driven by nearby Rigidbody2Ds
///   • underwater post-process tint driven by a custom Volume or a plain Camera override
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PolygonCollider2D))]
public class WaterBody2D : MonoBehaviour
{
    // ──────────────────────────────────────────────────────────
    //  Inspector
    // ──────────────────────────────────────────────────────────

    [Header("Shader Material")]
    [Tooltip("The material using WonderWater.shader.  Leave blank to use the SpriteRenderer's material.")]
    public Material waterMaterial;

    [Header("Surface Interaction")]
    [Tooltip("Particle system to spawn at splash entry / exit points.")]
    public ParticleSystem splashParticlePrefab;
    [Tooltip("How hard an object must hit the surface to trigger a big splash.")]
    public float splashVelocityThreshold = 2f;
    [Tooltip("Max simultaneous active splash instances.")]
    public int maxSplashInstances = 8;

    [Header("Ripple Shader Parameters")]
    [Tooltip("How much a near-surface rigidbody deflects the shader surface wave amplitude.")]
    public float rippleAmplitudeMultiplier = 3f;
    [Tooltip("Seconds for the extra wave amplitude to settle back to rest.")]
    public float rippleDecayTime = 1.2f;

    [Header("Underwater Effect")]
    [Tooltip("Camera that renders the scene (auto-found if left empty).")]
    public Camera sceneCamera;
    [Tooltip("Enable a subtle full-screen tint while the player camera is underwater.")]
    public bool enableUnderwaterOverlay = true;
    [Tooltip("Color of the overlay when the camera is fully submerged.")]
    public Color underwaterOverlayColor = new Color(0.1f, 0.4f, 0.9f, 0.35f);
    [Tooltip("Transition height in world units around the water surface.")]
    public float surfaceTransitionRange = 0.4f;

    // ──────────────────────────────────────────────────────────
    //  Private state
    // ──────────────────────────────────────────────────────────

    private SpriteRenderer _sr;
    private PolygonCollider2D _col;
    private Material _mat;

    // Shader property IDs (cached for performance)
    private static readonly int ID_SurfaceWaveAmp  = Shader.PropertyToID("_SurfaceWaveAmp");
    private static readonly int ID_DistortionStr   = Shader.PropertyToID("_DistortionStrength");
    private static readonly int ID_CausticsStr     = Shader.PropertyToID("_CausticsStrength");

    // Runtime
    private float _baseWaveAmp;
    private float _baseDistortion;
    private float _baseCaustics;
    private float _currentExtraAmp;
    private Coroutine _rippleDecayCoroutine;

    // Pool for splash particles
    private readonly Queue<ParticleSystem> _splashPool = new();
    private readonly List<ParticleSystem>  _activeSplashes = new();

    // Underwater overlay mesh
    private GameObject  _overlayGO;
    private MeshRenderer _overlayMR;
    private Material    _overlayMat;
    private float       _waterSurfaceWorldY;

    // Tracked submerged objects (so we only fire enter/exit once per object)
    private readonly HashSet<Collider2D> _submerged = new();

    // ──────────────────────────────────────────────────────────
    //  Unity lifecycle
    // ──────────────────────────────────────────────────────────

    void Awake()
    {
        _sr  = GetComponent<SpriteRenderer>();
        _col = GetComponent<PolygonCollider2D>();

        // Use assigned material or clone the sprite's
        _mat = waterMaterial != null
             ? waterMaterial
             : new Material(_sr.sharedMaterial);
        _sr.material = _mat;

        // Cache base shader values
        _baseWaveAmp   = _mat.GetFloat(ID_SurfaceWaveAmp);
        _baseDistortion = _mat.GetFloat(ID_DistortionStr);
        _baseCaustics   = _mat.GetFloat(ID_CausticsStr);

        // Cache water surface Y in world space (top of sprite bounds)
        _waterSurfaceWorldY = _sr.bounds.max.y;

        if (!sceneCamera)
            sceneCamera = Camera.main;

        if (enableUnderwaterOverlay)
            BuildUnderwaterOverlay();

        // Pre-warm splash pool
        if (splashParticlePrefab)
        {
            for (int i = 0; i < maxSplashInstances; i++)
            {
                var ps = Instantiate(splashParticlePrefab, transform);
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.gameObject.SetActive(false);
                _splashPool.Enqueue(ps);
            }
        }

        // Make sure the collider is a trigger
        _col.isTrigger = true;
    }

    void Update()
    {
        UpdateUnderwaterOverlay();
    }

    // ──────────────────────────────────────────────────────────
    //  Trigger events (entry / exit / stay)
    // ──────────────────────────────────────────────────────────

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_submerged.Contains(other)) return;
        _submerged.Add(other);

        float impactVelocity = 0f;
        var rb = other.attachedRigidbody;
        if (rb) impactVelocity = Mathf.Abs(rb.velocity.y);

        if (impactVelocity > splashVelocityThreshold * 0.5f)
        {
            SpawnSplash(other.bounds.center, impactVelocity, entering: true);
            TriggerRipple(impactVelocity);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        _submerged.Remove(other);

        var rb = other.attachedRigidbody;
        float exitVelocity = rb ? Mathf.Abs(rb.velocity.y) : 0f;

        if (exitVelocity > splashVelocityThreshold * 0.3f)
        {
            SpawnSplash(other.bounds.center, exitVelocity * 0.6f, entering: false);
            TriggerRipple(exitVelocity * 0.5f);
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // Keep driving small ripples from moving submerged bodies
        var rb = other.attachedRigidbody;
        if (rb && rb.velocity.magnitude > 0.5f)
        {
            float microRipple = rb.velocity.magnitude * 0.08f;
            _mat.SetFloat(ID_DistortionStr, _baseDistortion + microRipple * 0.003f);
        }
    }

    // ──────────────────────────────────────────────────────────
    //  Ripple – shader driven
    // ──────────────────────────────────────────────────────────

    private void TriggerRipple(float velocity)
    {
        float extra = Mathf.Clamp(velocity / 10f, 0.1f, 1f) * _baseWaveAmp * rippleAmplitudeMultiplier;
        _currentExtraAmp = Mathf.Max(_currentExtraAmp, extra);
        _mat.SetFloat(ID_SurfaceWaveAmp, _baseWaveAmp + _currentExtraAmp);

        if (_rippleDecayCoroutine != null) StopCoroutine(_rippleDecayCoroutine);
        _rippleDecayCoroutine = StartCoroutine(DecayRipple());
    }

    private IEnumerator DecayRipple()
    {
        float elapsed = 0f;
        float startExtra = _currentExtraAmp;
        while (elapsed < rippleDecayTime)
        {
            elapsed += Time.deltaTime;
            _currentExtraAmp = Mathf.Lerp(startExtra, 0f, elapsed / rippleDecayTime);
            _mat.SetFloat(ID_SurfaceWaveAmp, _baseWaveAmp + _currentExtraAmp);
            yield return null;
        }
        _currentExtraAmp = 0f;
        _mat.SetFloat(ID_SurfaceWaveAmp, _baseWaveAmp);
    }

    // ──────────────────────────────────────────────────────────
    //  Splash particles
    // ──────────────────────────────────────────────────────────

    private void SpawnSplash(Vector3 pos, float velocity, bool entering)
    {
        if (!splashParticlePrefab) return;

        // Clamp X to water body bounds
        var b = _sr.bounds;
        pos.x = Mathf.Clamp(pos.x, b.min.x, b.max.x);
        pos.y = _waterSurfaceWorldY;
        pos.z = transform.position.z - 0.1f;

        // Retrieve from pool or reuse oldest active
        ParticleSystem ps = GetPooledSplash();
        if (!ps) return;

        ps.transform.position = pos;
        ps.gameObject.SetActive(true);

        // Scale emission by velocity
        var main = ps.main;
        float speed = Mathf.Clamp(velocity / 12f, 0.2f, 1f);
        main.startSpeedMultiplier = speed;
        main.startSizeMultiplier  = speed;
        if (!entering) { var s = ps.shape; s.rotation = new Vector3(180, 0, 0); }

        ps.Play(true);
        _activeSplashes.Add(ps);
        StartCoroutine(ReturnSplashToPool(ps));
    }

    private ParticleSystem GetPooledSplash()
    {
        // Clean finished active splashes back into pool first
        for (int i = _activeSplashes.Count - 1; i >= 0; i--)
        {
            if (!_activeSplashes[i].isPlaying)
            {
                var done = _activeSplashes[i];
                _activeSplashes.RemoveAt(i);
                done.gameObject.SetActive(false);
                _splashPool.Enqueue(done);
            }
        }

        if (_splashPool.Count > 0) return _splashPool.Dequeue();
        if (_activeSplashes.Count > 0)
        {
            // Recycle oldest
            var oldest = _activeSplashes[0];
            _activeSplashes.RemoveAt(0);
            oldest.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return oldest;
        }
        return null;
    }

    private IEnumerator ReturnSplashToPool(ParticleSystem ps)
    {
        yield return new WaitWhile(() => ps && ps.isPlaying);
        if (!ps) yield break;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.gameObject.SetActive(false);
        _activeSplashes.Remove(ps);
        _splashPool.Enqueue(ps);
    }

    // ──────────────────────────────────────────────────────────
    //  Underwater camera overlay
    // ──────────────────────────────────────────────────────────

    private void BuildUnderwaterOverlay()
    {
        _overlayGO = new GameObject("WaterUnderwaterOverlay");
        _overlayGO.transform.SetParent(sceneCamera ? sceneCamera.transform : transform);
        _overlayGO.transform.localPosition = new Vector3(0, 0, sceneCamera ? (sceneCamera.nearClipPlane + 0.05f) : 0.1f);

        var mf = _overlayGO.AddComponent<MeshFilter>();
        mf.mesh = BuildQuad(2f, 2f);   // NDC-sized quad (will be scaled by camera)

        _overlayMR  = _overlayGO.AddComponent<MeshRenderer>();
        _overlayMat = new Material(Shader.Find("UI/Default"));
        _overlayMat.renderQueue = 4000;
        _overlayMR.material  = _overlayMat;
        _overlayMR.shadowCastingMode    = UnityEngine.Rendering.ShadowCastingMode.Off;
        _overlayMR.receiveShadows       = false;

        _overlayGO.SetActive(false);
    }

    private void UpdateUnderwaterOverlay()
    {
        if (!enableUnderwaterOverlay || !sceneCamera || _overlayGO == null) return;

        float camY    = sceneCamera.transform.position.y;
        float surfY   = _waterSurfaceWorldY;
        float depth   = surfY - camY;   // positive = camera is below surface

        float t = Mathf.Clamp01((depth + surfaceTransitionRange) / (surfaceTransitionRange * 2f));

        if (t <= 0f)
        {
            _overlayGO.SetActive(false);
            return;
        }

        _overlayGO.SetActive(true);
        Color c = underwaterOverlayColor;
        c.a *= t;
        _overlayMat.color = c;
    }

    private static Mesh BuildQuad(float w, float h)
    {
        var mesh = new Mesh();
        mesh.vertices  = new[] { new Vector3(-w,-h,0), new Vector3(w,-h,0), new Vector3(-w,h,0), new Vector3(w,h,0) };
        mesh.uv        = new[] { new Vector2(0,0), new Vector2(1,0), new Vector2(0,1), new Vector2(1,1) };
        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateNormals();
        return mesh;
    }

    // ──────────────────────────────────────────────────────────
    //  Public API
    // ──────────────────────────────────────────────────────────

    /// <summary>Call this to manually trigger a splash at a world-space X position.</summary>
    public void ForceRippleAt(float worldX, float strength = 1f)
    {
        SpawnSplash(new Vector3(worldX, _waterSurfaceWorldY, 0), strength * 8f, entering: true);
        TriggerRipple(strength * 8f);
    }

    /// <summary>Instantly set shader caustics intensity (0-1 range).</summary>
    public void SetCausticsIntensity(float t)
    {
        _mat.SetFloat(ID_CausticsStr, _baseCaustics * Mathf.Clamp01(t));
    }

    void OnDestroy()
    {
        if (_overlayGO) Destroy(_overlayGO);
    }
}
