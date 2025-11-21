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

        dialogueGroup.gameObject.SetActive(true);

        // Set UI fields
        nameTMP.text = data.characterName;
        characterIconImage.sprite = data.characterIcon;
        dialogueTMP.text = "";

        StartCoroutine(FadeCanvasGroup(0f, 1f, fadeDuration));

        typingRoutine = StartCoroutine(TypeText(data));
    }

    private IEnumerator TypeText(DialogueData data)
    {
        dialogueTMP.text = "";
        foreach (char c in data.dialogueText)
        {
            dialogueTMP.text += c;
            yield return new WaitForSeconds(data.textSpeed);
        }

        // Start auto-hide countdown
        hideRoutine = StartCoroutine(AutoHideTimer(data.autoHideTime));
    }

    private IEnumerator AutoHideTimer(float time)
    {
        float timer = 0f;

        while (timer < time)
        {
            timer += Time.deltaTime;
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
