using UnityEngine;
using System.Collections;

public class TrainMotion2D : MonoBehaviour
{
    [Header("Positions")]
    public float startX = -12f;
    public float stopX = 0f;
    public float exitX = 12f;
    public float returnX = -14f;

    [Header("Timing")]
    public float approachTime = 2.5f;
    public float stopDuration = 1.0f;
    public float exitTime = 2.0f;
    public float returnDelay = 1.5f;
    public float startDelay = 0f;

    [Header("Audio")]
    public AudioSource audioSource;

    [Header("Approach Sound")]
    public bool playApproachSound = true;
    public AudioClip approachClip;

    [Header("Arrival Impact Sound")]
    public bool playStopImpactSound = true;
    public AudioClip stopImpactClip;

    [Header("Stopped Loop Sound")]
    public bool playStoppedLoop = true;
    public AudioClip stoppedLoopClip;

    [Header("Exit Sound")]
    public bool playExitSound = true;
    public AudioClip exitClip;

    private void Start()
    {
        StartCoroutine(TrainLoop());
    }

    IEnumerator TrainLoop()
    {
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        while (true)
        {
            SetX(startX);

            // -------- APPROACH --------
            if (playApproachSound && audioSource && approachClip)
            {
                audioSource.clip = approachClip;
                audioSource.loop = true;
                audioSource.Play();
            }

            yield return MoveX(startX, stopX, approachTime, EaseOut);

            // Stop approach sound
            if (audioSource && audioSource.isPlaying)
                audioSource.Stop();

            // Impact sound
            if (playStopImpactSound && audioSource && stopImpactClip)
                audioSource.PlayOneShot(stopImpactClip);

            // -------- STOPPED --------
            if (playStoppedLoop && audioSource && stoppedLoopClip)
            {
                audioSource.clip = stoppedLoopClip;
                audioSource.loop = true;
                audioSource.Play();
            }

            yield return new WaitForSeconds(stopDuration);

            if (audioSource && audioSource.isPlaying)
                audioSource.Stop();

            // -------- EXIT --------
            if (playExitSound && audioSource && exitClip)
            {
                audioSource.clip = exitClip;
                audioSource.loop = true;
                audioSource.Play();
            }

            yield return MoveX(stopX, exitX, exitTime, EaseIn);

            if (audioSource && audioSource.isPlaying)
                audioSource.Stop();

            // Reset
            SetX(returnX);

            yield return new WaitForSeconds(returnDelay);
        }
    }

    IEnumerator MoveX(float from, float to, float duration, System.Func<float, float> easing)
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / duration);
            float e = easing(n);

            SetX(Mathf.Lerp(from, to, e));
            yield return null;
        }
    }

    void SetX(float x)
    {
        transform.position = new Vector3(x, transform.position.y, transform.position.z);
    }

    // -------- EASING --------

    float EaseOut(float t) => 1f - Mathf.Pow(1f - t, 3f);
    float EaseIn(float t) => t * t * t;
}
