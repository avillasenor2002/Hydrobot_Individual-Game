using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Reflection;
using System;

public class LevelIntroManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject introUIRoot;
    [SerializeField] private float fadeDuration = 1f;

    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip introMusic;
    [SerializeField] private AudioClip mainMusic;

    [Header("Intro Dialogue (Optional)")]
    public DialogueSystem dialogueSystem;
    public DialogueData[] dialogueSequence;
    public bool playDialogueOnce = true;
    public bool playSequentially = true;

    [Header("Debug Options")]
    public bool useOldEnemyCounter = false; // debug toggle to use old EnemyCounter

    private bool isIntroActive = true;
    private Image[] uiImages;
    private Text[] uiTexts;

    // Dialogue playback state
    private int currentIndex = 0;
    private bool dialoguePlayed = false;

    private void Start()
    {
        Time.timeScale = 0f; // Pause gameplay

        if (musicSource != null && introMusic != null)
        {
            musicSource.clip = introMusic;
            musicSource.Play();
        }

        if (introUIRoot != null)
        {
            uiImages = introUIRoot.GetComponentsInChildren<Image>(true);
            uiTexts = introUIRoot.GetComponentsInChildren<Text>(true);
        }

        // Optional: initialize old EnemyCounter if debug bool is true
        if (useOldEnemyCounter)
        {
            Debug.Log("[LevelIntroManager] Using old EnemyCounter for debugging purposes.");

            EnemyCounter counter = FindObjectOfType<EnemyCounter>();
            if (counter == null)
            {
                // Try to find the prefab or create a new GameObject
                GameObject counterGO = new GameObject("EnemyCounter");
                counter = counterGO.AddComponent<EnemyCounter>();
                Debug.Log("[LevelIntroManager] EnemyCounter was missing, created a new one at runtime.");
            }

            // Ensure the counter is active
            counter.gameObject.SetActive(true);
        }
    }

    private void Update()
    {
        if (!isIntroActive) return;

        bool anyKeyboard = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
        bool anyGamepad = Gamepad.current != null && (
            Gamepad.current.buttonSouth.wasPressedThisFrame ||
            Gamepad.current.buttonNorth.wasPressedThisFrame ||
            Gamepad.current.buttonEast.wasPressedThisFrame ||
            Gamepad.current.buttonWest.wasPressedThisFrame ||
            Gamepad.current.startButton.wasPressedThisFrame ||
            Gamepad.current.selectButton.wasPressedThisFrame
        );

        if (anyKeyboard || anyGamepad)
        {
            StartCoroutine(FadeOutIntro());
        }
    }

    private IEnumerator FadeOutIntro()
    {
        isIntroActive = false;

        if (musicSource != null && mainMusic != null)
        {
            musicSource.Stop();
            musicSource.clip = mainMusic;
            musicSource.Play();
        }

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);

            if (uiImages != null)
            {
                foreach (var img in uiImages)
                {
                    if (img == null) continue;
                    Color color = img.color;
                    color.a = alpha;
                    img.color = color;
                }
            }

            if (uiTexts != null)
            {
                foreach (var txt in uiTexts)
                {
                    if (txt == null) continue;
                    Color color = txt.color;
                    color.a = alpha;
                    txt.color = color;
                }
            }

            yield return null;
        }

        if (introUIRoot != null) introUIRoot.SetActive(false);
        Time.timeScale = 1f;

        // Start sequential dialogue if assigned
        if (dialogueSystem != null && dialogueSequence != null && dialogueSequence.Length > 0)
        {
            if (!dialoguePlayed || !playDialogueOnce)
            {
                dialoguePlayed = true;
                currentIndex = 0;
                dialogueSystem.OnDialogueFinished += HandleDialogueFinished;
                PlayDialogueLine(currentIndex);
            }
        }

        ReactivateAllTrackingEnemies();
    }

    private void PlayDialogueLine(int index)
    {
        index = Mathf.Clamp(index, 0, dialogueSequence.Length - 1);
        dialogueSystem.ShowDialogue(dialogueSequence[index]);
    }

    private void HandleDialogueFinished()
    {
        if (!playSequentially)
        {
            dialogueSystem.OnDialogueFinished -= HandleDialogueFinished;
            return;
        }

        currentIndex++;

        if (currentIndex >= dialogueSequence.Length)
        {
            dialogueSystem.OnDialogueFinished -= HandleDialogueFinished;
            return;
        }

        PlayDialogueLine(currentIndex);
    }

    // -----------------------------
    // Resetting / Reactivating logic
    // -----------------------------
    private void ReactivateAllTrackingEnemies()
    {
        // First: try explicit static-clear & public reset approach if TrackingEnemy exposes them
        bool didDirectReset = TryDirectResetAllTrackingEnemies();
        if (didDirectReset)
            return;

        // If direct reset wasn't available, fall back to the original reflection-based approach
        // Clear static event backing field on TrackingEnemy (if present) to avoid stale subscribers
        TryClearTrackingEnemyStaticEvent();

        // Find TrackingEnemy components (include inactive)
#if UNITY_2020_1_OR_NEWER
        var enemies = UnityEngine.Object.FindObjectsOfType(typeof(MonoBehaviour), true);
#else
        var enemies = UnityEngine.Object.FindObjectsOfType(typeof(MonoBehaviour));
#endif

        foreach (var mb in enemies)
        {
            if (mb == null) continue;

            var type = mb.GetType();
            if (type.Name != "TrackingEnemy") continue; // only target TrackingEnemy by name

            // cast to object, then call reflection helper
            ResetTrackingEnemyViaReflection(mb);
        }
    }

    /// <summary>
    /// Attempt to call TrackingEnemy.ClearStaticEvents() and ResetForScene() directly on all TrackingEnemy instances.
    /// Returns true if the direct method was found and invoked (no reflection fallback needed).
    /// </summary>
    private bool TryDirectResetAllTrackingEnemies()
    {
        // Try to get the TrackingEnemy type
        Type trackingType = Type.GetType("TrackingEnemy");
        if (trackingType == null)
        {
            // search assemblies
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                trackingType = asm.GetType("TrackingEnemy");
                if (trackingType != null) break;
            }
        }

        if (trackingType == null)
            return false;

        // Try to find static ClearStaticEvents method
        var clearMethod = trackingType.GetMethod("ClearStaticEvents", BindingFlags.Public | BindingFlags.Static);
        var resetMethod = trackingType.GetMethod("ResetForScene", BindingFlags.Public | BindingFlags.Instance);

        // If neither method exists, we cannot use direct approach
        if (clearMethod == null && resetMethod == null)
            return false;

        // If present, call ClearStaticEvents
        try
        {
            clearMethod?.Invoke(null, null);
        }
        catch (Exception)
        {
            // ignore and fallback to reflection below
        }

        // Now find all TrackingEnemy instances (including inactive)
