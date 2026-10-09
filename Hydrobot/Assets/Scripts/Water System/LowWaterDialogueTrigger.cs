using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Watches a WaterTank and fires a dialogue sequence the first time the
/// player's water drops below a threshold percentage. Also plays random
/// dialogue excerpts at random intervals after an initial delay.
/// Uses only DialogueSystem.
/// </summary>
public class LowWaterDialogueTrigger : MonoBehaviour
{
    /// <summary>
    /// A pool of dialogue lines treated as one "excerpt". One excerpt is
    /// picked at random each time the scheduler fires.
    /// </summary>
    [System.Serializable]
    public class RandomDialogueExcerpt
    {
        [Tooltip("Lines that make up this excerpt. Played in order.")]
        public DialogueData[] lines;
    }

    [Header("References")]
    [Tooltip("The water tank component to monitor.")]
    [SerializeField] private WaterTank waterTank;

    [Tooltip("DialogueSystem to use. Leave empty to auto-find in scene.")]
    [SerializeField] private DialogueSystem dialogueSystem;

    [Header("Water Dialogue")]
    [Tooltip("Lines played the first time water drops below the threshold.")]
    [SerializeField] private DialogueData[] dialogueSequence;

    [Header("Water Threshold")]
    [Range(0f, 1f)]
    [Tooltip("Water percentage (0-1) that fires the dialogue when crossed downward. 0.5 = 50%.")]
    [SerializeField] private float waterThreshold = 0.5f;

    [Tooltip("If true, the water dialogue only ever plays once per scene load.")]
    [SerializeField] private bool triggerOnce = true;

    [Tooltip("If true, the trigger waits until the water has started above the threshold " +
             "before it can fire. Prevents firing immediately on a level that starts low.")]
    [SerializeField] private bool requireAboveThresholdFirst = true;

    [Tooltip("Delay before the water dialogue starts, in seconds.")]
    [SerializeField] private float startDelay = 0f;

    [Header("Random Dialogue")]
    [Tooltip("If true, random excerpts are played at random intervals.")]
    [SerializeField] private bool enableRandomDialogue = true;

    [Tooltip("Seconds to wait after the scene starts before the first excerpt.")]
    [SerializeField] private float initialDelay = 15f;

    [Tooltip("Minimum seconds between random excerpts.")]
    [SerializeField] private float minInterval = 10f;

    [Tooltip("Maximum seconds between random excerpts.")]
    [SerializeField] private float maxInterval = 25f;

    [Tooltip("Pool of excerpts. One is picked at random each time.")]
    [SerializeField] private List<RandomDialogueExcerpt> randomExcerpts = new List<RandomDialogueExcerpt>();

    // ------------------------------------------------------------------
    // Dialogue shared state
    // ------------------------------------------------------------------
    private int dialogueIndex;
    private bool listeningForDialogue;
    private bool dialogueInProgress;
    private DialogueData[] activeSequence;
    private readonly Queue<DialogueData[]> pendingDialogues = new Queue<DialogueData[]>();

    // ------------------------------------------------------------------
    // Water trigger state
    // ------------------------------------------------------------------
    private bool hasTriggered;
    private bool hasBeenAboveThreshold;
    private bool lastWasAboveThreshold;

    // ------------------------------------------------------------------
    // Random dialogue state
    // ------------------------------------------------------------------
    private Coroutine randomDialogueCoroutine;

    // ------------------------------------------------------------------
    // Lifecycle
    // ------------------------------------------------------------------

    private void Awake()
    {
        if (waterTank == null)
            waterTank = FindObjectOfType<WaterTank>();

        if (waterTank == null)
            Debug.LogWarning("[LowWaterDialogueTrigger] No WaterTank found in scene.", this);

        if (dialogueSystem == null)
            dialogueSystem = FindObjectOfType<DialogueSystem>();

        if (dialogueSystem == null)
            Debug.LogWarning("[LowWaterDialogueTrigger] No DialogueSystem found in scene.", this);

        if (waterTank != null)
        {
            lastWasAboveThreshold = GetWaterPercent() >= waterThreshold;
            hasBeenAboveThreshold = lastWasAboveThreshold;
        }
    }

    private void OnEnable()
    {
        SubscribeToDialogueSystem();

        if (enableRandomDialogue && randomExcerpts != null && randomExcerpts.Count > 0)
            randomDialogueCoroutine = StartCoroutine(RandomDialogueRoutine());
    }

    private void OnDisable()
    {
        UnsubscribeFromDialogueSystem();

        if (randomDialogueCoroutine != null)
        {
            StopCoroutine(randomDialogueCoroutine);
            randomDialogueCoroutine = null;
        }
    }

    private void Update()
    {
        UpdateWaterTrigger();
    }

    // ------------------------------------------------------------------
    // WATER TRIGGER
    // ------------------------------------------------------------------

