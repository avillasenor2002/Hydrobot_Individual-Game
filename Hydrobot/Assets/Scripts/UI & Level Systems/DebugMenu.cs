using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// Persistent debug overlay.
///
/// Lives on a DontDestroyOnLoad object and draws a small always-on panel.
/// Keyboard hotkeys:
///   F1 - Load the specific scene set below
///   F2 - Restart the current scene
///   F3 - Toggle invert stick (if a PlayerSettingsManager exists)
///   F4 - Toggle blue indicator (if a PlayerSettingsManager exists)
///   F5 - Show/hide the debug panel
/// </summary>
public class DebugMenu : MonoBehaviour
{
    public static DebugMenu Instance { get; private set; }

    [Header("Scene Hotkeys")]
    [Tooltip("Scene loaded when the 'Load Specific Scene' hotkey is pressed.")]
    [SerializeField] private string specificSceneName = "";

    [Tooltip("Hotkey that loads the specific scene.")]
    [SerializeField] private Key specificSceneKey = Key.F1;

    [Tooltip("Hotkey that restarts the current scene.")]
    [SerializeField] private Key restartSceneKey = Key.F2;

    [Header("Settings Hotkeys")]
    [Tooltip("Hotkey that toggles the invert stick setting.")]
    [SerializeField] private Key invertStickKey = Key.F3;

    [Tooltip("Hotkey that toggles the blue indicator setting.")]
    [SerializeField] private Key showIndicatorKey = Key.F4;

    [Tooltip("Hotkey that shows/hides the debug panel.")]
    [SerializeField] private Key togglePanelKey = Key.F5;

    [Header("Panel")]
    [Tooltip("If true, the panel is visible on start. F5 toggles it.")]
    [SerializeField] private bool showPanel = true;

    [Tooltip("Screen-space offset of the panel.")]
    [SerializeField] private Vector2 panelPosition = new Vector2(10f, 10f);

    [Tooltip("Panel width in pixels.")]
    [SerializeField] private float panelWidth = 360f;

    [Header("Optional Toggle References")]
    [Tooltip("Optional. Assign the same Toggle that PlayerSettingsManager uses for Invert Stick " +
             "so its UI updates when the hotkey fires.")]
    [SerializeField] private Toggle invertStickToggle;

    [Header("Options")]
    [Tooltip("Log every debug action to the console.")]
    [SerializeField] private bool logActions = true;

    // Same PlayerPrefs keys as PlayerSettingsManager.
    private const string INVERT_STICK_KEY = "InvertStick";
    private const string SHOW_INDICATOR_KEY = "ShowBlueIndicator";

    // ------------------------------------------------------------------
    // Lifecycle
    // ------------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb[specificSceneKey].wasPressedThisFrame) LoadSpecificScene();
        if (kb[restartSceneKey].wasPressedThisFrame) RestartCurrentScene();
        if (kb[invertStickKey].wasPressedThisFrame) ToggleInvertStick();
        if (kb[showIndicatorKey].wasPressedThisFrame) ToggleShowIndicator();
        if (kb[togglePanelKey].wasPressedThisFrame) showPanel = !showPanel;
    }

    // ------------------------------------------------------------------
    // ACTIONS
    // ------------------------------------------------------------------

    private void LoadSpecificScene()
    {
        if (string.IsNullOrEmpty(specificSceneName))
        {
            Debug.LogWarning("[DebugMenu] No specific scene name set.", this);
            return;
        }

        if (logActions)
            Debug.Log($"[DebugMenu] Loading scene '{specificSceneName}'.", this);

        SceneManager.LoadScene(specificSceneName);
    }

    private void RestartCurrentScene()
    {
        Scene active = SceneManager.GetActiveScene();

        if (logActions)
            Debug.Log($"[DebugMenu] Restarting scene '{active.name}'.", this);

        SceneManager.LoadScene(active.buildIndex);
    }

    private void ToggleInvertStick()
    {
        PlayerSettingsManager psm = PlayerSettingsManager.Instance;

        if (psm == null)
        {
            if (logActions)
                Debug.Log("[DebugMenu] No PlayerSettingsManager in scene — invert stick not toggled.", this);
            return;
        }

        // Preferred path: flip the actual Toggle so its listener saves + updates UI.
        if (invertStickToggle != null)
        {
            invertStickToggle.isOn = !invertStickToggle.isOn;
        }
        else
        {
            // Fallback: set the field directly and persist it with the same key.
            psm.invertStick = !psm.invertStick;
            PlayerPrefs.SetInt(INVERT_STICK_KEY, psm.invertStick ? 1 : 0);
            PlayerPrefs.Save();
        }

        if (logActions)
            Debug.Log($"[DebugMenu] Invert Stick: {(psm.invertStick ? "ON" : "OFF")}.", this);
    }

    private void ToggleShowIndicator()
    {
        PlayerSettingsManager psm = PlayerSettingsManager.Instance;

        if (psm == null)
        {
            if (logActions)
                Debug.Log("[DebugMenu] No PlayerSettingsManager in scene — indicator not toggled.", this);
            return;
        }

        // showIndicatorToggle is public on PlayerSettingsManager, so we can use it directly.
        if (psm.showIndicatorToggle != null)
        {
            psm.showIndicatorToggle.isOn = !psm.showIndicatorToggle.isOn;
        }
        else
        {
            psm.showBlueIndicator = !psm.showBlueIndicator;
            PlayerPrefs.SetInt(SHOW_INDICATOR_KEY, psm.showBlueIndicator ? 1 : 0);
            PlayerPrefs.Save();
        }

        if (logActions)
            Debug.Log($"[DebugMenu] Blue Indicator: {(psm.showBlueIndicator ? "ON" : "OFF")}.", this);
    }

    // ------------------------------------------------------------------
    // PANEL
    // ------------------------------------------------------------------

    private void OnGUI()
    {
        if (!showPanel) return;

        const float lineHeight = 20f;
        const float padding = 10f;

        float height = lineHeight * 9f + padding * 2f + 6f;
        Rect box = new Rect(panelPosition.x, panelPosition.y, panelWidth, height);

        GUI.Box(box, $"Debug Menu  ({togglePanelKey} to hide)");

        Rect contentRect = new Rect(
            box.x + padding,
            box.y + lineHeight + 4f,
            box.width - padding * 2f,
            box.height - lineHeight - padding * 2f);

        GUILayout.BeginArea(contentRect);

        GUILayout.Label($"Scene: {SceneManager.GetActiveScene().name}");
        GUILayout.Label($"[{specificSceneKey}]  Load: {(string.IsNullOrEmpty(specificSceneName) ? "<not set>" : specificSceneName)}");
        GUILayout.Label($"[{restartSceneKey}]  Restart Current Scene");

        PlayerSettingsManager psm = PlayerSettingsManager.Instance;
        if (psm != null)
        {
            GUILayout.Label($"[{invertStickKey}]  Invert Stick: {(psm.invertStick ? "ON" : "OFF")}");
            GUILayout.Label($"[{showIndicatorKey}]  Blue Indicator: {(psm.showBlueIndicator ? "ON" : "OFF")}");
        }
        else
        {
            GUILayout.Label($"[{invertStickKey}]  Invert Stick: (no PlayerSettingsManager)");
            GUILayout.Label($"[{showIndicatorKey}]  Blue Indicator: (no PlayerSettingsManager)");
        }

        GUILayout.Label($"[{togglePanelKey}]  Hide Panel");

        GUILayout.EndArea();
    }
}