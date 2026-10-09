using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;

public class DialogueSystemFreeze : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup dialogueGroup;
    public Image backgroundOverlay; // Darkened background
    public TextMeshProUGUI dialogueTMP;
    public TextMeshProUGUI nameTMP;
    public Image characterIconImage; // Larger character images

    [Header("Settings")]
    public float fadeDuration = 0.25f;
    public float textSpeed = 0.03f; // Default typing speed (used if no dialogue clip)

    [Header("Gameplay References")]
    public GameObject player; // To disable player controls during dialogue

    [Header("Next Line Indicator")]
    public RectTransform nextLineTriangle; // Assign your triangle UI here
    public float pulseSpeed = 2f;
    public float pulseAmplitude = 0.2f;

    [Header("Voice Sounds (blips)")]
    public AudioSource audioSource;
    public AudioClip[] voiceSounds;          // Pool of clips to pick from randomly
    public int charsPerSound = 2;            // How many characters typed between each sound
    public float voicePitchMin = 0.9f;       // Random pitch range min
    public float voicePitchMax = 1.1f;       // Random pitch range max
    public bool silenceOnSpaces = true;      // Skip sound for spaces and punctuation

    [Header("Dialogue Clip")]
    // Optional: dedicated AudioSource for DialogueData.DialougeClip.
    // If left empty, 'audioSource' is used and typing blips are skipped while
    // the dialogue clip plays (so pitch doesn't get overwritten).
    public AudioSource dialogueClipSource;
    public bool matchTextSpeedToAudio = true;   // Type one letter per (clipLength / letterCount)

    private int charsSinceLastSound = 0;

    private DialogueData[] currentDialogueLines;
    private int currentLineIndex = 0;
    private bool isTyping = false;
    private bool showTriangle = false;
    private bool isDialogueActive = false;
    private Vector3 triangleOriginalScale;

    private Coroutine typingRoutine;

    public event System.Action OnDialogueFinished;

    private void Start()
    {
        dialogueGroup.alpha = 0f;
        dialogueGroup.gameObject.SetActive(false);

        if (backgroundOverlay != null)
            backgroundOverlay.gameObject.SetActive(false);

        if (nextLineTriangle != null)
        {
            nextLineTriangle.gameObject.SetActive(false);
            triangleOriginalScale = nextLineTriangle.localScale;
        }
    }

    public void StartDialogue(DialogueData[] lines)
    {
        if (lines == null || lines.Length == 0) return;

        if (isDialogueActive)
        {
            AppendDialogue(lines);
            return;
        }

        isDialogueActive = true;
        currentDialogueLines = lines;
        currentLineIndex = 0;

        // Freeze gameplay
        if (player != null)
            player.SetActive(false);

        // Show darkened background
        if (backgroundOverlay != null)
            backgroundOverlay.gameObject.SetActive(true);

        // Fade in once, at the start of the whole conversation
        dialogueGroup.gameObject.SetActive(true);
        StartCoroutine(FadeCanvasGroup(0f, 1f, fadeDuration));

        ShowCurrentLine();
    }

    private void AppendDialogue(DialogueData[] newLines)
    {
        int existingCount = currentDialogueLines.Length;
        DialogueData[] merged = new DialogueData[existingCount + newLines.Length];
        currentDialogueLines.CopyTo(merged, 0);
        newLines.CopyTo(merged, existingCount);
        currentDialogueLines = merged;
    }

    private void Update()
    {
        if (!isDialogueActive) return;

        // Progress dialogue with Submit button (East gamepad button)
        if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
        {
            if (!isTyping)
            {
                NextLine();
            }
            else
            {
                FinishTypingInstantly();
            }
        }

        // Pulsating triangle effect
        if (showTriangle && nextLineTriangle != null)
        {
            float scaleOffset = Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmplitude;
            nextLineTriangle.localScale = triangleOriginalScale * (1f + scaleOffset);
        }
    }

    private void ShowCurrentLine()
    {
        if (currentDialogueLines == null || currentLineIndex >= currentDialogueLines.Length)
            return;

        DialogueData data = currentDialogueLines[currentLineIndex];

        nameTMP.text = data.characterName;
        if (data.characterIcon != null)
            characterIconImage.sprite = data.characterIcon;
        dialogueTMP.text = "";

        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        // Stop any audio still playing from a previous line
        StopAllDialogueAudio();

        typingRoutine = StartCoroutine(TypeText(data));
    }

    // MODIFIED: now takes the whole DialogueData so it can read the clip
    private IEnumerator TypeText(DialogueData data)
    {
        isTyping = true;
        dialogueTMP.text = "";
        charsSinceLastSound = 0;

        string text = data.dialogueText ?? string.Empty;
        AudioClip clip = data.DialougeClip;

        if (nextLineTriangle != null)
            nextLineTriangle.gameObject.SetActive(false);

        // Which AudioSource plays the dialogue clip?
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

        // ---- Match typing speed to the clip length ----
        float typeSpeed = data.textSpeed > 0f ? data.textSpeed : textSpeed;
        if (matchTextSpeedToAudio && clip != null && clip.length > 0f && text.Length > 0)
        {
            typeSpeed = clip.length / text.Length;
        }

        // ---- Type the text ----
        foreach (char c in text)
        {
            dialogueTMP.text += c;

            // Only play blips if the dialogue clip isn't sharing the same AudioSource
            // (sharing would overwrite the clip's pitch).
            if (usingSeparateBlipSource || clip == null)
                PlayVoiceSound(c);

            if (typeSpeed > 0f)
                yield return new WaitForSecondsRealtime(typeSpeed);
            else
                yield return null;
        }

        // ---- Wait for the clip to finish before showing the "next line" triangle ----
        if (clip != null && clipSource != null)
        {
            float elapsed = 0f;
            // Safety cap in case isPlaying gets stuck or the clip was restarted elsewhere
            while (clipSource.isPlaying && elapsed < clip.length + 1f)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        isTyping = false;
        typingRoutine = null;

        // Show pulsating triangle when line + audio are complete
        if (nextLineTriangle != null)
        {
            nextLineTriangle.gameObject.SetActive(true);
            showTriangle = true;
        }
    }

    private void FinishTypingInstantly()
    {
        if (currentDialogueLines == null || currentLineIndex >= currentDialogueLines.Length)
            return;

        DialogueData data = currentDialogueLines[currentLineIndex];
        if (typingRoutine != null)
            StopCoroutine(typingRoutine);
        typingRoutine = null;

        // Player skipped the line — stop the dialogue clip too so it doesn't
        // keep talking while the text is already fully displayed.
        StopAllDialogueAudio();

        dialogueTMP.text = data.dialogueText;
        isTyping = false;

        if (nextLineTriangle != null)
        {
            nextLineTriangle.gameObject.SetActive(true);
            showTriangle = true;
        }
    }

    private void NextLine()
    {
        showTriangle = false;
        if (nextLineTriangle != null)
            nextLineTriangle.gameObject.SetActive(false);

        // Fade/move on — make sure the previous line's clip is silenced
        StopAllDialogueAudio();

        currentLineIndex++;

        if (currentLineIndex >= currentDialogueLines.Length)
        {
            EndDialogue();
        }
        else
        {
            ShowCurrentLine();
        }
    }

    private void EndDialogue()
    {
        isDialogueActive = false;
        StopAllDialogueAudio();
        StartCoroutine(FadeOutAndResume());
    }

    private IEnumerator FadeOutAndResume()
    {
        yield return StartCoroutine(FadeCanvasGroup(1f, 0f, fadeDuration));

        dialogueGroup.gameObject.SetActive(false);
        if (backgroundOverlay != null)
            backgroundOverlay.gameObject.SetActive(false);

        // Resume gameplay
        if (player != null)
            player.SetActive(true);

        OnDialogueFinished?.Invoke();
    }

    private void StopAllDialogueAudio()
    {
        if (audioSource != null) audioSource.Stop();
        if (dialogueClipSource != null) dialogueClipSource.Stop();
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

    private IEnumerator FadeCanvasGroup(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            dialogueGroup.alpha = Mathf.Lerp(from, to, t / duration);
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        dialogueGroup.alpha = to;
    }
}