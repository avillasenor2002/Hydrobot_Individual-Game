using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using System.Reflection;
using System;

/// <summary>
/// Level intro manager that fades intro UI and then reactivates/resets all TrackingEnemy instances in the scene.
/// This script uses reflection to reset private fields on TrackingEnemy so enemies reliably restart after the intro.
/// </summary>
public class LevelIntroManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject introUIRoot; // root of intro UI
    [SerializeField] private float fadeDuration = 1f;

    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip introMusic;
    [SerializeField] private AudioClip mainMusic;

    private bool isIntroActive = true;
    private Image[] uiImages;
    private Text[] uiTexts;

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
    }

    private void Update()
    {
        if (!isIntroActive) return;

        bool anyKeyboard = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
        bool anyGamepad = false;
        if (Gamepad.current != null)
        {
            anyGamepad = Gamepad.current.buttonSouth.wasPressedThisFrame ||
                         Gamepad.current.buttonNorth.wasPressedThisFrame ||
                         Gamepad.current.buttonEast.wasPressedThisFrame ||
                         Gamepad.current.buttonWest.wasPressedThisFrame ||
                         Gamepad.current.startButton.wasPressedThisFrame ||
                         Gamepad.current.selectButton.wasPressedThisFrame;
        }

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

        if (introUIRoot != null) introUIRoot.SetActive(false); // Fully disable UI
        Time.timeScale = 1f; // Resume gameplay

        // Reactivate/reset all tracking enemies now that gameplay resumes
        ReactivateAllTrackingEnemies();
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
        // Events are usually backed by a private static field with same name in C#
        var field = trackingType.GetField("OnTrackingEnemyDestroyed", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        if (field != null)
        {
            field.SetValue(null, null);
            return;
        }

        // If not found as a field, try as an event and reset via reflection to null (non-portable but attempted)
        var evt = trackingType.GetEvent("OnTrackingEnemyDestroyed", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (evt != null)
        {
            // obtain backing field by naming convention "<EventName>k__BackingField" or same name
            var backing = trackingType.GetField("OnTrackingEnemyDestroyed", BindingFlags.Static | BindingFlags.NonPublic);
            if (backing != null) backing.SetValue(null, null);
        }
    }

    private void ResetTrackingEnemyViaReflection(System.Object trackingEnemyInstance)
    {
        if (trackingEnemyInstance == null) return;

        var t = trackingEnemyInstance.GetType();

        // Activate GameObject
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
            // fallback: try field 'gameObject' (rare)
            var goField = t.GetField("gameObject", BindingFlags.Instance | BindingFlags.NonPublic);
            if (goField != null)
            {
                GameObject go = goField.GetValue(trackingEnemyInstance) as GameObject;
                if (go != null) go.SetActive(true);
            }
        }

        // Helper to set private instance fields safely
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

        // Reset common private flags used in the TrackingEnemy implementation
        SetPrivateBool("isDead", false);
        SetPrivateBool("hasNoticedPlayer", false);
        SetPrivateBool("isInvincible", false);

        SetPrivateFloat("invincibilityTimer", 0f);
        SetPrivateFloat("fireCooldown", 0f);

        // Restore playerTransform to current player
        Transform playerTrans = GameObject.FindWithTag("Player")?.transform;
        SetPrivateTransform("playerTransform", playerTrans);

        // Attempt to restore rotation to initialZRotation if that private field exists
        var initField = t.GetField("initialZRotation", BindingFlags.Instance | BindingFlags.NonPublic);
        if (initField != null && initField.FieldType == typeof(float))
        {
            float initZ = (float)initField.GetValue(trackingEnemyInstance);
            // set actual transform rotation
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

        // Call SnapToTilemap() if exists (private)
        var snapMethod = t.GetMethod("SnapToTilemap", BindingFlags.Instance | BindingFlags.NonPublic);
        if (snapMethod != null)
        {
            try { snapMethod.Invoke(trackingEnemyInstance, null); } catch { }
        }
        else
        {
            // try public SnapToTilemapIfNeeded or SnapToTilemap public variations
            var snapPub = t.GetMethod("SnapToTilemapIfNeeded", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (snapPub != null)
            {
                try { snapPub.Invoke(trackingEnemyInstance, null); } catch { }
            }
        }

        // If there is a public method to explicitly reset the enemy, call it
        var resetMethod = t.GetMethod("ResetEnemy", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (resetMethod != null)
        {
            try { resetMethod.Invoke(trackingEnemyInstance, null); } catch { }
        }
    }
}
