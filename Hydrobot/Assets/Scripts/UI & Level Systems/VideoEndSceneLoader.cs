using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

/// <summary>
/// Watches a VideoPlayer and loads a scene when the video finishes.
/// Uses the loopPointReached event, which fires when playback reaches the
/// end of the clip.
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
    }

    private void Start()
    {
        if (playOnStart && videoPlayer != null)
            videoPlayer.Play();
    }

    // ------------------------------------------------------------------
    // VIDEO END
    // ------------------------------------------------------------------

    private void HandleVideoFinished(VideoPlayer vp)
    {
        if (hasLoaded) return;

        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogWarning("[VideoEndSceneLoader] No scene name set — video ended but no scene will load.", this);
            return;
        }

        hasLoaded = true;

        if (logResult)
            Debug.Log($"[VideoEndSceneLoader] Video finished — loading '{sceneToLoad}'.", this);

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
        if (hasLoaded) return;
        hasLoaded = true;
        LoadScene();
    }
}