using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class FreedReadyBot : MonoBehaviour
{
    [Header("Scale Pulse")]
    public float pulseScale = 1.2f;         // Scale size at peak of pulse
    public float pulseDuration = 0.2f;      // Time to reach pulse peak
    public float returnDuration = 0.2f;     // Time to shrink back to original size

    [Header("Movement")]
    public float upwardForce = 2f;          // Initial upward push
    public float gravity = -4f;             // Simulated gravity pull

    [Header("Fade Out")]
    public float fadeDuration = 1f;         // How long it takes to fade away

    private SpriteRenderer sr;
    private Vector3 originalScale;
    private float verticalVelocity = 0f;
    private float fadeTimer = 0f;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;
        StartCoroutine(PulseRoutine());

        // Initial upward movement
        verticalVelocity = upwardForce;
    }

    void Update()
    {
        // Movement (fake gravity)
        verticalVelocity += gravity * Time.deltaTime;
        transform.position += new Vector3(0f, verticalVelocity * Time.deltaTime, 0f);

        // Fade out
        fadeTimer += Time.deltaTime;
        float fadeAmount = Mathf.Clamp01(1f - (fadeTimer / fadeDuration));

        Color c = sr.color;
        c.a = fadeAmount;
        sr.color = c;

        // Delete once invisible
        if (c.a <= 0f)
        {
            Destroy(gameObject);
        }
    }

    IEnumerator PulseRoutine()
    {
        // Pulse up
        float t = 0f;
        while (t < pulseDuration)
        {
            float lerp = t / pulseDuration;
            transform.localScale = Vector3.Lerp(originalScale, originalScale * pulseScale, lerp);
            t += Time.deltaTime;
            yield return null;
        }

        // Pulse back down
        t = 0f;
        while (t < returnDuration)
        {
            float lerp = t / returnDuration;
            transform.localScale = Vector3.Lerp(originalScale * pulseScale, originalScale, lerp);
            t += Time.deltaTime;
            yield return null;
        }

        transform.localScale = originalScale;
    }
}
