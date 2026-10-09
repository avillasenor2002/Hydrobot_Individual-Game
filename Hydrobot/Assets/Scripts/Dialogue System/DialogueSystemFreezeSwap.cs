using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;

/// <summary>
/// DialogueSystemFreeze with speaker portrait swapping.
///
/// For every line:
///   1. The line's speakerIndex picks the ACTIVE portrait (0 = A, 1 = B).
///   2. The active portrait's Image.sprite is set to the line's
///      DialogueData.characterIcon — the expression for that line.
///   3. The active portrait is restored to its original color and scale.
///   4. The inactive portrait is grayed out and slightly shrunk.
///
/// Portrait visuals are re-asserted every frame in LateUpdate so nothing
/// can override them.
/// </summary>
public class DialogueSystemFreezeSwap : MonoBehaviour
{
    [System.Serializable]
    public class SpeakerLine
    {
        [Tooltip("The line to play. Its characterIcon is the expression shown on the active portrait.")]
        public DialogueData dialogue;

        [Tooltip("Which portrait is the active speaker for this line. 0 = A, 1 = B.")]
        [Range(0, 1)]
        public int speakerIndex = 0;
    }

    [Header("UI References")]
    public CanvasGroup dialogueGroup;
    public Image backgroundOverlay;
    public TextMeshProUGUI dialogueTMP;
    public TextMeshProUGUI nameTMP;

    [Header("Speaker Portraits")]
    [Tooltip("First speaker portrait.")]
    public Image speakerA;

    [Tooltip("Second speaker portrait.")]
    public Image speakerB;

    [Tooltip("Which portrait starts active. 0 = A, 1 = B.")]
    [Range(0, 1)]
    public int startingSpeakerIndex = 0;

    [Header("Inactive Portrait Visuals")]
    [Tooltip("Tint applied to the inactive portrait. Alpha is preserved from the original.")]
    public Color inactiveColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    [Tooltip("Scale multiplier applied to the inactive portrait.")]
    [Range(0.1f, 1f)]
    public float inactiveScaleMultiplier = 0.9f;

    [Header("Settings")]
    public float fadeDuration = 0.25f;
    public float textSpeed = 0.03f;

    [Header("Gameplay References")]
    public GameObject player;

    [Header("Next Line Indicator")]
    public RectTransform nextLineTriangle;
    public float pulseSpeed = 2f;
    public float pulseAmplitude = 0.2f;

    [Header("Voice Sounds (blips)")]
    public AudioSource audioSource;
    public AudioClip[] voiceSounds;
    public int charsPerSound = 2;
    public float voicePitchMin = 0.9f;
    public float voicePitchMax = 1.1f;
    public bool silenceOnSpaces = true;

    [Header("Dialogue Clip")]
    public AudioSource dialogueClipSource;
    public bool matchTextSpeedToAudio = true;

    [Header("Debug")]
    [Tooltip("Log which portrait is active and which sprite was applied.")]
    public bool debugLogging = false;

    // ------------------------------------------------------------------
    // Runtime state
    // ------------------------------------------------------------------
    private int charsSinceLastSound = 0;

    private SpeakerLine[] currentSpeakerLines;
    private int currentLineIndex = 0;
    private bool isTyping = false;
    private bool showTriangle = false;
    private bool isDialogueActive = false;
    private Vector3 triangleOriginalScale;

    private Coroutine typingRoutine;

    // Portrait state — color/scale originals captured at Awake.
    private Image[] portraits;
    private Color[] originalColors;
    private Vector3[] originalScales;
    private int activeSpeakerIndex;

    public event System.Action OnDialogueFinished;

    // ------------------------------------------------------------------
    // Lifecycle
    // ------------------------------------------------------------------

    private void Awake()
    {
        if (speakerA == null)
            Debug.LogWarning("[DialogueSystemFreezeSwap] 'speakerA' is not assigned!", this);
        if (speakerB == null)
            Debug.LogWarning("[DialogueSystemFreezeSwap] 'speakerB' is not assigned!", this);

        portraits = new Image[] { speakerA, speakerB };
        originalColors = new Color[portraits.Length];
        originalScales = new Vector3[portraits.Length];

        for (int i = 0; i < portraits.Length; i++)
        {
            if (portraits[i] == null) continue;
            originalColors[i] = portraits[i].color;
            originalScales[i] = portraits[i].transform.localScale;
        }

        activeSpeakerIndex = Mathf.Clamp(startingSpeakerIndex, 0, portraits.Length - 1);
        ApplySpeakerVisuals();
    }

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

