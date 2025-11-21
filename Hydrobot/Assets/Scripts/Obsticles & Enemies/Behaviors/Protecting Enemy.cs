using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ProtectorEnemy : MonoBehaviour
{
    [Header("Linked Enemies")]
    public List<Enemy> protectedEnemies = new List<Enemy>();

    [Header("Protection Settings")]
    public Color protectedTintColor = Color.cyan;

    [Header("Electric Line Settings")]
    public float baseLineWidth = 0.05f;
    public float lineWidthVariation = 0.1f;
    public Color lineColor = new Color(0f, 1f, 1f, 0.5f);
    public float noiseAmplitude = 0.1f;
    public float noiseFrequency = 20f;
    public int lineSegments = 10;

    // Internal
    private Dictionary<SpriteRenderer, Color> originalColors = new Dictionary<SpriteRenderer, Color>();
    private List<LineRenderer> lineRenderers = new List<LineRenderer>();

    private void Start()
    {
        foreach (var enemy in protectedEnemies)
        {
            if (enemy == null) continue;

            // Initialize protection count dictionary if needed
            if (!EnemyProtection.protectionCounts.ContainsKey(enemy))
                EnemyProtection.protectionCounts[enemy] = 0;

            EnemyProtection.protectionCounts[enemy]++;
            enemy.isProtected = true;

            StartCoroutine(KeepAtMaxHealth(enemy));

            SpriteRenderer[] rends = enemy.GetComponentsInChildren<SpriteRenderer>();
            foreach (var r in rends)
            {
                if (!originalColors.ContainsKey(r))
                    originalColors[r] = r.material.GetColor("_Color");
            }

            // Create a LineRenderer for this enemy
            GameObject lineObj = new GameObject("ElectricLine");
            lineObj.transform.parent = transform;
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.positionCount = lineSegments + 1;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.useWorldSpace = true;
            lr.startColor = lr.endColor = lineColor;
            lr.widthCurve = new AnimationCurve(); // will update in Update()
            lineRenderers.Add(lr);
        }
    }

    private System.Collections.IEnumerator KeepAtMaxHealth(Enemy enemy)
    {
        while (enemy != null && !enemy.isDead)
        {
            enemy.health = Mathf.Max(enemy.health, enemy.health);
            yield return null;
        }
    }

    private void Update()
    {
        // Tint enemies every frame
        foreach (var enemy in protectedEnemies)
        {
            if (enemy == null) continue;

            SpriteRenderer[] rends = enemy.GetComponentsInChildren<SpriteRenderer>();
            foreach (var r in rends)
            {
                if (r != null)
                    r.material.SetColor("_Color", protectedTintColor);
            }
        }

        // Update electric lines
        for (int i = 0; i < protectedEnemies.Count; i++)
        {
            Enemy enemy = protectedEnemies[i];
            LineRenderer lr = lineRenderers[i];

            if (enemy == null || lr == null) continue;

            Vector3 start = transform.position;
            Vector3 end = enemy.transform.position;

            for (int j = 0; j <= lineSegments; j++)
            {
                float t = (float)j / lineSegments;
                Vector3 pos = Vector3.Lerp(start, end, t);

                pos.x += (Mathf.PerlinNoise(Time.time * noiseFrequency, j) - 0.5f) * noiseAmplitude;
                pos.y += (Mathf.PerlinNoise(Time.time * noiseFrequency + 100, j) - 0.5f) * noiseAmplitude;

                lr.SetPosition(j, pos);
            }

            AnimationCurve curve = new AnimationCurve();
            for (int j = 0; j <= lineSegments; j++)
            {
                float t = (float)j / lineSegments;
                float width = baseLineWidth + (Mathf.PerlinNoise(Time.time * 10f + j, j * 0.1f) - 0.5f) * lineWidthVariation;
                curve.AddKey(t, Mathf.Max(0f, width));
            }
            lr.widthCurve = curve;
        }
    }

    private void OnDestroy()
    {
        // Remove protection counts and update isProtected
        foreach (var enemy in protectedEnemies)
        {
            if (enemy != null && EnemyProtection.protectionCounts.ContainsKey(enemy))
            {
                EnemyProtection.protectionCounts[enemy]--;

                if (EnemyProtection.protectionCounts[enemy] <= 0)
                {
                    enemy.isProtected = false;
                    EnemyProtection.protectionCounts.Remove(enemy);

                    // Restore sprite colors to white when fully unprotected
                    SpriteRenderer[] rends = enemy.GetComponentsInChildren<SpriteRenderer>();
                    foreach (var r in rends)
                    {
                        if (r != null)
                            r.material.SetColor("_Color", Color.white);
                    }
                }
            }
        }

        // Clean up line renderers
        foreach (var lr in lineRenderers)
        {
            if (lr != null) Destroy(lr.gameObject);
        }
    }
}

public static class EnemyProtection
{
    public static Dictionary<Enemy, int> protectionCounts = new Dictionary<Enemy, int>();
}
