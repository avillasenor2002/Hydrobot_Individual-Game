using UnityEngine;

/// <summary>
/// Goes on a GameObject with a Collider2D set as a trigger. Broadcasts an
/// event when the player enters it. A SpawnGrowEffect subscribes to this
/// event via its own trigger list, so nothing needs to be wired by hand.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SpawnTriggerZone : MonoBehaviour
{
    /// <summary>
    /// Fired once when the player enters this zone.
    /// The argument is this component, so listeners can identify the source.
    /// </summary>
    public event System.Action<SpawnTriggerZone> OnPlayerEntered;

    [Header("Detection")]
    [Tooltip("Tag used to identify the player.")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("If true, only objects with the player tag can trigger this zone.")]
    [SerializeField] private bool playerOnly = true;

    [Header("Cleanup")]
    [Tooltip("If true, this trigger destroys itself the moment it fires. " +
             "Usually left off — the SpawnGrowEffect handles cleanup.")]
    [SerializeField] private bool destroySelfOnTrigger = false;

    private bool hasTriggered;

    private void Reset()
    {
        // Force the collider on this object to be a trigger when the component is added.
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered)
            return;

        if (playerOnly && !other.CompareTag(playerTag))
            return;

        hasTriggered = true;

        // Notify the SpawnGrowEffect (and any other listeners).
        OnPlayerEntered?.Invoke(this);

        if (destroySelfOnTrigger)
            Destroy(gameObject);
    }
}