using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DialogueTrigger2D : MonoBehaviour
{
    [Header("References")]
    public DialogueSystem dialogueSystem;
    public DialogueData[] dialogueSequence;

    [Header("Options")]
    public bool triggerOnce = true;
    public bool playSequentially = true;
    public string playerTag = "Player";

    // Shared active trigger
    private static DialogueTrigger2D activeTrigger = null;

    // Local state
    private int currentIndex = 0;
    private bool hasTriggered = false;
    private bool listening = false;

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
            Debug.LogWarning($"[DialogueTrigger2D] Collider on '{name}' is not set to IsTrigger!");

        if (dialogueSystem == null)
        {
            dialogueSystem = FindObjectOfType<DialogueSystem>();
            if (dialogueSystem == null)
                Debug.LogWarning($"[DialogueTrigger2D] No DialogueSystem found in scene.");
        }
    }

    private void OnEnable()
    {
        if (dialogueSystem != null)
        {
            dialogueSystem.OnDialogueFinished += HandleDialogueFinished;
            listening = true;
        }
    }

    private void OnDisable()
    {
        if (dialogueSystem != null && listening)
            dialogueSystem.OnDialogueFinished -= HandleDialogueFinished;

        if (activeTrigger == this)
            activeTrigger = null;

        listening = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        // If already active, DO NOT restart
        if (activeTrigger == this)
            return;

        // If already completed and one-shot, DO NOT restart
        if (hasTriggered && triggerOnce)
            return;

        StartSequenceFromThisTrigger();
    }

    public void TriggerNow()
    {
        if (activeTrigger == this) return;
        if (hasTriggered && triggerOnce) return;

        StartSequenceFromThisTrigger();
    }

    private void StartSequenceFromThisTrigger()
    {
        if (dialogueSystem == null || dialogueSequence == null || dialogueSequence.Length == 0)
            return;

        // Mark immediately so it can NEVER refire during playback
        if (triggerOnce)
            hasTriggered = true;

        // Override any previous trigger
        activeTrigger = this;

        currentIndex = 0;
        PlayLine(currentIndex);
    }

    private void PlayLine(int index)
    {
        index = Mathf.Clamp(index, 0, dialogueSequence.Length - 1);
        dialogueSystem.ShowDialogue(dialogueSequence[index]);
    }

    private void HandleDialogueFinished()
    {
        // Only respond if we are the current trigger
        if (activeTrigger != this)
            return;

        if (!playSequentially)
        {
            FinishTrigger();
            return;
        }

        currentIndex++;

        if (currentIndex >= dialogueSequence.Length)
        {
            FinishTrigger();
            return;
        }

        PlayLine(currentIndex);
    }

    private void FinishTrigger()
    {
        // Only disable ourselves as active
        if (activeTrigger == this)
            activeTrigger = null;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.15f);

        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;

        Gizmos.matrix = transform.localToWorldMatrix;

        if (col is BoxCollider2D box)
        {
            Gizmos.DrawCube(box.offset, box.size);
        }
        else if (col is CircleCollider2D cir)
        {
            Gizmos.DrawSphere(cir.offset, cir.radius);
        }
    }
}
#endif
