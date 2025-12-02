using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D; // SpriteShapeController

[RequireComponent(typeof(SpriteShapeController))]
[RequireComponent(typeof(PolygonCollider2D))]
public class WaterSurface : MonoBehaviour
{
    [Header("Wave simulation")]
    public float stiffness = 5f;           // spring stiffness
    public float damping = 0.9f;           // damping per update (0..1)
    public float spread = 0.25f;           // how fast waves spread to neighbors
    public float nodeMass = 1f;            // mass used for acceleration calc (affects response)

    [Header("Buoyancy")]
    public float buoyancyFactor = 40f;     // upward force per unit submerged depth (tune per project)
    public float linearDrag = 2f;          // applies drag while submerged
    public float angularDrag = 1f;         // angular drag while submerged

    [Header("Splash & impact")]
    public GameObject splashPrefab;        // optional particle prefab for splashes
    public float splashVelocityThreshold = 1.5f;   // min vertical speed to create big splash
    public float splashForce = 3f;         // impulse added to nodes on impact

    // internals
    private SpriteShapeController ssc;
    private PolygonCollider2D poly;
    private int nodeCount;
    private float[] displacements;
    private float[] velocities;
    private Vector3[] basePositionsLocal; // original spline positions (local)
    private bool initialized = false;

    // bodies inside water
    private readonly List<Rigidbody2D> submergedBodies = new List<Rigidbody2D>();

    private void Awake()
    {
        ssc = GetComponent<SpriteShapeController>();
        poly = GetComponent<PolygonCollider2D>();
        if (poly == null) poly = gameObject.AddComponent<PolygonCollider2D>();
        poly.isTrigger = true;

        InitializeNodes();
    }

    private void InitializeNodes()
    {
        var spline = ssc.spline;
        nodeCount = spline.GetPointCount();
        displacements = new float[nodeCount];
        velocities = new float[nodeCount];
        basePositionsLocal = new Vector3[nodeCount];

        for (int i = 0; i < nodeCount; i++)
        {
            Vector3 p = spline.GetPosition(i); // local space
            basePositionsLocal[i] = p;
            displacements[i] = 0f;
            velocities[i] = 0f;
        }

        initialized = true;
    }

    private void Update()
    {
        if (!initialized)
            InitializeNodes();

        SimulateWaves(Time.deltaTime);
        UpdateSpline();

        // Apply buoyancy to bodies inside the water
        ApplyBuoyancyToBodies();
    }

    // Spring-based 1D wave simulation
    private void SimulateWaves(float dt)
    {
        if (nodeCount <= 0) return;

        // spring acceleration to restore to 0 displacement
        for (int i = 0; i < nodeCount; i++)
        {
            float accel = (-stiffness * displacements[i]) / nodeMass;
            velocities[i] += accel * dt;
            velocities[i] *= Mathf.Pow(damping, dt * 60f); // frame rate independent-ish damping
        }

        // propagate to neighbors (simple discrete propagation)
        float[] leftDeltas = new float[nodeCount];
        float[] rightDeltas = new float[nodeCount];

        // neighbor coupling
        for (int j = 0; j < 8; j++) // multiple passes for smoother spread
        {
            for (int i = 0; i < nodeCount; i++)
            {
                if (i > 0)
                {
                    float delta = spread * (displacements[i] - displacements[i - 1]);
                    velocities[i - 1] += delta * 0.5f;
                }
                if (i < nodeCount - 1)
                {
                    float delta = spread * (displacements[i] - displacements[i + 1]);
                    velocities[i + 1] += delta * 0.5f;
                }
            }
            // update displacements a bit
            for (int i = 0; i < nodeCount; i++)
                displacements[i] += velocities[i] * dt;
        }
    }

    private void UpdateSpline()
    {
        var spline = ssc.spline;

        for (int i = 0; i < nodeCount; i++)
        {
            Vector3 local = basePositionsLocal[i];
            local.y = basePositionsLocal[i].y + displacements[i];

            // SetPosition expects local space point
            spline.SetPosition(i, local);
        }

        // After editing spline, must mark sprite shape controller geometry dirty
#if UNITY_EDITOR
        // In Editor, you may need to force rebuild; in runtime it's often automatic.
#endif
        ssc.BakeCollider(); // update collider geometry to match spline (keeps the polygon collider up to date if using that)
    }

