using UnityEngine;
using UnityEngine.UI;

public class PlayerSettingsManager : MonoBehaviour
{
    public static PlayerSettingsManager Instance;

    [Header("UI Toggles")]
    [SerializeField] private Toggle invertStickToggle;
    [SerializeField] public Toggle showIndicatorToggle;

    private const string INVERT_STICK_KEY = "InvertStick";
    private const string SHOW_INDICATOR_KEY = "ShowBlueIndicator";

    [HideInInspector] public bool invertStick;
    [HideInInspector] public bool showBlueIndicator;

    private void Awake()
    {
        // Singleton pattern to persist across scenes
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadSettings();
        SetupUIToggles();
    }

    /// <summary>
    /// Loads settings from PlayerPrefs
    /// </summary>
    private void LoadSettings()
    {
        invertStick = PlayerPrefs.GetInt(INVERT_STICK_KEY, 0) == 1;
        showBlueIndicator = PlayerPrefs.GetInt(SHOW_INDICATOR_KEY, 1) == 1; // default: visible
    }

    /// <summary>
    /// Saves current settings to PlayerPrefs
    /// </summary>
    private void SaveSettings()
    {
        PlayerPrefs.SetInt(INVERT_STICK_KEY, invertStick ? 1 : 0);
        PlayerPrefs.SetInt(SHOW_INDICATOR_KEY, showBlueIndicator ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Connect UI toggles to current settings
    /// </summary>
    private void SetupUIToggles()
    {
        if (invertStickToggle != null)
        {
            invertStickToggle.isOn = invertStick;
            invertStickToggle.onValueChanged.AddListener((value) =>
            {
                invertStick = value;
                SaveSettings();
            });
        }

        if (showIndicatorToggle != null)
        {
            showIndicatorToggle.isOn = showBlueIndicator;
            showIndicatorToggle.onValueChanged.AddListener((value) =>
            {
                showBlueIndicator = value;
                SaveSettings();
            });
        }
    }

    /// <summary>
    /// Helper functions to get the settings from other scripts
    /// </summary>
    public static bool IsStickInverted() => Instance != null && Instance.invertStick;
    public static bool IsBlueIndicatorVisible() => Instance != null && Instance.showBlueIndicator;
}
