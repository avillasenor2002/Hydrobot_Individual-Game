using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DialogueSystem : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup dialogueGroup;
    public TextMeshProUGUI dialogueTMP;
    public TextMeshProUGUI nameTMP;
    public Image characterIconImage;

    [Header("Auto Hide Timer UI")]
    public Image timerFillImage;

    [Header("Fade Settings")]
    public float fadeDuration = 0.25f;

    [Header("Voice Sounds (blips)")]
    public AudioSource audioSource;
    public AudioClip[] voiceSounds;          // Pool of clips to pick from randomly
    public int charsPerSound = 2;            // How many characters typed between each sound
    public float voicePitchMin = 0.9f;       // Random pitch range min
    public float voicePitchMax = 1.1f;       // Random pitch range max
    public bool silenceOnSpaces = true;      // Skip sound for spaces and punctuation

    [Header("Dialogue Clip")]
    // Optional: dedicated AudioSource for the DialogueData.DialougeClip.
    // If left empty, 'audioSource' is used and the typing blips are skipped
    // while a dialogue clip is playing (so the pitch doesn't get messed up).
    public AudioSource dialogueClipSource;
    public bool matchTextSpeedToAudio = true;   // Type one letter per (clipLength / letterCount)

    private int charsSinceLastSound = 0;

    private Coroutine typingRoutine;
    private Coroutine hideRoutine;

    public event System.Action OnDialogueFinished;

    private void Start()
    {
        dialogueGroup.alpha = 0f;
        dialogueGroup.gameObject.SetActive(false);
    }

    public void ShowDialogue(DialogueData data)
    {
        if (typingRoutine != null)
            StopCoroutine(typingRoutine);
        if (hideRoutine != null)
            StopCoroutine(hideRoutine);

        // Stop anything still playing from a previous line
        if (audioSource != null) audioSource.Stop();
        if (dialogueClipSource != null) dialogueClipSource.Stop();

        dialogueGroup.gameObject.SetActive(true);

        nameTMP.text = data.characterName;
        characterIconImage.sprite = data.characterIcon;
        dialogueTMP.text = "";

        if (timerFillImage != null)
            timerFillImage.fillAmount = 1f;

        StartCoroutine(FadeCanvasGroup(0f, 1f, fadeDuration));
        typingRoutine = StartCoroutine(TypeText(data));
    }

    private IEnumerator TypeText(DialogueData data)
    {
        dialogueTMP.text = "";
        charsSinceLastSound = 0;

        string text = data.dialogueText ?? string.Empty;
        AudioClip clip = data.DialougeClip;

        // Where does the dialogue clip play?
        AudioSource clipSource = dialogueClipSource != null ? dialogueClipSource : audioSource;
        bool usingSeparateBlipSource = dialogueClipSource != null;

        // ---- Start the dialogue audio clip ----
        if (clip != null && clipSource != null)
        {
            clipSource.Stop();
            clipSource.clip = clip;
            clipSource.loop = false;
            clipSource.pitch = 1f;
            clipSource.Play();
        }

        // ---- Work out the typing speed so the text matches the audio length ----
        float typeSpeed = data.textSpeed;
        if (matchTextSpeedToAudio && clip != null && clip.length > 0f && text.Length > 0)
        {
            typeSpeed = clip.length / text.Length;
        }

        // ---- Type the text ----
        foreach (char c in text)
        {
            dialogueTMP.text += c;

            // Only play blips if the dialogue clip isn't using the same AudioSource
            // (sharing it would overwrite the clip's pitch).
            if (usingSeparateBlipSource || clip == null)
                PlayVoiceSound(c);

            if (typeSpeed > 0f)
                yield return new WaitForSeconds(typeSpeed);
            else
                yield return null;
        }

        // ---- Wait for the audio clip to finish before starting the auto-hide timer ----
        if (clip != null && clipSource != null)
        {
            float elapsed = 0f;
            // Safety cap in case isPlaying gets stuck or the clip was restarted elsewhere
            while (clipSource.isPlaying && elapsed < clip.length + 1f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        typingRoutine = null;
        hideRoutine = StartCoroutine(AutoHideTimer(data.autoHideTime));
    }

    private void PlayVoiceSound(char c)
    {
        if (audioSource == null || voiceSounds == null || voiceSounds.Length == 0) return;

        // Optionally skip whitespace and punctuation
        if (silenceOnSpaces && (char.IsWhiteSpace(c) || char.IsPunctuation(c))) return;

        charsSinceLastSound++;
        if (charsSinceLastSound < charsPerSound) return;
        charsSinceLastSound = 0;

        AudioClip clip = voiceSounds[Random.Range(0, voiceSounds.Length)];
        audioSource.pitch = Random.Range(voicePitchMin, voicePitchMax);
        audioSource.PlayOneShot(clip);
    }

    private IEnumerator AutoHideTimer(float time)
    {
        float timer = 0f;
        while (timer < time)
        {
            timer += Time.deltaTime;
            if (timerFillImage != null)
                timerFillImage.fillAmount = Mathf.Lerp(1f, 0f, timer / time);
            yield return null;
        }

        yield return StartCoroutine(FadeCanvasGroup(1f, 0f, fadeDuration));
        dialogueGroup.gameObject.SetActive(false);
        OnDialogueFinished?.Invoke();
    }

    private IEnumerator FadeCanvasGroup(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            dialogueGroup.alpha = Mathf.Lerp(from, to, t / duration);
            t += Time.deltaTime;
            yield return null;
        }
        dialogueGroup.alpha = to;
    }
}