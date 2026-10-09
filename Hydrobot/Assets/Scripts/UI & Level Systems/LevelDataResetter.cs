using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Wired to a title-screen button. When invoked, it:
///   1. Sets isComplete = false on every LevelData in the "clearCompleteGroup".
///   2. Sets locked = true on every LevelData in the "lockGroup".
///   3. Sets isEvent = true on every LevelData in the "eventResetGroup".
/// Handy for a "New Game" button that resets progress.
/// </summary>
public class LevelDataResetter : MonoBehaviour
{
    [Header("Clear isComplete")]
    [Tooltip("Every LevelData in this list has isComplete set to false when the button fires.")]
    [SerializeField] private List<LevelData> clearCompleteGroup = new List<LevelData>();

    [Tooltip("Also reset bestTime and bestWater on these assets.")]
    [SerializeField] private bool alsoResetStats = false;

    [Header("Set locked = true")]
    [Tooltip("Every LevelData in this list has locked set to true when the button fires.")]
    [SerializeField] private List<LevelData> lockGroup = new List<LevelData>();

    [Header("Set isEvent = true")]
    [Tooltip("Every LevelData in this list has isEvent set to true when the button fires. " +
             "Use this to re-arm event gates for a fresh playthrough.")]
    [SerializeField] private List<LevelData> eventResetGroup = new List<LevelData>();

    [Header("Options")]
    [Tooltip("Log a line to the console for each asset that was changed.")]
    [SerializeField] private bool logChanges = false;

    /// <summary>
    /// Wire this to a UI Button's OnClick. Clears completion on one group,
    /// locks another, and re-arms events on a third. Safe to call multiple times.
    /// </summary>
    public void ApplyReset()
    {
        // --- Group 1: clear isComplete ---
        for (int i = 0; i < clearCompleteGroup.Count; i++)
        {
            LevelData level = clearCompleteGroup[i];
            if (level == null) continue;

            level.isComplete = false;

            if (alsoResetStats)
            {
                level.bestTime = 0f;
                level.bestWater = 0f;
            }

            if (logChanges)
                Debug.Log($"[LevelDataResetter] Cleared isComplete on '{level.levelName}'.", level);
        }

        // --- Group 2: set locked = true ---
        for (int i = 0; i < lockGroup.Count; i++)
        {
            LevelData level = lockGroup[i];
            if (level == null) continue;

            level.locked = true;

            if (logChanges)
                Debug.Log($"[LevelDataResetter] Locked '{level.levelName}'.", level);
        }

        // --- Group 3: set isEvent = true ---
        for (int i = 0; i < eventResetGroup.Count; i++)
        {
            LevelData level = eventResetGroup[i];
            if (level == null) continue;

            level.isEvent = true;

            if (logChanges)
                Debug.Log($"[LevelDataResetter] Re-armed isEvent on '{level.levelName}'.", level);
        }
    }
}