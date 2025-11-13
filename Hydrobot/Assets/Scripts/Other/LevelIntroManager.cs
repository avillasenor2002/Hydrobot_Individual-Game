using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

public class LevelIntroManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject introUIRoot; // Assign the root GameObject of your intro UI
    [SerializeField] private float fadeDuration = 1f;

    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip introMusic;
    [SerializeField] private AudioClip mainMusic;

    private bool isIntroActive = true;
    private Image[] uiImages;
    private Text[] uiTexts;

    private void Start()
    {
        Time.timeScale = 0f; // Pause gameplay

        if (musicSource != null && introMusic != null)
        {
            musicSource.clip = introMusic;
            musicSource.Play();
        }

        if (introUIRoot != null)
        {
            uiImages = introUIRoot.GetComponentsInChildren<Image>(true);
            uiTexts = introUIRoot.GetComponentsInChildren<Text>(true);
        }
    }

    private void Update()
    {
        if (!isIntroActive) return;

        bool anyKeyboard = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
        bool anyGamepad = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame ||
                          Gamepad.current.buttonNorth.wasPressedThisFrame ||
                          Gamepad.current.buttonEast.wasPressedThisFrame ||
                          Gamepad.current.buttonWest.wasPressedThisFrame ||
                          Gamepad.current.startButton.wasPressedThisFrame ||
                          Gamepad.current.selectButton.wasPressedThisFrame;

        if (anyKeyboard || anyGamepad)
        {
            StartCoroutine(FadeOutIntro());
        }
    }

    private IEnumerator FadeOutIntro()
    {
        isIntroActive = false;

        if (musicSource != null && mainMusic != null)
        {
            musicSource.Stop();
            musicSource.clip = mainMusic;
            musicSource.Play();
        }

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);

            foreach (var img in uiImages)
            {
                Color color = img.color;
                color.a = alpha;
                img.color = color;
            }

            foreach (var txt in uiTexts)
            {
                Color color = txt.color;
                color.a = alpha;
                txt.color = color;
            }

            yield return null;
        }

        introUIRoot.SetActive(false); // Fully disable UI
        Time.timeScale = 1f; // Resume gameplay
    }
}
