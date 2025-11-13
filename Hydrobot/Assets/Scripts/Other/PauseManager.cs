using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.EventSystems;


public class PauseManager : MonoBehaviour
{
    [SerializeField] private AudioSource musicSource; // Background music AudioSource
    [SerializeField] private float muffledPitch = 0.5f; // Lower pitch when paused
    [SerializeField] private float normalPitch = 1f; // Normal playback pitch
    [SerializeField] private Image pauseImage; // UI Image for pause overlay
    [SerializeField] private float fadeDuration = 0.1f; // UI fade speed
    [SerializeField] private float pitchLerpSpeed = 2f; // Speed of pitch transition
    [SerializeField] private float maxOpacity = 0.7f; // Maximum UI opacity

    [SerializeField] private Blur blurScript; // Reference to Blur script
    [SerializeField] private float blurEaseDuration = 0.3f; // Time to ease in/out blur
    [SerializeField] private GameObject pauseUIElement; // UI element to activate
    [SerializeField] private string mainMenuSceneName = "MainMenu"; // Scene to load when exiting
    [SerializeField] private GameObject firstSelectedOnPause;


    private bool isPaused = false;
    private Coroutine pitchCoroutine;
    private Coroutine blurCoroutine;

    public void OnPause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0 : 1;

        // Adjust pitch effect
        if (pitchCoroutine != null)
            StopCoroutine(pitchCoroutine);
        pitchCoroutine = StartCoroutine(AdjustPitch(isPaused ? muffledPitch : normalPitch));

        // Fade UI
        StartCoroutine(FadeUI(isPaused ? maxOpacity : 0));

        // Handle blur effect
        if (blurCoroutine != null)
            StopCoroutine(blurCoroutine);
        blurCoroutine = StartCoroutine(AdjustBlur(isPaused));

        // Toggle UI Element
        if (pauseUIElement)
            pauseUIElement.SetActive(isPaused);

        // Pause non-music sounds
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
        foreach (AudioSource source in allAudioSources)
        {
            if (source != musicSource)
            {
                if (isPaused) source.Pause();
                else source.UnPause();
            }
        }

        if (isPaused && firstSelectedOnPause != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(firstSelectedOnPause);
        }
    }

    public void ContinueGame()
    {
        if (isPaused)
        {
            isPaused = false;
            Time.timeScale = 1;

            // Resume pitch effect
            if (pitchCoroutine != null)
                StopCoroutine(pitchCoroutine);
            pitchCoroutine = StartCoroutine(AdjustPitch(normalPitch));

            // Fade UI out
            StartCoroutine(FadeUI(0));

            // Disable blur effect
            if (blurCoroutine != null)
                StopCoroutine(blurCoroutine);
            blurCoroutine = StartCoroutine(AdjustBlur(false));

            // Hide Pause UI
            if (pauseUIElement)
                pauseUIElement.SetActive(false);

            // Resume non-music sounds
            AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
            foreach (AudioSource source in allAudioSources)
            {
                if (source != musicSource)
                    source.UnPause();
            }
        }

        EventSystem.current.SetSelectedGameObject(null);
    }

    public void RestartGame()
    {
        Time.timeScale = 1; // Reset time before reloading
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ExitToMainMenu()
    {
        Time.timeScale = 1; // Reset time before switching scenes
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private IEnumerator AdjustPitch(float targetPitch)
    {
        float startPitch = musicSource.pitch;
        float elapsed = 0f;

        while (elapsed < 1f)
        {
            elapsed += Time.unscaledDeltaTime * pitchLerpSpeed;
            musicSource.pitch = Mathf.Lerp(startPitch, targetPitch, elapsed);
            yield return null;
        }
        musicSource.pitch = targetPitch;
    }

    private IEnumerator FadeUI(float targetAlpha)
    {
        float startAlpha = pauseImage.color.a;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            alpha = Mathf.Clamp(alpha, 0f, maxOpacity);
            pauseImage.color = new Color(pauseImage.color.r, pauseImage.color.g, pauseImage.color.b, alpha);
            yield return null;
        }

        pauseImage.color = new Color(pauseImage.color.r, pauseImage.color.g, pauseImage.color.b, targetAlpha);
    }

    private IEnumerator AdjustBlur(bool enable)
    {
        if (blurScript == null) yield break;

        float startBlur = enable ? 0f : blurScript.radius;
        float targetBlur = enable ? blurScript.defaultRadius : 0f;
        float elapsed = 0f;

        if (enable) blurScript.enabled = true; // Enable script when pausing

        while (elapsed < blurEaseDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            blurScript.radius = Mathf.Lerp(startBlur, targetBlur, elapsed / blurEaseDuration);
            yield return null;
        }

        blurScript.radius = targetBlur;

        if (!enable) blurScript.enabled = false; // Disable script when unpausing
    }
}
