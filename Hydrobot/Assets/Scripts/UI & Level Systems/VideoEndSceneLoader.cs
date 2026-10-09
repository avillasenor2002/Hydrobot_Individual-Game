using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

/// <summary>
/// Watches a VideoPlayer and loads a scene when the video finishes.
/// Also has a hard fail-safe timer that forces the load after a set
/// number of seconds, in case the video never fires its end event.
/// </summary>
[RequireComponent(typeof(VideoPlayer))]
public class VideoEndSceneLoader : MonoBehaviour
{
    [Header("Video")]
    [Tooltip("VideoPlayer to watch. Leave empty to use the one on this GameObject.")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Scene")]
    [Tooltip("Name of the scene to load when the video finishes. Must be in Build Settings.")]
    [SerializeField] private string sceneToLoad;

    [Header("Timing")]
    [Tooltip("Seconds to wait after the video ends before loading. 0 loads immediately.")]
    [SerializeField] private float delayBeforeLoad = 0f;

    [Tooltip("Maximum seconds to wait before forcing the load, even if the video hasn't ended. " +
             "Set to 0 to disable the fail-safe.")]
    [SerializeField] private float maxWaitTime = 50f;

    [Header("Options")]
    [Tooltip("If true, the video player's loop setting is forced off so the end event fires.")]
    [SerializeField] private bool forceLoopOff = true;

    [Tooltip("If true, the player is started automatically on Start.")]
    [SerializeField] private bool playOnStart = true;

    [Tooltip("Log when the video ends and the scene load begins.")]
    [SerializeField] private bool logResult = false;

    // ------------------------------------------------------------------
    // Runtime state
    // ------------------------------------------------------------------
    private bool hasLoaded;
    private Coroutine failSafeRoutine;

    private void Awake()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        if (videoPlayer == null)
        {
            Debug.LogError("[VideoEndSceneLoader] No VideoPlayer assigned or found on this GameObject.", this);
            return;
        }

        if (forceLoopOff)
            videoPlayer.isLooping = false;
    }

    private void OnEnable()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached += HandleVideoFinished;
    }

    private void OnDisable()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= HandleVideoFinished;

        if (failSafeRoutine != null)
        {
            StopCoroutine(failSafeRoutine);
            failSafeRoutine = null;
        }
    }

    private void Start()
    {
        if (playOnStart && videoPlayer != null)
            videoPlayer.Play();

        // Start the fail-safe timer.
        if (maxWaitTime > 0f)
            failSafeRoutine = StartCoroutine(FailSafeTimer());
    }

    // ------------------------------------------------------------------
    // FAIL-SAFE
    // ------------------------------------------------------------------

    private IEnumerator FailSafeTimer()
    {
        yield return new WaitForSecondsRealtime(maxWaitTime);

        if (hasLoaded)
            yield break;

        if (logResult)
            Debug.Log($"[VideoEndSceneLoader] Fail-safe fired after {maxWaitTime}s — forcing scene load.", this);

        TriggerLoad();
    }

    // ------------------------------------------------------------------
    // VIDEO END
    // ------------------------------------------------------------------

    private void HandleVideoFinished(VideoPlayer vp)
    {
        if (hasLoaded) return;

        if (logResult)
            Debug.Log("[VideoEndSceneLoader] Video finished normally.", this);

        TriggerLoad();
    }

    // ------------------------------------------------------------------
    // LOAD
    // ------------------------------------------------------------------

    private void TriggerLoad()
    {
        if (hasLoaded) return;

        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogWarning("[VideoEndSceneLoader] No scene name set — nothing will load.", this);
            return;
        }

        hasLoaded = true;

        if (failSafeRoutine != null)
        {
            StopCoroutine(failSafeRoutine);
            failSafeRoutine = null;
        }

        if (logResult)
            Debug.Log($"[VideoEndSceneLoader] Loading '{sceneToLoad}'.", this);

        if (delayBeforeLoad > 0f)
            Invoke(nameof(LoadScene), delayBeforeLoad);
        else
            LoadScene();
    }

    private void LoadScene()
    {
        SceneManager.LoadScene(sceneToLoad);
    }

    // ------------------------------------------------------------------
    // PUBLIC API
    // ------------------------------------------------------------------

    /// <summary>Manually fires the load. Useful for skip buttons.</summary>
    public void SkipToScene()
    {
        TriggerLoad();
    }
}