    // PUBLIC: get world Y of water surface at given worldX by interpolating between nodes
    public float GetWaterHeightAtX(float worldX)
    {
        // transform nodes into world positions and find segment that straddles worldX
        float bestX, nextX;
        Vector3 wp, wpNext;
        for (int i = 0; i < nodeCount - 1; i++)
        {
            wp = transform.TransformPoint(basePositionsLocal[i] + new Vector3(0, displacements[i], 0));
            wpNext = transform.TransformPoint(basePositionsLocal[i + 1] + new Vector3(0, displacements[i + 1], 0));
            bestX = wp.x;
            nextX = wpNext.x;

            if ((worldX >= bestX && worldX <= nextX) || (worldX >= nextX && worldX <= bestX))
            {
                float t = Mathf.InverseLerp(bestX, nextX, worldX);
                return Mathf.Lerp(wp.y, wpNext.y, t);
            }
        }

        // outside range: use first or last node y
        Vector3 first = transform.TransformPoint(basePositionsLocal[0] + new Vector3(0, displacements[0], 0));
        Vector3 last = transform.TransformPoint(basePositionsLocal[nodeCount - 1] + new Vector3(0, displacements[nodeCount - 1], 0));
        if (worldX < first.x) return first.y;
        return last.y;
    }

    private void ApplyBuoyancyToBodies()
    {
        if (submergedBodies.Count == 0) return;

        for (int i = submergedBodies.Count - 1; i >= 0; i--)
        {
            Rigidbody2D rb = submergedBodies[i];
            if (rb == null)
            {
                submergedBodies.RemoveAt(i);
                continue;
            }

            Collider2D col = rb.GetComponent<Collider2D>();
            if (col == null) continue;

            Bounds b = col.bounds;
            float sampleX = rb.position.x; // center X sample; could sample multiple points for larger objects
            float waterY = GetWaterHeightAtX(sampleX);

            float bottomY = b.min.y;
            float topY = b.max.y;
            float objectHeight = topY - bottomY;
            float submergedDepth = Mathf.Clamp01((waterY - bottomY) / (objectHeight + 0.0001f)); // fraction submerged [0..1]

            if (submergedDepth > 0f)
            {
                float submergedDepthWorld = Mathf.Clamp(waterY - bottomY, 0, objectHeight);
                // force proportional to submerged depth and object's area
                float force = buoyancyFactor * submergedDepthWorld * (rb.mass); // mass included so heavier objects need more buoyancy
                rb.AddForce(Vector2.up * force * Time.deltaTime * 60f, ForceMode2D.Force);

                // simple drag
                rb.velocity *= 1f / (1f + linearDrag * submergedDepth * Time.deltaTime);
                rb.angularVelocity *= 1f / (1f + angularDrag * submergedDepth * Time.deltaTime);

                // Add small horizontal wave influence: approximate slope by sampling neighbor water heights
                float leftY = GetWaterHeightAtX(sampleX - 0.1f);
                float rightY = GetWaterHeightAtX(sampleX + 0.1f);
                float slope = (rightY - leftY) * 5f;
                rb.AddForce(new Vector2(slope * rb.mass * 0.1f, 0) * Time.deltaTime * 60f, ForceMode2D.Force);
            }
        }
    }

    // collisions: track rigidbodies entering/exiting the polygon trigger
    private void OnTriggerEnter2D(Collider2D collider)
    {
        Rigidbody2D rb = collider.attachedRigidbody;
        if (rb != null && !submergedBodies.Contains(rb))
        {
            submergedBodies.Add(rb);

            // create a small splash for entering
            Vector2 contactPoint = collider.ClosestPoint(transform.position);
            DoSplash(contactPoint, rb.velocity.y);
        }
    }

    private void OnTriggerExit2D(Collider2D collider)
    {
        Rigidbody2D rb = collider.attachedRigidbody;
        if (rb != null)
        {
            submergedBodies.Remove(rb);
        }
    }

    // Call this for an impact at world position and with some incoming vertical velocity (optional)
    public void AddImpact(Vector2 worldPos, float incomingYVelocity, float force = 1f)
    {
        // find closest node by world X
        float worldX = worldPos.x;
        int closest = 0;
        float bestDist = float.MaxValue;
        for (int i = 0; i < nodeCount; i++)
        {
            Vector3 nodeWorld = transform.TransformPoint(basePositionsLocal[i] + new Vector3(0, displacements[i], 0));
            float d = Mathf.Abs(worldX - nodeWorld.x);
            if (d < bestDist)
            {
                bestDist = d;
                closest = i;
            }
        }

        float impact = (Mathf.Abs(incomingYVelocity) + force) * splashForce;
        velocities[closest] += impact;

        // optionally spawn a particle splash
        DoSplash(worldPos, incomingYVelocity);
    }

    private void DoSplash(Vector2 worldPos, float incomingYVelocity)
    {
        if (splashPrefab != null)
        {
            var go = Instantiate(splashPrefab, worldPos, Quaternion.identity);
            // if particle system should be auto destroyed:
            var ps = go.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(go, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(go, 2f);
            }
        }

        // make a stronger ripple if impact is strong
        if (Mathf.Abs(incomingYVelocity) > splashVelocityThreshold)
        {
            // approximate worldPos.x for AddImpact
            AddImpact(worldPos, incomingYVelocity, Mathf.Abs(incomingYVelocity));
        }
    }
}