    private void UpdateWaterTrigger()
    {
        if (waterTank == null)
            return;

        if (hasTriggered && triggerOnce)
            return;

        float percent = GetWaterPercent();
        bool isAbove = percent >= waterThreshold;

        if (isAbove)
            hasBeenAboveThreshold = true;

        bool crossedDown = lastWasAboveThreshold && !isAbove;

        if (crossedDown)
        {
            if (!requireAboveThresholdFirst || hasBeenAboveThreshold)
            {
                if (triggerOnce)
                    hasTriggered = true;

                if (startDelay > 0f)
                    StartCoroutine(DelayedWaterDialogue());
                else
                    PlayDialogueSequence(dialogueSequence);
            }
        }

        lastWasAboveThreshold = isAbove;
    }

    private IEnumerator DelayedWaterDialogue()
    {
        yield return new WaitForSeconds(startDelay);
        PlayDialogueSequence(dialogueSequence);
    }

    /// <summary>
    /// Returns the current water as a 0-1 fraction.
    /// >>> Adjust this if your WaterTank uses different field names. <<<
    /// </summary>
    private float GetWaterPercent()
    {
        if (waterTank == null)
            return 1f;

        if (waterTank.maxWater <= 0f)
            return 0f;

        return Mathf.Clamp01(waterTank.currentWater / waterTank.maxWater);
    }

    // ------------------------------------------------------------------
    // RANDOM DIALOGUE
    // ------------------------------------------------------------------

    private IEnumerator RandomDialogueRoutine()
    {
        // Wait the initial delay before the first excerpt.
        yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            // Wait a random interval between excerpts.
            float wait = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(wait);

            // If another sequence is playing (e.g. the water trigger fired),
            // skip this turn and try again on the next interval.
            if (dialogueInProgress)
                continue;

            RandomDialogueExcerpt excerpt = PickRandomExcerpt();

            if (excerpt == null || excerpt.lines == null || excerpt.lines.Length == 0)
                continue;

            PlayDialogueSequence(excerpt.lines);

            // Wait for it to finish before scheduling the next one.
            while (dialogueInProgress)
                yield return null;
        }
    }

    private RandomDialogueExcerpt PickRandomExcerpt()
    {
        if (randomExcerpts == null || randomExcerpts.Count == 0)
            return null;

        return randomExcerpts[Random.Range(0, randomExcerpts.Count)];
    }

    // ------------------------------------------------------------------
    // DIALOGUE SYSTEM
    // ------------------------------------------------------------------

    private void SubscribeToDialogueSystem()
    {
        if (dialogueSystem != null)
        {
            dialogueSystem.OnDialogueFinished += HandleDialogueFinished;
            listeningForDialogue = true;
        }
    }

    private void UnsubscribeFromDialogueSystem()
    {
        if (listeningForDialogue && dialogueSystem != null)
            dialogueSystem.OnDialogueFinished -= HandleDialogueFinished;

        listeningForDialogue = false;
    }

    /// <summary>
    /// Queues a dialogue sequence. If one is already playing, this waits its
    /// turn instead of stomping or dropping it.
    /// </summary>
    private void PlayDialogueSequence(DialogueData[] sequence)
    {
        if (sequence == null || sequence.Length == 0)
            return;

        if (dialogueSystem == null)
        {
            Debug.LogWarning("[LowWaterDialogueTrigger] No DialogueSystem found.", this);
            return;
        }

        if (dialogueInProgress)
        {
            pendingDialogues.Enqueue(sequence);
            return;
        }

        StartDialogueNow(sequence);
    }

    private void StartDialogueNow(DialogueData[] sequence)
    {
        activeSequence = sequence;
        dialogueIndex = 0;
        dialogueInProgress = true;

        PlayLine(0);
    }

    private void PlayLine(int index)
    {
        if (activeSequence == null || dialogueSystem == null)
            return;

        index = Mathf.Clamp(index, 0, activeSequence.Length - 1);
        dialogueSystem.ShowDialogue(activeSequence[index]);
    }

    private void HandleDialogueFinished()
    {
        if (!dialogueInProgress || activeSequence == null)
            return;

        dialogueIndex++;

        if (dialogueIndex >= activeSequence.Length)
        {
            FinishCurrentDialogue();
            return;
        }

        PlayLine(dialogueIndex);
    }

    private void FinishCurrentDialogue()
    {
        dialogueInProgress = false;
        activeSequence = null;

        if (pendingDialogues.Count > 0)
        {
            DialogueData[] next = pendingDialogues.Dequeue();
            StartDialogueNow(next);
        }
    }

    // ------------------------------------------------------------------
    // PUBLIC API
    // ------------------------------------------------------------------

    /// <summary>Manually resets the water trigger so it can fire again.</summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
        lastWasAboveThreshold = GetWaterPercent() >= waterThreshold;
    }

    /// <summary>Restarts the random dialogue scheduler from the initial delay.</summary>
    public void RestartRandomDialogue()
    {
        if (randomDialogueCoroutine != null)
            StopCoroutine(randomDialogueCoroutine);

        if (enableRandomDialogue && randomExcerpts != null && randomExcerpts.Count > 0)
            randomDialogueCoroutine = StartCoroutine(RandomDialogueRoutine());
    }
}