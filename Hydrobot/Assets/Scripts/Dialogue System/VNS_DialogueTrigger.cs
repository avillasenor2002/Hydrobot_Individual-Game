using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Feeds a dialogue sequence into DialogueSystemFreezeSwap.
///
/// The trigger converts its per-line entries into SpeakerLine[] (resolving
/// the "swap toggle" and "explicit speaker" options into an explicit
/// speaker index per line) and hands the whole batch to the freeze swap
/// system in one call. The freeze swap owns all portrait visuals, typing,
/// and line advancement.
///
/// When the sequence finishes, the trigger calls the assigned
/// LevelSelectTransition to play its reverse cutout and load a scene
/// (the main menu).
///
/// Fires either on a delay when the scene starts, or when another script
/// calls TriggerNow().
/// </summary>
public class VNS_DialogueTrigger : MonoBehaviour
{
    [System.Serializable]
    public class DialogueLineEntry
    {
        [Tooltip("The line to play.")]
        public DialogueData dialogue;

        [Tooltip("If true, the active speaker swaps to the other one before this line plays. " +
                 "Ignored when useExplicitSpeaker is on.")]
        public bool swapSpeakerOnThisLine = false;

        [Tooltip("If true, this line uses the explicitSpeakerIndex below instead of the swap toggle.")]
        public bool useExplicitSpeaker = false;

        [Tooltip("Which portrait is the active speaker on this line. 0 = A, 1 = B.")]
        [Range(0, 1)]
        public int explicitSpeakerIndex = 0;
    }

    [Header("References")]
    [Tooltip("The DialogueSystemFreezeSwap that will play the sequence.")]
    public DialogueSystemFreezeSwap dialogueSystemSwap;

    [Header("Dialogue")]
    public DialogueLineEntry[] dialogueSequence;

    [Header("Options")]
    [Tooltip("Which portrait is active before the first line. 0 = A, 1 = B.")]
    [Range(0, 1)]
    public int startingSpeakerIndex = 0;

    [Tooltip("If true, the sequence can only fire once per scene load.")]
    public bool triggerOnce = true;

    [Header("Scene-Start Trigger")]
    [Tooltip("If true, the sequence fires automatically after startDelay seconds.")]
    public bool triggerOnStart = false;

    [Tooltip("Delay before the sequence fires when triggerOnStart is true.")]
    public float startDelay = 1f;

    [Header("Transition On Complete")]
    [Tooltip("The cutout transition to fire once dialogue finishes.")]
    public LevelSelectTransition levelSelectTransition;

    [Tooltip("Scene to load when the transition fires. Usually the main menu.")]
    public string mainMenuSceneName = "MainMenu";

    [Tooltip("Delay (unscaled seconds) after the last line before the transition fires.")]
    public float transitionDelay = 0.5f;

    [Tooltip("If false, the transition is skipped entirely when dialogue finishes.")]
    public bool transitionOnComplete = true;

    [Header("Debug")]
    [Tooltip("Log when the sequence starts and finishes.")]
    public bool debugLogging = false;

    // ------------------------------------------------------------------
    // Shared active trigger
    // ------------------------------------------------------------------
    private static VNS_DialogueTrigger activeTrigger = null;

    // Sequence state
    private bool hasTriggered = false;
    private bool listening = false;
    private bool sequenceActive = false;

    // ------------------------------------------------------------------
    // Lifecycle
    // ------------------------------------------------------------------

    private void Awake()
    {
        if (dialogueSystemSwap == null)
        {
            dialogueSystemSwap = FindObjectOfType<DialogueSystemFreezeSwap>();
            if (dialogueSystemSwap == null)
                Debug.LogWarning("[VNS_DialogueTrigger] No DialogueSystemFreezeSwap found in scene.", this);
        }

        if (levelSelectTransition == null)
        {
            levelSelectTransition = FindObjectOfType<LevelSelectTransition>();
            if (levelSelectTransition == null && transitionOnComplete)
                Debug.LogWarning("[VNS_DialogueTrigger] No LevelSelectTransition found in scene. " +
                                 "Transition on complete will be skipped.", this);
        }
    }

    private void OnEnable()
    {
        if (dialogueSystemSwap != null)
        {
            dialogueSystemSwap.OnDialogueFinished += HandleDialogueFinished;
            listening = true;
        }
    }