    private void LateUpdate()
    {
        // Re-apply portrait visuals every frame while dialogue is running so
        // nothing can stomp the active/inactive look. This only touches
        // color and scale, NOT the sprite — sprite swapping is handled
        // explicitly per line.
        if (isDialogueActive)
            ApplySpeakerVisuals();
    }

    // ------------------------------------------------------------------
    // START DIALOGUE
    // ------------------------------------------------------------------

    public void StartDialogue(SpeakerLine[] lines)
    {
        if (lines == null || lines.Length == 0) return;

        if (isDialogueActive)
        {
            AppendSpeakerDialogue(lines);
            return;
        }

        isDialogueActive = true;
        currentSpeakerLines = lines;
        currentLineIndex = 0;

        if (player != null)
            player.SetActive(false);

        if (backgroundOverlay != null)
            backgroundOverlay.gameObject.SetActive(true);

        dialogueGroup.gameObject.SetActive(true);
        StartCoroutine(FadeCanvasGroup(0f, 1f, fadeDuration));

        ShowCurrentSpeakerLine();
    }

    public void StartDialogue(DialogueData[] lines)
    {
        if (lines == null || lines.Length == 0) return;

        SpeakerLine[] wrapped = new SpeakerLine[lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            wrapped[i] = new SpeakerLine
            {
                dialogue = lines[i],
                speakerIndex = activeSpeakerIndex
            };
        }

        StartDialogue(wrapped);
    }

    private void AppendSpeakerDialogue(SpeakerLine[] newLines)
    {
        int existingCount = currentSpeakerLines.Length;
        SpeakerLine[] merged = new SpeakerLine[existingCount + newLines.Length];
        currentSpeakerLines.CopyTo(merged, 0);
        newLines.CopyTo(merged, existingCount);
        currentSpeakerLines = merged;
    }

    // ------------------------------------------------------------------
    // Update
    // ------------------------------------------------------------------

    private void Update()
    {
        if (!isDialogueActive) return;

        if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
        {
            if (!isTyping) NextLine();
            else FinishTypingInstantly();
        }

        if (showTriangle && nextLineTriangle != null)
        {
            float scaleOffset = Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmplitude;
            nextLineTriangle.localScale = triangleOriginalScale * (1f + scaleOffset);
        }
    }

    // ------------------------------------------------------------------
    // LINE PLAYBACK
    // ------------------------------------------------------------------

    private void ShowCurrentSpeakerLine()
    {
        if (currentSpeakerLines == null || currentLineIndex >= currentSpeakerLines.Length)
            return;

        SpeakerLine line = currentSpeakerLines[currentLineIndex];
        DialogueData data = line.dialogue;

        // --- Set the active portrait for this line ---
        activeSpeakerIndex = Mathf.Clamp(line.speakerIndex, 0, portraits.Length - 1);

        // --- Swap the ACTIVE portrait's sprite to the expression in the data ---
        Image activePortrait = portraits[activeSpeakerIndex];
        if (activePortrait != null && data.characterIcon != null)
        {
            activePortrait.sprite = data.characterIcon;

            if (debugLogging)
                Debug.Log($"[DialogueSystemFreezeSwap] Line {currentLineIndex}: portrait {activeSpeakerIndex} " +
                          $"sprite = '{data.characterIcon.name}'.", this);
        }
        else if (debugLogging)
        {
            Debug.Log($"[DialogueSystemFreezeSwap] Line {currentLineIndex}: portrait {activeSpeakerIndex} " +
                      $"sprite unchanged ({(activePortrait == null ? "portrait null" : "data icon null")}).", this);
        }

        // --- Apply the active/inactive look ---
        ApplySpeakerVisuals();

        // --- Populate the dialogue box ---
        nameTMP.text = data.characterName;
        dialogueTMP.text = "";

        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        StopAllDialogueAudio();

        typingRoutine = StartCoroutine(TypeText(data));
    }

