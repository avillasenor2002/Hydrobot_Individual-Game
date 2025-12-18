using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LevelEndManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject endUIRoot;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField, Range(0f, 1f)] private float maxOpacity = 0.8f;

    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip endMusic;      // First one-time track
    [SerializeField] private AudioClip loopingMusic;  // Second looping track

    [Header("Slow Motion Settings")]
    [SerializeField] private float slowTimeScale = 0.3f;
    [SerializeField] private float slowDuration = 2f;

    [Header("Game Over")]
    [SerializeField] private GameObject gameOverUIRoot; // Game Over screen root
    [SerializeField] private AudioClip gameOverMusic;   // Unique Game Over track

    [Header("UI Navigation")]
    [SerializeField] private GameObject gameOverFirstSelected;
    [SerializeField] private GameObject levelEndFirstSelected;

    private Image rootImage;
    private Image[] childImages;
    private Text[] childTexts;

    [Header("Level Settings")]
    [SerializeField] private int currentLevelIndex = 0; // 0-based index for this level

    private void Start()
    {
        if (endUIRoot != null)
        {
            endUIRoot.SetActive(false);

            rootImage = endUIRoot.GetComponent<Image>();
            childImages = endUIRoot.GetComponentsInChildren<Image>(true);
            childTexts = endUIRoot.GetComponentsInChildren<Text>(true);

            // Exclude root image from children array
            childImages = System.Array.FindAll(childImages, img => img != rootImage);
        }
    }

    public void TriggerLevelEnd()
    {
        // Mark level as complete and unlock next level
        MarkLevelComplete();

        StartCoroutine(HandleLevelEndSequence());
    }

    public void TriggerGameOver()
    {
        StartCoroutine(HandleGameOverSequence());
    }

    private void MarkLevelComplete()
    {
        // Mark current level as complete in PlayerPrefs
        PlayerPrefs.SetInt($"Level{currentLevelIndex}Complete", 1);
        PlayerPrefs.Save();

        // Unlock the next level (handled by PlayerSettingsManager)
        if (PlayerSettingsManager.Instance != null)
        {
            PlayerSettingsManager.Instance.UpdateLevelSelectLock();
        }
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

        Time.timeScale = 0f;

        if (gameOverFirstSelected != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(gameOverFirstSelected);
        }
    }

    private IEnumerator HandleLevelEndSequence()
    {
        // Start end music (non-looping)
        if (musicSource != null && endMusic != null)
        {
            musicSource.loop = false;
            musicSource.Stop();
            musicSource.clip = endMusic;
            musicSource.Play();
        }

        // Apply slow motion
        Time.timeScale = slowTimeScale;
        yield return new WaitForSecondsRealtime(slowDuration);

        // Begin UI fade-in
        if (endUIRoot != null)
        {
            endUIRoot.SetActive(true);
            yield return StartCoroutine(FadeInUI());
        }

        // Freeze gameplay
        Time.timeScale = 0f;

        // Wait for first music clip to finish
        if (musicSource != null && loopingMusic != null)
        {
            while (musicSource.isPlaying)
            {
                yield return null;
            }

            musicSource.loop = true;
            musicSource.clip = loopingMusic;
            musicSource.Play();
        }

        if (levelEndFirstSelected != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(levelEndFirstSelected);
        }
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
                Color color = img.color;
                color.a = alpha;
                img.color = color;
            }

            foreach (var txt in childTexts)
            {
                Color color = txt.color;
                color.a = alpha;
                txt.color = color;
            }

            yield return null;
        }
    }
}