    private void OnDisable()
    {
        if (listening && dialogueSystemSwap != null)
            dialogueSystemSwap.OnDialogueFinished -= HandleDialogueFinished;

        if (activeTrigger == this)
            activeTrigger = null;

        listening = false;
    }

    private void Start()
    {
        if (triggerOnStart)
            StartCoroutine(StartDelayed());
    }

    private IEnumerator StartDelayed()
    {
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        TriggerNow();
    }

    // ------------------------------------------------------------------
    // PUBLIC API
    // ------------------------------------------------------------------

    /// <summary>Fires the sequence. Safe to call multiple times.</summary>
    public void TriggerNow()
    {
        if (activeTrigger == this) return;
        if (hasTriggered && triggerOnce) return;

        StartSequenceFromThisTrigger();
    }

    /// <summary>Re-arms the trigger.</summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
    }

    // ------------------------------------------------------------------
    // SEQUENCE
    // ------------------------------------------------------------------

    private void StartSequenceFromThisTrigger()
    {
        if (dialogueSystemSwap == null || dialogueSequence == null || dialogueSequence.Length == 0)
            return;

        if (triggerOnce)
            hasTriggered = true;

        activeTrigger = this;
        sequenceActive = true;

        DialogueSystemFreezeSwap.SpeakerLine[] lines = BuildSpeakerLines();

        if (lines == null || lines.Length == 0)
        {
            FinishTrigger();
            return;
        }

        dialogueSystemSwap.StartDialogue(lines);

        if (debugLogging)
            Debug.Log($"[VNS_DialogueTrigger] Sequence started with {lines.Length} lines.", this);
    }

    /// <summary>
    /// Walks the per-line entries and converts them into SpeakerLine[] with
    /// an explicit speaker index on each line. The toggle-flip behavior is
    /// resolved here so the freeze swap just reads a straight index.
    /// </summary>
    private DialogueSystemFreezeSwap.SpeakerLine[] BuildSpeakerLines()
    {
        List<DialogueSystemFreezeSwap.SpeakerLine> result =
            new List<DialogueSystemFreezeSwap.SpeakerLine>();

        int currentSpeaker = Mathf.Clamp(startingSpeakerIndex, 0, 1);

        for (int i = 0; i < dialogueSequence.Length; i++)
        {
            DialogueLineEntry entry = dialogueSequence[i];
            if (entry == null || entry.dialogue == null)
                continue;

            if (entry.useExplicitSpeaker)
                currentSpeaker = Mathf.Clamp(entry.explicitSpeakerIndex, 0, 1);
            else if (entry.swapSpeakerOnThisLine)
                currentSpeaker = 1 - currentSpeaker;

            result.Add(new DialogueSystemFreezeSwap.SpeakerLine
            {
                dialogue = entry.dialogue,
                speakerIndex = currentSpeaker
            });
        }

        return result.ToArray();
    }

    private void HandleDialogueFinished()
    {
        if (!sequenceActive) return;

        FinishTrigger();
    }

    private void FinishTrigger()
    {
        sequenceActive = false;

        if (activeTrigger == this)
            activeTrigger = null;

        if (debugLogging)
            Debug.Log("[VNS_DialogueTrigger] Sequence finished.", this);

        // Kick off the cutout transition back to the main menu.
        if (transitionOnComplete)
            StartCoroutine(TransitionToMainMenu());
    }

    // ------------------------------------------------------------------
    // TRANSITION
    // ------------------------------------------------------------------

    private IEnumerator TransitionToMainMenu()
    {
        if (transitionDelay > 0f)
            yield return new WaitForSecondsRealtime(transitionDelay);

        if (levelSelectTransition == null)
        {
            if (debugLogging)
                Debug.Log("[VNS_DialogueTrigger] No LevelSelectTransition assigned — " +
                          "skipping transition.", this);
            yield break;
        }

        if (debugLogging)
            Debug.Log($"[VNS_DialogueTrigger] Firing cutout transition to '{mainMenuSceneName}'.", this);

        // PlayReverseAndLoadScene shrinks the cutout and loads the scene by name.
        levelSelectTransition.PlayReverseAndLoadScene(mainMenuSceneName);
    }
}