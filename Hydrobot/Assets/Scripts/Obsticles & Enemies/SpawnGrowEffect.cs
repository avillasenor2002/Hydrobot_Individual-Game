using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Grows an object from zero to a preset scale while a particle effect plays.
/// The growing object is held at the effect's position until the effect
/// finishes. Optionally holds an Enemy invincible for the duration.
/// Changes the music once the effect is over.
/// Fires a dialogue sequence when the grow animation finishes.
/// When everything is done, the GameObject this script is attached to is destroyed.
///
/// The particle effect is held stopped until one of the assigned trigger
/// zones is entered by the player, which then fires the whole sequence.
/// </summary>
public class SpawnGrowEffect : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The particle system that plays during the spawn.")]
    [SerializeField] private ParticleSystem spawnEffect;

    [Tooltip("The object that grows from zero. Leave empty to use targetEnemy's transform.")]
    [SerializeField] private Transform growingObject;

    [Tooltip("Enemy whose isInvincible flag is held true during the animation.")]
    [SerializeField] private Enemy targetEnemy;

    [Header("Trigger Zones")]
    [Tooltip("Trigger zones that fire this effect when the player enters any of them.")]
    [SerializeField] private List<SpawnTriggerZone> triggerZones = new List<SpawnTriggerZone>();

    [Tooltip("If true, all assigned trigger zones are destroyed the moment the effect begins.")]
    [SerializeField] private bool destroyTriggersWhenTriggered = true;

    [Header("Scale")]
    [Tooltip("The scale the object grows into.")]
    [SerializeField] private Vector3 targetScale = Vector3.one;

    [Tooltip("How long the grow animation lasts.")]
    [SerializeField] private float growDuration = 1f;

    [Tooltip("Optional ease curve. Falls back to linear if unset.")]
    [SerializeField] private AnimationCurve growCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Options")]
    [Tooltip("Keep the growing object's position matched to the effect until the effect ends.")]
    [SerializeField] private bool matchEffectPosition = true;

    [Tooltip("Hold the enemy invincible for the duration of the animation.")]
    [SerializeField] private bool makeInvincibleDuringSpawn = true;

    [Tooltip("Destroy the GameObject this script is attached to once the effect is done.")]
    [SerializeField] private bool destroySelfWhenDone = true;

    [Header("Dialogue")]
    [Tooltip("DialogueSystem to use. Leave empty to auto-find in scene.")]
    [SerializeField] private DialogueSystem dialogueSystem;

    [Tooltip("DialogueSystemFreeze to use. Leave empty to auto-find in scene.")]
    [SerializeField] private DialogueSystemFreeze dialogueSystemFreeze;

    [Tooltip("Dialogue lines played when the grow animation finishes.")]
    [SerializeField] private DialogueData[] dialogueSequence;

    [Tooltip("If true, the whole sequence is passed at once so the UI never fades between lines. " +
             "Only applies to DialogueSystemFreeze.")]
    [SerializeField] private bool playSequentially = true;

    [Header("Music")]
    [Tooltip("AudioSource that plays the music. Leave empty to auto-find the one on the 'SFX' object.")]
    [SerializeField] private AudioSource musicSource;

    [Tooltip("Music clip to switch to once the effect finishes.")]
    [SerializeField] private AudioClip postSpawnMusic;

    [Tooltip("If true, the previous clip is restored instead of playing postSpawnMusic.")]
    [SerializeField] private bool restorePreviousMusicOnEnd = false;

    [Tooltip("Match the looping state of the previous music clip.")]
    [SerializeField] private bool matchPreviousLoop = true;

    // Cached state
    private Transform growTarget;
    private bool cachedInvincible;
    private float cachedInvincibilityTimer;

    // Cached music state for optional restore.
    private AudioClip previousClip;
    private bool previousLoop;
    private bool previousWasPlaying;

    // Runtime guards
    private bool effectStarted;
    private bool subscribedToTriggers;

    // Dialogue state (mirrors DialogueTrigger2D)
    private int dialogueIndex;
    private bool listeningForDialogue;

    private void Awake()
    {
        // Resolve the target transform.
        if (growingObject != null)
            growTarget = growingObject;
        else if (targetEnemy != null)
            growTarget = targetEnemy.transform;

        // Start fully collapsed so nothing shows before the trigger fires.
        if (growTarget != null)
            growTarget.localScale = Vector3.zero;

        // Make sure the particle system doesn't auto-play. Clear any
        // particles already emitted so the effect starts clean.
        if (spawnEffect != null)
        {
            spawnEffect.playOnAwake = false;
            spawnEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // Save invincibility state and hold it true during the spawn.
        if (makeInvincibleDuringSpawn && targetEnemy != null)
        {
            cachedInvincible = targetEnemy.isInvincible;
            cachedInvincibilityTimer = targetEnemy.invincibilityTimer;

            targetEnemy.isInvincible = true;
            // Large timer so it can't expire mid-animation. Restored at the end.
            targetEnemy.invincibilityTimer = float.MaxValue;
        }

        // Auto-find the audio source if one wasn't assigned.
        if (musicSource == null)
        {
            GameObject sfxObject = GameObject.Find("SFX");
            if (sfxObject != null)
                musicSource = sfxObject.GetComponent<AudioSource>();
        }

        // Cache the music state up front so we can restore it if needed.
        if (musicSource != null)
        {
            previousClip = musicSource.clip;
            previousLoop = musicSource.loop;
            previousWasPlaying = musicSource.isPlaying;
        }

        // Auto-find dialogue systems if none assigned.
        if (dialogueSystem == null)
            dialogueSystem = FindObjectOfType<DialogueSystem>();

        // Hook into every trigger zone.
        SubscribeToTriggers();
    }

    private void Start()
    {
        // Belt and braces: if for any reason the PS started between Awake
        // and Start, stop it again so it's dark until triggered.
        if (!effectStarted && spawnEffect != null && spawnEffect.isPlaying)
        {
            spawnEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void OnEnable()
    {
        // Listen for dialogue finished events (mirrors DialogueTrigger2D).
        if (dialogueSystem != null)
        {
            dialogueSystem.OnDialogueFinished += HandleDialogueFinished;
            listeningForDialogue = true;
        }
    }

    private void OnDisable()
    {
        if (listeningForDialogue)
        {
            if (dialogueSystem != null)
                dialogueSystem.OnDialogueFinished -= HandleDialogueFinished;
            if (dialogueSystemFreeze != null)
                dialogueSystemFreeze.OnDialogueFinished -= HandleDialogueFinished;
        }

        listeningForDialogue = false;
    }

    private void OnDestroy()
    {
        UnsubscribeFromTriggers();
    }

    // ------------------------------------------------------------------
    // TRIGGERS
    // ------------------------------------------------------------------

    private void SubscribeToTriggers()
    {
        if (subscribedToTriggers)
            return;

        subscribedToTriggers = true;

        for (int i = 0; i < triggerZones.Count; i++)
        {
            SpawnTriggerZone zone = triggerZones[i];
            if (zone == null)
                continue;

            zone.OnPlayerEntered += HandleTriggerEntered;
        }
    }

    private void UnsubscribeFromTriggers()
    {
        if (!subscribedToTriggers)
            return;

        subscribedToTriggers = false;

        for (int i = 0; i < triggerZones.Count; i++)
        {
            SpawnTriggerZone zone = triggerZones[i];
            if (zone == null)
                continue;

            zone.OnPlayerEntered -= HandleTriggerEntered;
        }
    }

    private void HandleTriggerEntered(SpawnTriggerZone zone)
    {
        BeginEffect();
    }

    // ------------------------------------------------------------------
    // EFFECT
    // ------------------------------------------------------------------

    /// <summary>
    /// Fires the spawn effect. Safe to call multiple times — only the
    /// first call has any effect. This is what the trigger zones invoke.
    /// </summary>
    public void BeginEffect()
    {
        if (effectStarted)
            return;

        effectStarted = true;

        // Play the particle effect now that we've been triggered.
        if (spawnEffect != null)
        {
            spawnEffect.Clear(true);
            spawnEffect.Play(true);
        }

        // Stop listening, and optionally clear the trigger zones.
        UnsubscribeFromTriggers();

        if (destroyTriggersWhenTriggered)
        {
            for (int i = 0; i < triggerZones.Count; i++)
            {
                if (triggerZones[i] != null)
                    Destroy(triggerZones[i].gameObject);
            }
        }

        StartCoroutine(SpawnRoutine());
    }

    // ------------------------------------------------------------------
    // DIALOGUE
    // ------------------------------------------------------------------

    /// <summary>
    /// Starts the dialogue sequence. Mirrors DialogueTrigger2D's flow:
    /// DialogueSystemFreeze + playSequentially passes the whole sequence
    /// at once; all other paths advance line by line.
    /// </summary>
    private void StartDialogue()
    {
        if (dialogueSequence == null || dialogueSequence.Length == 0)
            return;

        if (dialogueSystem == null && dialogueSystemFreeze == null)
        {
            Debug.LogWarning("[SpawnGrowEffect] No DialogueSystem or DialogueSystemFreeze found for dialogue.", this);
            return;
        }

        dialogueIndex = 0;

        if (dialogueSystemFreeze != null && playSequentially)
        {
            // Pass the whole sequence at once so the UI never fades between lines.
            dialogueSystemFreeze.StartDialogue(dialogueSequence);
        }
        else
        {
            // Original DialogueSystem path, or playSequentially = false:
            // feed lines one at a time via HandleDialogueFinished.
            PlayLine(dialogueIndex);
        }
    }

    // Used by the original DialogueSystem path, or playSequentially = false
    private void PlayLine(int index)
    {
        index = Mathf.Clamp(index, 0, dialogueSequence.Length - 1);

        if (dialogueSystem != null)
            dialogueSystem.ShowDialogue(dialogueSequence[index]);
        else if (dialogueSystemFreeze != null)
            dialogueSystemFreeze.StartDialogue(new DialogueData[] { dialogueSequence[index] });
    }

    private void HandleDialogueFinished()
    {
        if (dialogueSequence == null || dialogueSequence.Length == 0)
            return;

        // DialogueSystemFreeze + playSequentially: the whole sequence was passed
        // at once, so one finished event means everything is done.
        if (dialogueSystemFreeze != null && playSequentially)
            return;

        // All other paths: advance line by line.
        if (!playSequentially)
            return;

        dialogueIndex++;

        if (dialogueIndex >= dialogueSequence.Length)
            return;

        PlayLine(dialogueIndex);
    }

    // ------------------------------------------------------------------
    // MUSIC
    // ------------------------------------------------------------------

    private void PlayEndMusic()
    {
        if (musicSource == null)
            return;

        if (restorePreviousMusicOnEnd)
        {
            musicSource.Stop();
            musicSource.clip = previousClip;
            musicSource.loop = previousLoop;

            if (previousWasPlaying && previousClip != null)
                musicSource.Play();

            return;
        }

        if (postSpawnMusic == null)
            return;

        musicSource.Stop();
        musicSource.clip = postSpawnMusic;

        if (matchPreviousLoop)
            musicSource.loop = previousLoop;

        musicSource.Play();
    }

    // ------------------------------------------------------------------
    // SPAWN ROUTINE
    // ------------------------------------------------------------------

    private IEnumerator SpawnRoutine()
    {
        // 1. Grow from zero to target scale while following the effect.
        if (growTarget != null)
        {
            float elapsed = 0f;
            while (elapsed < growDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / growDuration);
                float eased = growCurve != null ? growCurve.Evaluate(t) : t;

                growTarget.localScale = Vector3.Lerp(Vector3.zero, targetScale, eased);

                if (matchEffectPosition && spawnEffect != null)
                    growTarget.position = spawnEffect.transform.position;

                yield return null;
            }

            growTarget.localScale = targetScale;
        }

        // 2. Dialogue fires the moment the grow duration finishes.
        StartDialogue();

        // 3. Keep the growing object locked to the effect until it stops.
        if (spawnEffect != null)
        {
            while (spawnEffect.isPlaying)
            {
                if (matchEffectPosition && growTarget != null)
                    growTarget.position = spawnEffect.transform.position;

                yield return null;
            }
        }

        // 4. Release invincibility back to whatever it was before.
        if (makeInvincibleDuringSpawn && targetEnemy != null)
        {
            targetEnemy.isInvincible = cachedInvincible;
            targetEnemy.invincibilityTimer = cachedInvincibilityTimer;
        }

        // 5. Start the end music now that the effect is over.
        PlayEndMusic();

        // 6. Destroy the object this script is attached to.
        if (destroySelfWhenDone)
            Destroy(gameObject);
    }
}