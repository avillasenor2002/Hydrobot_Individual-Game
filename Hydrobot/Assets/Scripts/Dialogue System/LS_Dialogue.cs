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
    public float textSpeed = 0.03f; // Default typing speed

    [Header("Gameplay References")]
    public GameObject player; // To disable player controls during dialogue

    [Header("Next Line Indicator")]
    public RectTransform nextLineTriangle; // Assign your triangle UI here
    public float pulseSpeed = 2f;
    public float pulseAmplitude = 0.2f;

    [Header("Voice Sounds")]
    public AudioSource audioSource;
    public AudioClip[] voiceSounds;          // Pool of clips to pick from randomly
    public int charsPerSound = 2;            // How many characters typed between each sound
    public float voicePitchMin = 0.9f;       // Random pitch range min
    public float voicePitchMax = 1.1f;       // Random pitch range max
    public bool silenceOnSpaces = true;      // Skip sound for spaces and punctuation

    private int charsSinceLastSound = 0;

    private DialogueData[] currentDialogueLines;
    private int currentLineIndex = 0;
    private bool isTyping = false;
    private bool showTriangle = false;
    private bool isDialogueActive = false; // FIX: guard against re-triggering mid-dialogue
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

        // FIX: if dialogue is already running, queue the new lines onto the current
        // session instead of fading out and back in.
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

        // FIX: only fade in once, at the start of the whole conversation
        dialogueGroup.gameObject.SetActive(true);
        StartCoroutine(FadeCanvasGroup(0f, 1f, fadeDuration));

        ShowCurrentLine();
    }

    // FIX: appends new lines to the active dialogue without touching the UI visibility
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
        // Only process input while dialogue is active
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

        typingRoutine = StartCoroutine(TypeText(data.dialogueText));
    }

    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        dialogueTMP.text = "";
        charsSinceLastSound = 0;

        if (nextLineTriangle != null)
            nextLineTriangle.gameObject.SetActive(false);

        foreach (char c in text)
        {
            dialogueTMP.text += c;
            PlayVoiceSound(c);
            yield return new WaitForSecondsRealtime(textSpeed); // FIX: unscaled so it works if timeScale = 0
        }

        isTyping = false;

        // Show pulsating triangle when line is complete
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
        isDialogueActive = false; // FIX: clear the guard before fading out
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