    private IEnumerator TypeText(DialogueData data)
    {
        isTyping = true;
        dialogueTMP.text = "";
        charsSinceLastSound = 0;

        string text = data.dialogueText ?? string.Empty;
        AudioClip clip = data.DialougeClip;

        if (nextLineTriangle != null)
            nextLineTriangle.gameObject.SetActive(false);

        AudioSource clipSource = dialogueClipSource != null ? dialogueClipSource : audioSource;
        bool usingSeparateBlipSource = dialogueClipSource != null;

        if (clip != null && clipSource != null)
        {
            clipSource.Stop();
            clipSource.clip = clip;
            clipSource.loop = false;
            clipSource.pitch = 1f;
            clipSource.Play();
        }

        float typeSpeed = data.textSpeed > 0f ? data.textSpeed : textSpeed;
        if (matchTextSpeedToAudio && clip != null && clip.length > 0f && text.Length > 0)
            typeSpeed = clip.length / text.Length;

        foreach (char c in text)
        {
            dialogueTMP.text += c;

            if (usingSeparateBlipSource || clip == null)
                PlayVoiceSound(c);

            if (typeSpeed > 0f)
                yield return new WaitForSecondsRealtime(typeSpeed);
            else
                yield return null;
        }

        if (clip != null && clipSource != null)
        {
            float elapsed = 0f;
            while (clipSource.isPlaying && elapsed < clip.length + 1f)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        isTyping = false;
        typingRoutine = null;

        if (nextLineTriangle != null)
        {
            nextLineTriangle.gameObject.SetActive(true);
            showTriangle = true;
        }
    }

    private void FinishTypingInstantly()
    {
        if (currentSpeakerLines == null || currentLineIndex >= currentSpeakerLines.Length)
            return;

        DialogueData data = currentSpeakerLines[currentLineIndex].dialogue;

        if (typingRoutine != null)
            StopCoroutine(typingRoutine);
        typingRoutine = null;

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

        StopAllDialogueAudio();

        currentLineIndex++;

        if (currentLineIndex >= currentSpeakerLines.Length)
            EndDialogue();
        else
            ShowCurrentSpeakerLine();
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

        if (player != null)
            player.SetActive(true);

        OnDialogueFinished?.Invoke();
    }

    // ------------------------------------------------------------------
    // PORTRAIT VISUALS
    // ------------------------------------------------------------------

    /// <summary>
    /// Active portrait: original color + scale.
    /// Inactive portrait: gray (preserving alpha) + shrunk.
    /// Does NOT touch sprites.
    /// </summary>
    private void ApplySpeakerVisuals()
    {
        if (portraits == null) return;

        for (int i = 0; i < portraits.Length; i++)
        {
            Image img = portraits[i];
            if (img == null) continue;

            if (i == activeSpeakerIndex)
            {
                img.color = originalColors[i];
                img.transform.localScale = originalScales[i];
            }
            else
            {
                Color c = inactiveColor;
                c.a = originalColors[i].a;
                img.color = c;
                img.transform.localScale = originalScales[i] * inactiveScaleMultiplier;
            }
        }
    }

    /// <summary>Force-sets which portrait is active. Safe to call anytime.</summary>
    public void SetActiveSpeaker(int index)
    {
        activeSpeakerIndex = Mathf.Clamp(index, 0, portraits.Length - 1);
        ApplySpeakerVisuals();
    }

    /// <summary>Restores both portraits to their original color and scale.</summary>
    public void ResetSpeakers()
    {
        activeSpeakerIndex = Mathf.Clamp(startingSpeakerIndex, 0, portraits.Length - 1);

        if (portraits == null) return;

        for (int i = 0; i < portraits.Length; i++)
        {
            Image img = portraits[i];
            if (img == null) continue;

            img.color = originalColors[i];
            img.transform.localScale = originalScales[i];
        }
    }

    // ------------------------------------------------------------------
    // AUDIO HELPERS
    // ------------------------------------------------------------------

    private void StopAllDialogueAudio()
    {
        if (audioSource != null) audioSource.Stop();
        if (dialogueClipSource != null) dialogueClipSource.Stop();
    }

    private void PlayVoiceSound(char c)
    {
        if (audioSource == null || voiceSounds == null || voiceSounds.Length == 0) return;
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