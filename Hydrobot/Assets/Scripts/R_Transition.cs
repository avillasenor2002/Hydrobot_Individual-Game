using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class R_Transition : MonoBehaviour
{
    [Header("Tagged Objects")]
    public string targetTag = "Movable";
    public float moveDuration = 1.5f;
    public AnimationCurve easingCurve;

    [Header("Center Object")]
    public Transform objectToCenter;
    public float centerMoveDuration = 1.5f;
    public float overshootDistance = 50f;
    public AnimationCurve centerEasingCurve;

    [Header("Score Fill")]
    [Tooltip("Assign the R_ScoreFill component to auto‑trigger after the transition.")]
    

    // ---- Completion Event ----
    public System.Action OnMoveComplete;

    private bool isMoving = false;

    void Start()
    {
        // Subscribe the score fill trigger to the completion event

    }

    public void TriggerMove()
    {
        if (isMoving) return;
        StartCoroutine(MoveSequence());
    }

    private IEnumerator MoveSequence()
    {
        isMoving = true;

        // ---- 1. Prepare tagged objects ----
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(targetTag);
        List<Transform> taggedTransforms = new List<Transform>();
        List<Vector3> startPositions = new List<Vector3>();
        foreach (GameObject go in taggedObjects)
        {
            taggedTransforms.Add(go.transform);
            startPositions.Add(go.transform.position);
        }
        Vector3[] targetPositions = new Vector3[startPositions.Count];
        for (int i = 0; i < startPositions.Count; i++)
        {
            targetPositions[i] = startPositions[i] + Vector3.left * 1000f;
        }

        // ---- 2. Prepare center object ----
        bool hasCenter = objectToCenter != null;
        Vector3 centerStartPos = hasCenter ? objectToCenter.position : Vector3.zero;
        Vector3 centerTargetPos = hasCenter ? new Vector3(0f, centerStartPos.y, centerStartPos.z) : Vector3.zero;
        float centerDistance = hasCenter ? Mathf.Abs(centerTargetPos.x - centerStartPos.x) : 0f;
        float direction = hasCenter ? Mathf.Sign(centerTargetPos.x - centerStartPos.x) : 1f;
        Vector3 overshootPos = hasCenter ? new Vector3(centerTargetPos.x + direction * overshootDistance, centerStartPos.y, centerStartPos.z) : Vector3.zero;

        // ---- 3. Run both animations concurrently ----
        float taggedElapsed = 0f;
        float centerElapsed = 0f;

        float totalDistance = centerDistance + 2f * overshootDistance;
        float centerSpeed = (totalDistance > 0.001f) ? totalDistance / centerMoveDuration : 0f;
        float phase1Distance = centerDistance + overshootDistance;
        float phase2Distance = overshootDistance;
        float phase1Duration = (centerSpeed > 0.001f) ? phase1Distance / centerSpeed : 0f;

        while (taggedElapsed < moveDuration || centerElapsed < centerMoveDuration)
        {
            // ---- Tagged objects ----
            if (taggedElapsed < moveDuration)
            {
                taggedElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(taggedElapsed / moveDuration);
                float easedT = (easingCurve != null) ? easingCurve.Evaluate(t) : t;
                for (int i = 0; i < taggedTransforms.Count; i++)
                {
                    if (taggedTransforms[i] != null)
                        taggedTransforms[i].position = Vector3.Lerp(startPositions[i], targetPositions[i], easedT);
                }
            }
            else
            {
                for (int i = 0; i < taggedTransforms.Count; i++)
                {
                    if (taggedTransforms[i] != null)
                        taggedTransforms[i].position = targetPositions[i];
                }
            }

            // ---- Center object (with easing) ----
            if (hasCenter && objectToCenter != null)
            {
                if (centerElapsed < centerMoveDuration)
                {
                    centerElapsed += Time.deltaTime;
                    float centerProgress = Mathf.Clamp01(centerElapsed / centerMoveDuration);

                    float phase1Progress = phase1Duration / centerMoveDuration;
                    if (centerProgress <= phase1Progress)
                    {
                        float rawP = centerProgress / phase1Progress;
                        float easedP = (centerEasingCurve != null) ? centerEasingCurve.Evaluate(rawP) : rawP;
                        objectToCenter.position = Vector3.Lerp(centerStartPos, overshootPos, easedP);
                    }
                    else
                    {
                        float rawP = (centerProgress - phase1Progress) / (1f - phase1Progress);
                        float easedP = (centerEasingCurve != null) ? centerEasingCurve.Evaluate(rawP) : rawP;
                        objectToCenter.position = Vector3.Lerp(overshootPos, centerTargetPos, easedP);
                    }
                }
                else
                {
                    objectToCenter.position = centerTargetPos;
                }
            }

            yield return null;
        }

        // ---- Final snap ----
        for (int i = 0; i < taggedTransforms.Count; i++)
        {
            if (taggedTransforms[i] != null)
                taggedTransforms[i].position = targetPositions[i];
        }
        if (hasCenter && objectToCenter != null)
            objectToCenter.position = centerTargetPos;

        isMoving = false;

        // ---- Fire completion event ----
        OnMoveComplete?.Invoke();
    }

    /// <summary>
    /// Unsubscribe from the event when the object is destroyed (optional clean‑up).
    /// </summary>
    private void OnDestroy()
    {

    }
}