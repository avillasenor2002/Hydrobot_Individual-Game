using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Runs at the start of every scene. Walks a list of gate entries and, for
/// the first entry whose LevelData is UNLOCKED and has isEvent == true:
///   - Sets isEvent to false (consuming the event).
///   - Loads the gate's scene.
///
/// Place one of these in every scene that needs routing. Each instance
/// runs its checks in the scene it lives in.
///
/// A fallback scene can be set for the case where no gate matches.
/// </summary>
public class LevelGateSceneLoader : MonoBehaviour
{
    /// <summary>
    /// One gate: "if this level is unlocked and its isEvent flag is true,
    /// consume the event and load this scene."
    /// </summary>
    [System.Serializable]
    public class GateEntry
    {
        [Tooltip("The LevelData asset to examine.")]
        public LevelData levelToCheck;

        [Tooltip("Scene loaded when this gate fires.")]
        public string sceneToLoad;

        [Tooltip("Uncheck to skip this entry without deleting it.")]
        public bool enabled = true;
    }

    [Header("Gates")]
    [Tooltip("Evaluated top to bottom. The first matching entry fires and the rest are ignored.")]
    [SerializeField] private List<GateEntry> gates = new List<GateEntry>();

    [Header("Fallback")]
    [Tooltip("Scene loaded if no gate matches. Leave empty to stay in the current scene.")]
    [SerializeField] private string fallbackSceneName = "";

    [Header("Timing")]
    [Tooltip("Seconds to wait before running the checks. 0 runs immediately on Start.")]
    [SerializeField] private float delayBeforeCheck = 0f;

    [Tooltip("If true, the checks run automatically in Start.")]
    [SerializeField] private bool runOnStart = true;

    [Header("Options")]
    [Tooltip("Log which gate fired (or that the fallback was used).")]
    [SerializeField] private bool logResult = false;

    // ------------------------------------------------------------------
    // Runtime state
    // ------------------------------------------------------------------
    private bool hasFired;

    /// <summary>True once this scene's loader has run its checks.</summary>
    public bool HasFired => hasFired;

    // ------------------------------------------------------------------

    private void Start()
    {
        if (runOnStart)
            RunChecks();
    }

    // ------------------------------------------------------------------
    // PUBLIC API
    // ------------------------------------------------------------------

    /// <summary>
    /// Evaluates every gate and fires the first one that matches. Safe to
    /// call multiple times — only the first call has any effect per scene.
    /// </summary>
    public void RunChecks()
    {
        if (hasFired)
            return;

        hasFired = true;

        if (delayBeforeCheck > 0f)
            StartCoroutine(DelayedLoad());
        else
            EvaluateAndLoad();
    }

    /// <summary>Manually resets the flag so the checks can run again this scene.</summary>
    public void ResetFiredFlag()
    {
        hasFired = false;
    }

    // ------------------------------------------------------------------
    // INTERNALS
    // ------------------------------------------------------------------

    private IEnumerator DelayedLoad()
    {
        yield return new WaitForSecondsRealtime(delayBeforeCheck);
        EvaluateAndLoad();
    }

    private void EvaluateAndLoad()
    {
        for (int i = 0; i < gates.Count; i++)
        {
            GateEntry entry = gates[i];

            if (entry == null || !entry.enabled)
                continue;

            if (entry.levelToCheck == null)
            {
                Debug.LogWarning(
                    $"[LevelGateSceneLoader] Gate {i} has no LevelData assigned — skipping.", this);
                continue;
            }

            // --- Condition: unlocked AND isEvent ---
            bool unlocked = !entry.levelToCheck.locked;
            bool isEvent = entry.levelToCheck.isEvent;

            if (!unlocked || !isEvent)
                continue;

            // --- Validate the target scene ---
            if (string.IsNullOrEmpty(entry.sceneToLoad))
            {
                Debug.LogError(
                    $"[LevelGateSceneLoader] Gate {i} matched (level '{entry.levelToCheck.levelName}' " +
                    $"is unlocked and isEvent is true) but sceneToLoad is empty. " +
                    $"Fix this in the Inspector — no transition can happen.", this);
                continue;
            }

            // --- Consume the event BEFORE loading so it can't double-fire ---
            entry.levelToCheck.isEvent = false;

            if (logResult)
            {
                Debug.Log(
                    $"[LevelGateSceneLoader] Gate {i} fired — level " +
                    $"'{entry.levelToCheck.levelName}' is unlocked and isEvent was true. " +
                    $"isEvent set to false. Loading '{entry.sceneToLoad}'.", this);
            }

            // --- Load the scene. This is the transition. ---
            SceneManager.LoadScene(entry.sceneToLoad);
            return;
        }

        // --- No gate matched — fall back if one is set ---
        if (string.IsNullOrEmpty(fallbackSceneName))
        {
            if (logResult)
            {
                Debug.Log(
                    "[LevelGateSceneLoader] No gate matched and no fallback scene is set — staying put.",
                    this);
            }
            return;
        }

        if (logResult)
        {
            Debug.Log(
                $"[LevelGateSceneLoader] No gate matched — loading fallback '{fallbackSceneName}'.",
                this);
        }

        SceneManager.LoadScene(fallbackSceneName);
    }
}