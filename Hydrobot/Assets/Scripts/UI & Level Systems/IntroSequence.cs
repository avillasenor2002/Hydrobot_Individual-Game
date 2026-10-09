using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// Plays an intro sequence: several DialogueData lines in order, then fades
/// a UI element away, optionally plays one more line, moves the camera's Y
/// position to a preset height, activates an object and fades its sprite,
/// UI Image, and TextMeshPro up to full opacity, waits for any player input,
/// presses the assigned button, plays a final audio-only dialogue clip, and
/// starts looping music.
/// </summary>
public class IntroDialogueSequence : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup dialogueGroup;
    public TextMeshProUGUI dialogueTMP;

    [Header("Fade Settings")]
    public float fadeDuration = 0.25f;

    [Header("Dialogue Clip Audio")]
    [Tooltip("AudioSource used to play each DialogueData clip.")]
    public AudioSource dialogueClipSource;

    [Tooltip("If true, each line's reveal is timed so it finishes when the audio clip ends.")]
    public bool matchTextSpeedToAudio = true;

    [Header("Sequence — Dialogue")]
    [Tooltip("Lines played in order before the UI fade.")]
    public DialogueData[] dialogueClips;

    [Tooltip("How many of dialogueClips play before the UI element fades away.")]
    public int clipsBeforeUIFade = 3;

    [Tooltip("Optional line played after the UI fade, before the camera moves. Leave empty to skip.")]
    public DialogueData fourthClip;

    [Header("Sequence — UI Fade Out")]
    [Tooltip("CanvasGroup of the UI element to fade away after the first dialogue clips.")]
    public CanvasGroup uiToFadeOut;

    [Tooltip("How long the UI fade takes.")]
    public float uiFadeOutDuration = 1f;

    [Tooltip("Pause after the UI fade finishes, in seconds.")]
    public float delayAfterUIFade = 0.5f;

    [Header("Sequence — Camera Move (Y only)")]
    [Tooltip("The camera (or camera rig) to move. Only its Y position is changed.")]
    public Transform cameraTransform;

    [Tooltip("The Y position the camera moves to. X and Z stay untouched.")]
    public float cameraTargetY = 0f;

    [Tooltip("How long the camera move takes.")]
    public float cameraMoveDuration = 2f;

    [Tooltip("Optional ease curve for the camera move. Falls back to linear.")]
    public AnimationCurve cameraMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Sequence — Object Fade In")]
    [Tooltip("Object activated just before the fade begins.")]
    public GameObject objectToActivate;

    [Tooltip("SpriteRenderer whose alpha is faded from 0 to full opacity.")]
    public SpriteRenderer spriteToFade;

    [Tooltip("UI Image whose alpha is faded from 0 to full opacity.")]
    public Image imageToFade;

    [Tooltip("TextMeshProUGUI whose alpha is faded from 0 to full opacity.")]
    public TextMeshProUGUI tmpToFade;

    [Tooltip("How long the fade-in takes.")]
    public float objectFadeInDuration = 1f;

    [Header("Sequence — Button Selection")]
    [Tooltip("Button that gets selected and pressed once the player provides any input.")]
    public Button buttonToSelect;

    [Tooltip("If true, the button's onClick is invoked as soon as input arrives. " +
             "If false, the button is only focused (highlighted).")]
    public bool pressButtonOnAnyInput = true;

    [Tooltip("Ignore input for a short window after the fade so the input that " +
             "started the sequence can't carry over.")]
    public float inputCooldown = 0.25f;

    [Header("Sequence — Final Audio-Only Dialogue")]
    [Tooltip("Dialogue clip whose audio plays at the end. Its text is hidden, so only the clip is heard.")]
    public DialogueData finalAudioOnlyClip;

    [Header("Sequence — Music")]
    [Tooltip("Separate AudioSource for the looping music track.")]
    public AudioSource musicSource;

    [Tooltip("Music clip that starts looping after the final dialogue clip begins.")]
    public AudioClip musicClip;

    [Header("Options")]
    [Tooltip("If true, the sequence starts automatically on Start.")]
    public bool playOnStart = true;

    // ------------------------------------------------------------------
    // Runtime state
    // ------------------------------------------------------------------
    private Coroutine typingRoutine;
    private bool dialogueActive;

    // Cached original sprite / image / TMP colors so the fade always ends
    // at "full opacity" as authored, but the RGB components are preserved.
    private Color spriteOriginal = Color.white;
    private Color imageOriginal = Color.white;
    private Color tmpOriginal = Color.white;

    private Coroutine inputRoutine;

    public event System.Action OnDialogueFinished;
    public event System.Action OnSequenceComplete;

    // ------------------------------------------------------------------

    private void Awake()
    {
        // Cache the RGB and set alpha to 0 so the fade starts hidden.
        if (spriteToFade != null)
        {
            spriteOriginal = spriteToFade.color;
            Color c = spriteOriginal; c.a = 0f;
            spriteToFade.color = c;
        }

        if (imageToFade != null)
        {
            imageOriginal = imageToFade.color;
            Color c = imageOriginal; c.a = 0f;
            imageToFade.color = c;
        }

        if (tmpToFade != null)
        {
            tmpOriginal = tmpToFade.color;
            Color c = tmpOriginal; c.a = 0f;
            tmpToFade.color = c;
        }
    }

    private void Start()
    {
        if (dialogueGroup != null)
        {
            dialogueGroup.alpha = 0f;
            dialogueGroup.gameObject.SetActive(false);
        }

        if (playOnStart)
            StartSequence();
    }

    // ------------------------------------------------------------------
    // SEQUENCE
    // ------------------------------------------------------------------

    /// <summary>Kicks off the whole intro sequence.</summary>
    public void StartSequence()
    {
        StartCoroutine(SequenceRoutine());
    }

    private IEnumerator SequenceRoutine()
    {
        // 1. Play the first batch of dialogue clips in order.
        if (dialogueClips != null)
        {
            int count = Mathf.Clamp(clipsBeforeUIFade, 0, dialogueClips.Length);

            for (int i = 0; i < count; i++)
            {
                if (dialogueClips[i] == null) continue;
                yield return StartCoroutine(PlayDialogueAndWait(dialogueClips[i]));
            }
        }

        // 2. Fade the assigned UI element away.
        if (uiToFadeOut != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(uiToFadeOut, 1f, 0f, uiFadeOutDuration));
        }

        if (delayAfterUIFade > 0f)
            yield return new WaitForSeconds(delayAfterUIFade);

        // 3. Optional fourth dialogue clip after the UI fade.
        if (fourthClip != null)
        {
            yield return StartCoroutine(PlayDialogueAndWait(fourthClip));
        }

        // 4. Move only the camera's Y position.
        if (cameraTransform != null)
        {
            yield return StartCoroutine(MoveCameraY(
                cameraTransform,
                cameraTargetY,
                cameraMoveDuration));
        }

        // 5. Activate the object and fade sprite + image + TMP to full opacity.
        if (objectToActivate != null)
            objectToActivate.SetActive(true);

        yield return StartCoroutine(FadeInObjectRoutine(objectFadeInDuration));

        // 6. Wait for any player input, then select + press the button.
        inputRoutine = StartCoroutine(WaitForAnyInputAndPress());

        // 7. Play the final dialogue clip as audio only, and start the music.
        StartCoroutine(PlayAudioOnlyDialogue(finalAudioOnlyClip));
        StartLoopingMusic();

        OnSequenceComplete?.Invoke();
    }

    // Waits for a single dialogue clip to fully finish.
    private IEnumerator PlayDialogueAndWait(DialogueData data)
    {
        ShowDialogue(data);

        while (dialogueActive)
            yield return null;
    }

    // ------------------------------------------------------------------
    // OBJECT FADE — sprite + UI Image + TMP
    // ------------------------------------------------------------------

    private IEnumerator FadeInObjectRoutine(float duration)
    {
        // Start fully invisible.
        SetFadeAlpha(0f);

        if (duration <= 0f)
        {
            SetFadeAlpha(1f);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetFadeAlpha(t);
            yield return null;
        }

        // Land exactly on full opacity.
        SetFadeAlpha(1f);
    }

    /// <summary>
    /// Lerps sprite, image, and TMP alpha between 0 and 1.
    /// t = 0 is invisible, t = 1 is full opacity.
    /// </summary>
    private void SetFadeAlpha(float t)
    {
        t = Mathf.Clamp01(t);

        if (spriteToFade != null)
        {
            Color c = spriteOriginal;
            c.a = t;                       // full opacity is 1, not the authored alpha
            spriteToFade.color = c;
        }

        if (imageToFade != null)
        {
            Color c = imageOriginal;
            c.a = t;
            imageToFade.color = c;
        }

        if (tmpToFade != null)
        {
            Color c = tmpOriginal;
            c.a = t;
            tmpToFade.color = c;
        }
    }

    // ------------------------------------------------------------------
    // INPUT — press any button to continue
    // ------------------------------------------------------------------

    private IEnumerator WaitForAnyInputAndPress()
    {
        // Give the input system a beat so the input that ended the previous
        // step doesn't immediately register as "any button".
        if (inputCooldown > 0f)
            yield return new WaitForSecondsRealtime(inputCooldown);

        while (true)
        {
            if (AnyButtonPressedThisFrame())
                break;

            yield return null;
        }

        FireButton();
    }

    /// <summary>
    /// Selects the button and (optionally) invokes its onClick.
    /// </summary>
    public void FireButton()
    {
        if (buttonToSelect == null)
            return;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(buttonToSelect.gameObject);

        if (pressButtonOnAnyInput)
            buttonToSelect.onClick.Invoke();
    }

    private bool AnyButtonPressedThisFrame()
    {
        // --- Keyboard ---
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            return true;

        // --- Mouse ---
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame) return true;
            if (Mouse.current.rightButton.wasPressedThisFrame) return true;
            if (Mouse.current.middleButton.wasPressedThisFrame) return true;
        }

        // --- Gamepad ---
        Gamepad gp = Gamepad.current;
        if (gp != null)
        {
            if (gp.buttonSouth.wasPressedThisFrame) return true;
            if (gp.buttonNorth.wasPressedThisFrame) return true;
            if (gp.buttonEast.wasPressedThisFrame) return true;
            if (gp.buttonWest.wasPressedThisFrame) return true;
            if (gp.startButton.wasPressedThisFrame) return true;
            if (gp.selectButton.wasPressedThisFrame) return true;
            if (gp.leftShoulder.wasPressedThisFrame) return true;
            if (gp.rightShoulder.wasPressedThisFrame) return true;
        }

        return false;
    }

    // ------------------------------------------------------------------
    // DIALOGUE — PUBLIC ENTRY
    // ------------------------------------------------------------------

    public void ShowDialogue(DialogueData data)
    {
        if (data == null) return;

        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        if (dialogueClipSource != null)
            dialogueClipSource.Stop();

        dialogueGroup.gameObject.SetActive(true);

        dialogueTMP.text = data.dialogueText ?? string.Empty;
        dialogueTMP.maxVisibleCharacters = 0;

        dialogueActive = true;

        StartCoroutine(FadeCanvasGroup(dialogueGroup, 0f, 1f, fadeDuration));
        typingRoutine = StartCoroutine(RevealText(data));
    }

    // ------------------------------------------------------------------
    // DIALOGUE — TEXT REVEAL
    // ------------------------------------------------------------------

    private IEnumerator RevealText(DialogueData data)
    {
        dialogueTMP.maxVisibleCharacters = 0;

        dialogueTMP.ForceMeshUpdate();
        int totalChars = dialogueTMP.textInfo.characterCount;

        AudioClip clip = data.DialougeClip;

        if (clip != null && dialogueClipSource != null)
        {
            dialogueClipSource.Stop();
            dialogueClipSource.clip = clip;
            dialogueClipSource.loop = false;
            dialogueClipSource.pitch = 1f;
            dialogueClipSource.Play();
        }

        float typeSpeed = data.textSpeed;
        if (matchTextSpeedToAudio && clip != null && clip.length > 0f && totalChars > 0)
        {
            typeSpeed = clip.length / totalChars;
        }

        for (int i = 0; i < totalChars; i++)
        {
            dialogueTMP.maxVisibleCharacters = i + 1;

            if (typeSpeed > 0f)
                yield return new WaitForSeconds(typeSpeed);
            else
                yield return null;
        }

        if (clip != null && dialogueClipSource != null)
        {
            float elapsed = 0f;
            while (dialogueClipSource.isPlaying && elapsed < clip.length + 1f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        typingRoutine = null;

        if (data.autoHideTime > 0f)
            yield return new WaitForSeconds(data.autoHideTime);

        yield return StartCoroutine(FadeCanvasGroup(dialogueGroup, 1f, 0f, fadeDuration));
        dialogueGroup.gameObject.SetActive(false);

        dialogueActive = false;
        OnDialogueFinished?.Invoke();
    }

    // ------------------------------------------------------------------
    // DIALOGUE — AUDIO ONLY
    // ------------------------------------------------------------------

    private IEnumerator PlayAudioOnlyDialogue(DialogueData data)
    {
        if (data == null || dialogueClipSource == null)
            yield break;

        AudioClip clip = data.DialougeClip;
        if (clip == null)
            yield break;

        dialogueClipSource.Stop();
        dialogueClipSource.clip = clip;
        dialogueClipSource.loop = false;
        dialogueClipSource.pitch = 1f;
        dialogueClipSource.Play();

        float elapsed = 0f;
        while (dialogueClipSource.isPlaying && elapsed < clip.length + 1f)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    // ------------------------------------------------------------------
    // MUSIC
    // ------------------------------------------------------------------

    private void StartLoopingMusic()
    {
        if (musicSource == null || musicClip == null)
            return;

        musicSource.Stop();
        musicSource.clip = musicClip;
        musicSource.loop = true;
        musicSource.Play();
    }

    // ------------------------------------------------------------------
    // CAMERA MOVE (Y only)
    // ------------------------------------------------------------------

    private IEnumerator MoveCameraY(Transform cam, float targetY, float duration)
    {
        if (duration <= 0f)
        {
            Vector3 p = cam.position;
            p.y = targetY;
            cam.position = p;
            yield break;
        }

        float startY = cam.position.y;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = cameraMoveCurve != null ? cameraMoveCurve.Evaluate(t) : t;

            Vector3 p = cam.position;
            p.y = Mathf.Lerp(startY, targetY, eased);
            cam.position = p;

            yield return null;
        }

        Vector3 final = cam.position;
        final.y = targetY;
        cam.position = final;
    }

    // ------------------------------------------------------------------
    // UI FADE HELPER
    // ------------------------------------------------------------------

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null) yield break;

        if (duration <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            group.alpha = Mathf.Lerp(from, to, t / duration);
            t += Time.deltaTime;
            yield return null;
        }

        group.alpha = to;
    }
}