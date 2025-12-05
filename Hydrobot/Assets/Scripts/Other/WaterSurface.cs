using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

[RequireComponent(typeof(SpriteShapeController), typeof(EdgeCollider2D))]
public class TopOnlyWater : MonoBehaviour
{
    [Header("Water Settings")]
    [SerializeField] private float springStrength = 0.002f;  // gentle spring
    [SerializeField] private float damping = 0.15f;           // more damping to prevent overshoot
    [SerializeField] private float interactionRadius = 0.2f;  // small interaction area
    [SerializeField] private float interactionForce = 0.005f; // subtle dip
    [SerializeField] private float neighborSpread = 0.01f;    // gentle ripple propagation

    [Header("Procedural Wave Noise")]
    [SerializeField] private float waveAmplitude = 0.005f;    // tiny vertical motion
    [SerializeField] private float waveFrequency = 1f;        // speed of waves
    [SerializeField] private float wavePhaseOffset = 0.5f;    // phase offset between points

    private SpriteShapeController shapeController;
    private EdgeCollider2D edgeCollider;

    private List<int> topPointIndices = new List<int>();
    private Vector3[] originalPositions;
    private Vector3[] displacedPositions;
    private float[] velocities;

    private List<Transform> interactingObjects = new List<Transform>();
    private float time;

    private void Awake()
    {
        shapeController = GetComponent<SpriteShapeController>();
        edgeCollider = GetComponent<EdgeCollider2D>();

        int totalPoints = shapeController.spline.GetPointCount();
        originalPositions = new Vector3[totalPoints];
        displacedPositions = new Vector3[totalPoints];

        for (int i = 0; i < totalPoints; i++)
        {
            Vector3 pos = shapeController.spline.GetPosition(i);
            originalPositions[i] = pos;
            displacedPositions[i] = pos;
        }

        // Identify top points
        float maxY = float.MinValue;
        for (int i = 0; i < totalPoints; i++)
            if (originalPositions[i].y > maxY) maxY = originalPositions[i].y;

        for (int i = 0; i < totalPoints; i++)
            if (Mathf.Approximately(originalPositions[i].y, maxY))
                topPointIndices.Add(i);

        velocities = new float[topPointIndices.Count];
    }

    private void FixedUpdate()
    {
        time += Time.fixedDeltaTime;

        ApplySpringPhysics();
        RipplePropagation();
        ApplyWaveNoise();
        UpdateTopSurface();
    }

    private void ApplySpringPhysics()
    {
        for (int i = 1; i < topPointIndices.Count - 1; i++)
        {
            int index = topPointIndices[i];
            float displacementY = originalPositions[index].y - displacedPositions[index].y;
            float acceleration = springStrength * displacementY - velocities[i] * damping;
            velocities[i] += acceleration * Time.fixedDeltaTime;

            foreach (Transform obj in interactingObjects)
            {
                if (obj == null) continue;
                Vector3 localObjPos = transform.InverseTransformPoint(obj.position);
                float distance = Mathf.Abs(localObjPos.x - displacedPositions[index].x);
                if (distance < interactionRadius)
                {
                    float force = (interactionRadius - distance) / interactionRadius * interactionForce;
                    velocities[i] -= force;
                }
            }

            displacedPositions[index].y += velocities[i] * Time.fixedDeltaTime;
        }
    }

    private void RipplePropagation()
    {
        float[] leftDeltas = new float[topPointIndices.Count];
        float[] rightDeltas = new float[topPointIndices.Count];

        for (int i = 1; i < topPointIndices.Count - 1; i++)
        {
            int index = topPointIndices[i];
            if (i > 0)
                leftDeltas[i] = neighborSpread * (displacedPositions[index].y - displacedPositions[topPointIndices[i - 1]].y);
            if (i < topPointIndices.Count - 1)
                rightDeltas[i] = neighborSpread * (displacedPositions[index].y - displacedPositions[topPointIndices[i + 1]].y);
        }

        for (int i = 1; i < topPointIndices.Count - 1; i++)
        {
            if (i > 0) velocities[i - 1] += leftDeltas[i];
            if (i < topPointIndices.Count - 1) velocities[i + 1] += rightDeltas[i];
        }
    }

    private void ApplyWaveNoise()
    {
        for (int i = 1; i < topPointIndices.Count - 1; i++)
        {
            int index = topPointIndices[i];
            float phase = i * wavePhaseOffset;
            displacedPositions[index].y += Mathf.Sin(time * waveFrequency + phase) * waveAmplitude;
        }
    }

    private void UpdateTopSurface()
    {
        for (int i = 1; i < topPointIndices.Count - 1; i++)
        {
            int index = topPointIndices[i];
            Vector3 newPos = displacedPositions[index];
            newPos.x = originalPositions[index].x;
            newPos.z = originalPositions[index].z;
            shapeController.spline.SetPosition(index, newPos);
        }

        if (edgeCollider != null)
        {
            Vector2[] colliderPoints = new Vector2[topPointIndices.Count];
            for (int i = 0; i < topPointIndices.Count; i++)
            {
                int index = topPointIndices[i];
                colliderPoints[i] = new Vector2(displacedPositions[index].x, displacedPositions[index].y);
            }
            edgeCollider.points = colliderPoints;
        }

        shapeController.BakeMesh();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!interactingObjects.Contains(collision.transform))
            interactingObjects.Add(collision.transform);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (interactingObjects.Contains(collision.transform))
            interactingObjects.Remove(collision.transform);
    }
}
