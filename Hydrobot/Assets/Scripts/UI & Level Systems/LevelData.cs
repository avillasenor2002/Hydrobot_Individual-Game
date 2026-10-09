using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Level Select/Level Data", order = 0)]
public class LevelData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Display name of the level. Must be unique - used as the save key.")]
    public string levelName = "New Level";

    [Tooltip("Name of the objective shown to the player (e.g. 'Save the village').")]
    public string objectiveName = "";
    public string objectiveNum = "";

    [Header("Locking")]
    [Tooltip("If true, the level select button is locked: not navigable, children hidden, locked sprite shown.")]
    public bool locked;

    [Header("Progress")]
    [Tooltip("Set at runtime by LevelProgressTracker. Not saved to the asset itself.")]
    public bool isComplete;

    [Header("Event")]
    [Tooltip("Set at runtime by LevelProgressTracker. Not saved to the asset itself.")]
    public bool isEvent;

    [Tooltip("Fastest completion time in seconds. 0 means no time recorded yet.")]
    public float bestTime;

    [Range(0f, 100f)]
    [Tooltip("Highest percentage of water remaining at the end of the level. 0 means none recorded yet.")]
    public float bestWater;

    /// <summary>True if the player has any recorded progress on this level.</summary>
    public bool HasBeenPlayed => isComplete || bestTime > 0f || bestWater > 0f;

    /// <summary>Wipes all runtime progress on this level.</summary>
    public void ResetProgress()
    {
        isComplete = false;
        bestTime = 0f;
        bestWater = 0f;
    }

    /// <summary>Records a completion time, keeping only the fastest.</summary>
    public void ReportTime(float timeInSeconds)
    {
        if (timeInSeconds <= 0f)
            return;

        if (bestTime <= 0f || timeInSeconds < bestTime)
            bestTime = timeInSeconds;
    }

    /// <summary>Records a water percentage, keeping only the highest.</summary>
    public void ReportWater(float waterPercent)
    {
        if (waterPercent <= 0f)
            return;

        if (waterPercent > bestWater)
            bestWater = Mathf.Clamp(waterPercent, 0f, 100f);
    }
}