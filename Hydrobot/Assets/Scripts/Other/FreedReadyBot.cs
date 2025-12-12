using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class FreedReadyBot : MonoBehaviour
{
    [Header("Scale Pulse")]
    public float pulseScale = 1.2f;
    public float pulseDuration = 0.2f;
    public float returnDuration = 0.2f;

    [Header("Movement")]
    public float upwardForce = 2f;
    public float gravity = -4f;

    [Header("Fade Out")]
    public float fadeDuration = 1f;

    [Header("Audio")]
    public AudioClip spawnSFX;        // Sound played when spawned

    private AudioSource localSource;  // AudioSource attached dynamically
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

        // Create dedicated AudioSource
        CreateLocalAudioSource();

        // Play spawn SFX
        if (localSource != null && spawnSFX != null)
        {
            localSource.PlayOneShot(spawnSFX, 1f);   // Full volume
        }
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

    void CreateLocalAudioSource()
    {
        localSource = gameObject.AddComponent<AudioSource>();
        localSource.playOnAwake = false;
        localSource.spatialBlend = 0f;   // 2D sound
        localSource.volume = 1f;
        localSource.pitch = 1f;
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
