using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class LevelSelectTransition : MonoBehaviour
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
    [Tooltip("Fallback scene used when a load function is called with an empty name.")]
    public string targetSceneName = "";

    [Header("UI Selection")]
    [Tooltip("The UI element that gets selected once the intro transition finishes.")]
    public GameObject defaultSelectedUI;

    [Tooltip("If true, defaultSelectedUI is selected automatically after the grow transition completes.")]
    public bool selectDefaultUIOnStart = true;

    [Tooltip("Extra delay (unscaled seconds) before the default UI element is selected.")]
    public float selectDelay = 0f;

    [Header("Events")]
    public UnityEvent onTransitionComplete;
    public UnityEvent onReverseComplete;

    private Coroutine activeCoroutine;

    private void Start()
    {
        if (cutoutMask == null)
        {
            Debug.LogWarning("UI_CutoutTransitionWithSceneLoad: No cutoutMask assigned.", this);
            onTransitionComplete?.Invoke();

            if (selectDefaultUIOnStart)
                SelectDefaultUI();

            return;
        }

        activeCoroutine = StartCoroutine(GrowCutout());
    }

    // ------------------------------------------------------------------
    // UI SELECTION
    // ------------------------------------------------------------------

    /// <summary>
    /// Selects the element assigned in the "Default Selected UI" slot.
    /// Called automatically after the intro transition if selectDefaultUIOnStart is true.
    /// Can also be called manually (e.g. from a button or an event).
    /// </summary>
    public void SelectDefaultUI()
    {
        if (defaultSelectedUI == null)
        {
            Debug.LogWarning("UI_CutoutTransitionWithSceneLoad: No defaultSelectedUI assigned.", this);
            return;
        }

        StartCoroutine(SelectUIRoutine(defaultSelectedUI, selectDelay));
    }

    /// <summary>
    /// Selects any UI element you pass in. Useful for wiring to buttons/events.
    /// </summary>
    public void SelectUI(GameObject uiElement)
    {
        if (uiElement == null)
        {
            Debug.LogWarning("UI_CutoutTransitionWithSceneLoad: SelectUI was called with a null element.", this);
            return;
        }

        StartCoroutine(SelectUIRoutine(uiElement, selectDelay));
    }

    private IEnumerator SelectUIRoutine(GameObject uiElement, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        if (EventSystem.current == null)
        {
            Debug.LogWarning("UI_CutoutTransitionWithSceneLoad: No EventSystem found in the scene, cannot select UI.", this);
            yield break;
        }

        // Clear first so the selection registers even if it was already selected.
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(uiElement);
    }

    // ------------------------------------------------------------------
    // BUTTON HOOKS
    // ------------------------------------------------------------------

    // ---- Wire this to a UI Button's OnClick ----
    // Reverses the transition, then reloads the current scene.
    public void PlayReverseAndRestart()
    {
        if (cutoutMask == null)
        {
            Debug.LogWarning("UI_CutoutTransitionWithSceneLoad: No cutoutMask assigned.", this);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        StopActiveCoroutine();

        activeCoroutine = StartCoroutine(ShrinkCutoutAndLoad(() =>
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex)));
    }

    // ---- Wire this to a UI Button's OnClick ----
    // Reverses the transition, then loads the scene named in the Inspector.
    public void PlayReverseAndLoadScene()
    {
        PlayReverseAndLoadScene(targetSceneName);
    }

    // ---- Wire this to a UI Button's OnClick ----
    // Reverses the transition, then loads the scene whose name the button passes in.
    // In the Button's OnClick list, pick this function and type the scene name
    // into the string field that appears.
    public void PlayReverseAndLoadScene(string sceneName)
    {
        if (cutoutMask == null)
        {
            Debug.LogWarning("UI_CutoutTransitionWithSceneLoad: No cutoutMask assigned.", this);
            LoadSceneByName(sceneName);
            return;
        }

        StopActiveCoroutine();

        activeCoroutine = StartCoroutine(ShrinkCutoutAndLoad(() => LoadSceneByName(sceneName)));
    }

    // ------------------------------------------------------------------
    // INTERNALS
    // ------------------------------------------------------------------

    private void StopActiveCoroutine()
    {
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }
    }

    private void LoadSceneByName(string sceneName)
    {
        // Fall back to the Inspector value if the button didn't supply a name.
        if (string.IsNullOrEmpty(sceneName))
        {
            if (!string.IsNullOrEmpty(targetSceneName))
            {
                sceneName = targetSceneName;
            }
            else
            {
                Debug.LogWarning("UI_CutoutTransitionWithSceneLoad: No scene name supplied and targetSceneName is empty. " +
                                 "Reloading the current scene instead.", this);
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                return;
            }
        }

        SceneManager.LoadScene(sceneName);
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

        // Select the starting UI element now that the intro transition is done.
        if (selectDefaultUIOnStart)
            SelectDefaultUI();
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