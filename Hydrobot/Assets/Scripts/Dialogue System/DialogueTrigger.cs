using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DialogueTrigger2D : MonoBehaviour
{
    [Header("References")]
    public DialogueSystem dialogueSystem; // can be auto-found
    public DialogueData[] dialogueSequence;

    [Header("Options")]
    public bool triggerOnce = true;
    public string playerTag = "Player"; // tag to detect

    // runtime
    private bool hasTriggered = false;
    private int currentIndex = 0;

    private void Awake()
    {
        // Ensure collider is a trigger and warn if not
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
            Debug.LogWarning($"[DialogueTrigger2D] Collider on '{name}' is not set to IsTrigger. Set it or triggers won't fire.");

        // Try auto-find DialogueSystem if not assigned
        if (dialogueSystem == null)
        {
            dialogueSystem = FindObjectOfType<DialogueSystem>();
            if (dialogueSystem == null)
                Debug.LogWarning($"[DialogueTrigger2D] No DialogueSystem found in scene. Assign one in the inspector on '{name}'.");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered && triggerOnce) return;

        if (IsPlayerCollider(other))
        {
            TryStartDialogue();
        }
    }

    // Optional: allow manual triggering from code
    public void TriggerNow()
    {
        if (hasTriggered && triggerOnce) return;
        TryStartDialogue();
    }

    private bool IsPlayerCollider(Collider2D other)
    {
        if (other == null) return false;

        // Check tag first
        if (!string.IsNullOrEmpty(playerTag) && other.CompareTag(playerTag))
            return true;

        // fallback: check for any common player component (safe non-specific check)
        // Replace "Player" below with your project's player component type if you have one.
        var playerComponent = other.GetComponentInParent<Transform>(); // always true, keep for structure
        // If you have a Player script, check for it instead:
        // if (other.GetComponent<Player>() != null) return true;

        return false;
    }

    private void TryStartDialogue()
    {
        if (dialogueSystem == null)
        {
            Debug.LogError($"[DialogueTrigger2D] No DialogueSystem assigned or found for '{name}'. Cannot start dialogue.");
            return;
        }

        if (dialogueSequence == null || dialogueSequence.Length == 0)
        {
            Debug.LogWarning($"[DialogueTrigger2D] No DialogueData assigned on '{name}'.");
            return;
        }

        // Play current dialogue in sequence
        DialogueData data = dialogueSequence[Mathf.Clamp(currentIndex, 0, dialogueSequence.Length - 1)];
        dialogueSystem.ShowDialogue(data);

        currentIndex++;

        if (currentIndex >= dialogueSequence.Length && triggerOnce)
            hasTriggered = true;
    }

#if UNITY_EDITOR
    // editor helper to show state while editing
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.15f);
        Collider2D col = GetComponent<Collider2D>();
        if (col is BoxCollider2D box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.offset, box.size);
        }
        else if (col is CircleCollider2D cir)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawSphere(cir.offset, cir.radius);
        }
        // other collider types will still show the transform position
    }
#endif
}
