using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UI_CutoutTransition : MonoBehaviour
{
    [Header("Cutout Target")]
    [Tooltip("The RectTransform used as the cutout mask.")]
    public RectTransform cutoutMask;

    [Header("Timing")]
    [Tooltip("How long the cutout takes to grow from zero to target size.")]
    public float growDuration = 1.0f;

    [Tooltip("Degrees per second of rotation during the transition.")]
    public float rotationSpeed = 360f;

    [Tooltip("Optional curve to ease the growth. Falls back to linear if unset.")]
    public AnimationCurve growCurve;

    [Tooltip("Delay before the transition starts, in seconds (unscaled).")]
    public float startDelay = 0f;

    [Header("Scale")]
    [Tooltip("The local scale the cutout grows into. Set in the Inspector.")]
    public Vector3 targetScale = Vector3.one;

    [Header("Scene Loading")]
    [Tooltip("Scene to load when PlayReverseAndLoadScene is called.")]
    public string targetSceneName = "";

    [Header("Events")]
    public UnityEngine.Events.UnityEvent onTransitionComplete;
    public UnityEngine.Events.UnityEvent onReverseComplete;

    private Coroutine activeCoroutine;

    private void Start()
    {
        if (cutoutMask == null)
        {
            Debug.LogWarning("UI_CutoutTransition: No cutoutMask assigned.", this);
            onTransitionComplete?.Invoke();
            return;
        }

        activeCoroutine = StartCoroutine(GrowCutout());
    }

    // ---- Wire this to a UI Button's OnClick ----
    // Reverses the transition, then reloads the current scene.
    public void PlayReverseAndRestart()
    {
        if (cutoutMask == null)
        {
            Debug.LogWarning("UI_CutoutTransition: No cutoutMask assigned.", this);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }

        activeCoroutine = StartCoroutine(ShrinkCutoutAndLoad(() =>
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex)));
    }

    // ---- Wire this to a UI Button's OnClick ----
    // Reverses the transition, then loads the scene set in the Inspector.
    public void PlayReverseAndLoadScene()
    {
        if (cutoutMask == null)
        {
            Debug.LogWarning("UI_CutoutTransition: No cutoutMask assigned.", this);
            LoadTargetScene();
            return;
        }

        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }

        activeCoroutine = StartCoroutine(ShrinkCutoutAndLoad(LoadTargetScene));
    }

    private void LoadTargetScene()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("UI_CutoutTransition: targetSceneName is empty. Loading current scene instead.", this);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        SceneManager.LoadScene(targetSceneName);
    }

    private IEnumerator GrowCutout()
    {
        // Start fully collapsed
        cutoutMask.localScale = Vector3.zero;

        if (startDelay > 0f)
            yield return new WaitForSecondsRealtime(startDelay);

        float elapsed = 0f;
        while (elapsed < growDuration)
        {
            // Unscaled so the transition still plays while the game is paused
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / growDuration);
            float easedT = (growCurve != null) ? growCurve.Evaluate(t) : t;

            cutoutMask.localScale = Vector3.Lerp(Vector3.zero, targetScale, easedT);

            float angle = rotationSpeed * Time.unscaledDeltaTime;
            cutoutMask.Rotate(0f, 0f, angle, Space.Self);

            yield return null;
        }

        cutoutMask.localScale = targetScale;
        onTransitionComplete?.Invoke();
        activeCoroutine = null;
    }

    // Shared shrink routine. The supplied callback is invoked after the
    // reverse animation finishes so the caller can load whatever scene it wants.
    private IEnumerator ShrinkCutoutAndLoad(System.Action onComplete)
    {
        Vector3 startScale = cutoutMask.localScale;
        float elapsed = 0f;

        while (elapsed < growDuration)
        {
            // Unscaled so it still works while time is paused
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / growDuration);

            // Reverse the curve so the shrink mirrors the grow:
            // at t=0 the eased value is 1 (fully open), at t=1 it is 0 (closed).
            float easedT = (growCurve != null) ? growCurve.Evaluate(1f - t) : (1f - t);

            // Lerp FROM zero TO startScale using the reversed ease,
            // so it starts at startScale and ends at zero.
            cutoutMask.localScale = Vector3.Lerp(Vector3.zero, startScale, easedT);

            float angle = rotationSpeed * Time.unscaledDeltaTime;
            cutoutMask.Rotate(0f, 0f, angle, Space.Self);

            yield return null;
        }

        cutoutMask.localScale = Vector3.zero;
        onReverseComplete?.Invoke();
        activeCoroutine = null;

        onComplete?.Invoke();
    }
}