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

    // Cached buttons under the pause UI element
    private Button[] pauseButtons;

    private void Start()
    {
        // Cache all buttons under the pause UI element (including inactive ones)
        CachePauseButtons();

        // Make sure buttons start disabled since the pause UI begins at 0 opacity
        SetPauseButtonsInteractable(false);

        // Select the first pause button at the start of the scene
        if (firstSelectedOnPause != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(firstSelectedOnPause);
        }
    }

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
        // Get all Graphic components on the pause BG object and its children
        Graphic[] graphics = pauseImage.GetComponentsInChildren<Graphic>(true);
        float[] startAlphas = new float[graphics.Length];

        // Record the starting alpha of each graphic
        for (int i = 0; i < graphics.Length; i++)
        {
            startAlphas[i] = graphics[i].color.a;
        }

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / fadeDuration;

            for (int i = 0; i < graphics.Length; i++)
            {
                float alpha = Mathf.Lerp(startAlphas[i], targetAlpha, t);
                alpha = Mathf.Clamp(alpha, 0f, maxOpacity);
                Color c = graphics[i].color;
                c.a = alpha;
                graphics[i].color = c;
            }

            yield return null;
        }

        // Ensure final alpha is exactly the target
        for (int i = 0; i < graphics.Length; i++)
        {
            Color c = graphics[i].color;
            c.a = targetAlpha;
            graphics[i].color = c;
        }

        // Buttons are only interactable while the pause UI is visible (opacity > 0)
        SetPauseButtonsInteractable(targetAlpha > 0f);
    }

    private void CachePauseButtons()
    {
        if (pauseUIElement == null)
        {
            pauseButtons = new Button[0];
            return;
        }

        // Include inactive children so this works even if the UI element starts disabled
        pauseButtons = pauseUIElement.GetComponentsInChildren<Button>(true);
    }

    private void SetPauseButtonsInteractable(bool value)
    {
        if (pauseButtons == null)
            CachePauseButtons();

        if (pauseButtons == null) return;

        foreach (Button btn in pauseButtons)
        {
            if (btn != null)
                btn.interactable = value;
        }
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