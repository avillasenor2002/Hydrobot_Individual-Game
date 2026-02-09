using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DialogueTrigger2D_Random : MonoBehaviour
{
    [Header("References")]
    public DialogueSystem dialogueSystem;
    public DialogueSystemFreeze dialogueSystemFreeze;
    public DialogueData[] dialogueOptions;

    [Header("Options")]
    public bool triggerOnce = true;
    public string playerTag = "Player";

    // Shared active trigger
    private static DialogueTrigger2D_Random activeTrigger = null;

    private bool hasTriggered = false;
    private bool listening = false;

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
            Debug.LogWarning($"[DialogueTrigger2D_Random] Collider on '{name}' is not set to IsTrigger!");

        if (dialogueSystem == null && dialogueSystemFreeze == null)
        {
            dialogueSystem = FindObjectOfType<DialogueSystem>();
            if (dialogueSystem == null)
            {
                dialogueSystemFreeze = FindObjectOfType<DialogueSystemFreeze>();
                if (dialogueSystemFreeze == null)
                    Debug.LogWarning($"[DialogueTrigger2D_Random] No DialogueSystem or DialogueSystemFreeze found in scene.");
            }
        }
    }

    private void OnEnable()
    {
        if (dialogueSystem != null)
        {
            dialogueSystem.OnDialogueFinished += HandleDialogueFinished;
            listening = true;
        }
        else if (dialogueSystemFreeze != null)
        {
            dialogueSystemFreeze.OnDialogueFinished += HandleDialogueFinished;
            listening = true;
        }
    }

    private void OnDisable()
    {
        if (listening)
        {
            if (dialogueSystem != null)
                dialogueSystem.OnDialogueFinished -= HandleDialogueFinished;
            if (dialogueSystemFreeze != null)
                dialogueSystemFreeze.OnDialogueFinished -= HandleDialogueFinished;
        }

        if (activeTrigger == this)
            activeTrigger = null;

        listening = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        TryTrigger();
    }

    public void TriggerNow()
    {
        TryTrigger();
    }

    private void TryTrigger()
    {
        if (activeTrigger == this) return;
        if (hasTriggered && triggerOnce) return;
        if (dialogueOptions == null || dialogueOptions.Length == 0) return;

        activeTrigger = this;
        hasTriggered = true;

        DialogueData randomLine =
            dialogueOptions[Random.Range(0, dialogueOptions.Length)];

        PlayDialogue(randomLine);
    }

    private void PlayDialogue(DialogueData data)
    {
        if (dialogueSystem != null)
        {
            dialogueSystem.ShowDialogue(data);
        }
        else if (dialogueSystemFreeze != null)
        {
            dialogueSystemFreeze.StartDialogue(new DialogueData[] { data });
        }
    }

    private void HandleDialogueFinished()
    {
        if (activeTrigger == this)
        {
            activeTrigger = null;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.6f, 0.2f, 1f, 0.15f);

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
#endif
}
