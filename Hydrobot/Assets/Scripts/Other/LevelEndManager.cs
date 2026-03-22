using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class LevelEndManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject endUIRoot;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField, Range(0f, 1f)] private float maxOpacity = 0.8f;

    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip endMusic;
    [SerializeField] private AudioClip loopingMusic;

    [Header("Slow Motion Settings")]
    [SerializeField] private float slowTimeScale = 0.3f;
    [SerializeField] private float slowDuration = 2f;

    [Header("Game Over")]
    [SerializeField] private GameObject gameOverUIRoot;
    [SerializeField] private AudioClip gameOverMusic;

    [Header("UI Navigation")]
    [SerializeField] private GameObject gameOverFirstSelected;
    [SerializeField] private GameObject levelEndFirstSelected;

    [Header("Level Settings")]
    [SerializeField] private int currentLevelIndex = 0;

    private Image rootImage;
    private Image[] childImages;
    private Text[] childTexts;

    // Track which object should stay selected so we can re-assert it if focus is lost
    private GameObject targetSelected = null;

    private void Start()
    {
        if (endUIRoot != null)
        {
            endUIRoot.SetActive(false);

            rootImage = endUIRoot.GetComponent<Image>();
            childImages = endUIRoot.GetComponentsInChildren<Image>(true);
            childTexts = endUIRoot.GetComponentsInChildren<Text>(true);

            childImages = System.Array.FindAll(childImages, img => img != rootImage);
        }
    }

    private void Update()
    {
        // If a UI screen is up and focus has drifted (e.g. mouse click on empty space,
        // gamepad disconnected briefly), re-assert the intended selection every frame.
        if (targetSelected != null && EventSystem.current != null)
        {
            if (EventSystem.current.currentSelectedGameObject != targetSelected)
                EventSystem.current.SetSelectedGameObject(targetSelected);
        }
    }

    public void TriggerLevelEnd()
    {
        MarkLevelComplete();
        StartCoroutine(HandleLevelEndSequence());
    }

    public void TriggerGameOver()
    {
        StartCoroutine(HandleGameOverSequence());
    }

    // Call this from buttons that transition away (restart, next level, main menu)
    // so the re-assertion loop stops fighting the new screen.
    public void ClearNavigationTarget()
    {
        targetSelected = null;
    }

    private void MarkLevelComplete()
    {
        PlayerPrefs.SetInt($"Level{currentLevelIndex}Complete", 1);
        PlayerPrefs.Save();

        if (PlayerSettingsManager.Instance != null)
            PlayerSettingsManager.Instance.UpdateLevelSelectLock();
    }

    private IEnumerator HandleGameOverSequence()
    {
        if (musicSource != null && gameOverMusic != null)
        {
            musicSource.loop = false;
            musicSource.Stop();
            musicSource.clip = gameOverMusic;
            musicSource.Play();
        }

        Time.timeScale = slowTimeScale;
        yield return new WaitForSecondsRealtime(slowDuration);

        Time.timeScale = 0f;

        if (gameOverUIRoot != null)
        {
            gameOverUIRoot.SetActive(true);

            Image[] uiImages = gameOverUIRoot.GetComponentsInChildren<Image>(true);
            Text[] uiTexts = gameOverUIRoot.GetComponentsInChildren<Text>(true);

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(0f, maxOpacity, elapsed / fadeDuration);

                foreach (var img in uiImages)
                {
                    Color c = img.color;
                    c.a = alpha;
                    img.color = c;
                }

                foreach (var txt in uiTexts)
                {
                    Color c = txt.color;
                    c.a = alpha;
                    txt.color = c;
                }

                yield return null;
            }
        }

        yield return StartCoroutine(AssignUIFocus(gameOverFirstSelected));
    }

    private IEnumerator HandleLevelEndSequence()
    {
        if (musicSource != null && endMusic != null)
        {
            musicSource.loop = false;
            musicSource.Stop();
            musicSource.clip = endMusic;
            musicSource.Play();
        }

        Time.timeScale = slowTimeScale;
        yield return new WaitForSecondsRealtime(slowDuration);

        if (endUIRoot != null)
        {
            endUIRoot.SetActive(true);
            yield return StartCoroutine(FadeInUI());
        }

        Time.timeScale = 0f;

        if (musicSource != null && loopingMusic != null)
        {
            while (musicSource.isPlaying)
                yield return null;

            musicSource.loop = true;
            musicSource.clip = loopingMusic;
            musicSource.Play();
        }

        yield return StartCoroutine(AssignUIFocus(levelEndFirstSelected));
    }

    // Waits a frame for the UI and EventSystem to settle, then assigns focus
    // and sets targetSelected so Update() keeps re-asserting it.
    private IEnumerator AssignUIFocus(GameObject target)
    {
        if (target == null) yield break;

        // Wait two frames: one for the UI to fully activate, one for the
        // EventSystem to process any lingering pointer/gamepad events that
        // could immediately steal focus away again.
        yield return null;
        yield return null;

        if (EventSystem.current == null)
        {
            Debug.LogWarning("[LevelEndManager] No EventSystem found in scene.");
            yield break;
        }

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(target);
        targetSelected = target;
    }

    private IEnumerator FadeInUI()
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0f, maxOpacity, elapsed / fadeDuration);

            if (rootImage != null)
            {
                Color bgColor = rootImage.color;
                bgColor.a = alpha;
                rootImage.color = bgColor;
            }

            foreach (var img in childImages)
            {
                Color c = img.color;
                c.a = alpha;
                img.color = c;
            }

            foreach (var txt in childTexts)
            {
                Color c = txt.color;
                c.a = alpha;
                txt.color = c;
            }

            yield return null;
        }
    }
}