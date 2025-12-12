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

    private DialogueData[] currentDialogueLines;
    private int currentLineIndex = 0;
    private bool isTyping = false;
    private bool showTriangle = false;
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

        currentDialogueLines = lines;
        currentLineIndex = 0;

        // Freeze gameplay
        if (player != null)
            player.SetActive(false);

        // Show darkened background
        if (backgroundOverlay != null)
            backgroundOverlay.gameObject.SetActive(true);

        dialogueGroup.gameObject.SetActive(true);
        StartCoroutine(FadeCanvasGroup(0f, 1f, fadeDuration));

        ShowCurrentLine();
    }

    private void Update()
    {
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

        if (nextLineTriangle != null)
            nextLineTriangle.gameObject.SetActive(false);

        foreach (char c in text)
        {
            dialogueTMP.text += c;
            yield return new WaitForSeconds(textSpeed);
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

    private IEnumerator FadeCanvasGroup(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            dialogueGroup.alpha = Mathf.Lerp(from, to, t / duration);
            t += Time.unscaledDeltaTime; // Use unscaled delta to ignore game pause
            yield return null;
        }
        dialogueGroup.alpha = to;
    }
}