#if UNITY_2020_1_OR_NEWER
        var instances = GameObject.FindObjectsOfType(trackingType, true);
#else
        var all = Resources.FindObjectsOfTypeAll(trackingType);
        var instances = new System.Collections.ArrayList();
        foreach (var o in all)
        {
            var go = o as UnityEngine.Object;
            if (go == null) continue;
            // ensure it's a scene object
            var comp = o as Component;
            if (comp != null && comp.gameObject.scene.IsValid())
                instances.Add(o);
        }
#endif

        // Invoke ResetForScene on each instance if available
        try
        {
#if UNITY_2020_1_OR_NEWER
            foreach (var inst in instances)
            {
                if (inst == null) continue;
                resetMethod?.Invoke(inst, null);
            }
#else
            foreach (var inst in instances)
            {
                if (inst == null) continue;
                resetMethod?.Invoke(inst, null);
            }
#endif
        }
        catch (Exception)
        {
            // if anything fails, allow fallback
            return false;
        }

        return true;
    }

    private void TryClearTrackingEnemyStaticEvent()
    {
        // Attempt to find the TrackingEnemy type
        var trackingType = Type.GetType("TrackingEnemy");
        if (trackingType == null)
        {
            // Try searching all loaded assemblies if direct type lookup fails
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                trackingType = asm.GetType("TrackingEnemy");
                if (trackingType != null) break;
            }
        }

        if (trackingType == null) return;

        // Try to find an event or field named OnTrackingEnemyDestroyed and clear it
        var field = trackingType.GetField("OnTrackingEnemyDestroyed", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        if (field != null)
        {
            field.SetValue(null, null);
            return;
        }

        var evt = trackingType.GetEvent("OnTrackingEnemyDestroyed", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (evt != null)
        {
            var backing = trackingType.GetField("OnTrackingEnemyDestroyed", BindingFlags.Static | BindingFlags.NonPublic);
            if (backing != null) backing.SetValue(null, null);
        }
    }

    private void ResetTrackingEnemyViaReflection(System.Object trackingEnemyInstance)
    {
        if (trackingEnemyInstance == null) return;

        var t = trackingEnemyInstance.GetType();

        var goProp = t.GetProperty("gameObject", BindingFlags.Instance | BindingFlags.Public);
        if (goProp != null)
        {
            GameObject go = goProp.GetValue(trackingEnemyInstance, null) as GameObject;
            if (go != null)
            {
                go.SetActive(true);
            }
        }
        else
        {
            var goField = t.GetField("gameObject", BindingFlags.Instance | BindingFlags.NonPublic);
            if (goField != null)
            {
                GameObject go = goField.GetValue(trackingEnemyInstance) as GameObject;
                if (go != null) go.SetActive(true);
            }
        }

        void SetPrivateBool(string name, bool value)
        {
            var fi = t.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (fi != null && fi.FieldType == typeof(bool))
                fi.SetValue(trackingEnemyInstance, value);
        }

        void SetPrivateFloat(string name, float value)
        {
            var fi = t.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (fi != null && fi.FieldType == typeof(float))
                fi.SetValue(trackingEnemyInstance, value);
        }

        void SetPrivateTransform(string name, Transform value)
        {
            var fi = t.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (fi != null && typeof(Transform).IsAssignableFrom(fi.FieldType))
                fi.SetValue(trackingEnemyInstance, value);
        }

        SetPrivateBool("isDead", false);
        SetPrivateBool("hasNoticedPlayer", false);
        SetPrivateBool("isInvincible", false);

        SetPrivateFloat("invincibilityTimer", 0f);
        SetPrivateFloat("fireCooldown", 0f);

        Transform playerTrans = GameObject.FindWithTag("Player")?.transform;
        SetPrivateTransform("playerTransform", playerTrans);

        var initField = t.GetField("initialZRotation", BindingFlags.Instance | BindingFlags.NonPublic);
        if (initField != null && initField.FieldType == typeof(float))
        {
            float initZ = (float)initField.GetValue(trackingEnemyInstance);
            var transformProp = t.GetProperty("transform", BindingFlags.Instance | BindingFlags.Public);
            if (transformProp != null)
            {
                Transform instTransform = transformProp.GetValue(trackingEnemyInstance, null) as Transform;
                if (instTransform != null)
                {
                    instTransform.rotation = Quaternion.Euler(0f, 0f, initZ);
                }
            }
        }

        var snapMethod = t.GetMethod("SnapToTilemap", BindingFlags.Instance | BindingFlags.NonPublic);
        if (snapMethod != null)
        {
            try { snapMethod.Invoke(trackingEnemyInstance, null); } catch { }
        }
        else
        {
            var snapPub = t.GetMethod("SnapToTilemapIfNeeded", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (snapPub != null)
            {
                try { snapPub.Invoke(trackingEnemyInstance, null); } catch { }
            }
        }

        var resetMethod = t.GetMethod("ResetEnemy", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (resetMethod != null)
        {
            try { resetMethod.Invoke(trackingEnemyInstance, null); } catch { }
        }
    }
}
