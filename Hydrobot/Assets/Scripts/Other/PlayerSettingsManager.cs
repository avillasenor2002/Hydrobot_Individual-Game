using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerSettingsManager : MonoBehaviour
{
    public static PlayerSettingsManager Instance;

    [Header("UI Toggles")]
    [SerializeField] private Toggle invertStickToggle;
    [SerializeField] public Toggle showIndicatorToggle;

    [Header("Level Select Buttons")]
    [SerializeField] private Button[] levelButtons;

    private const string INVERT_STICK_KEY = "InvertStick";
    private const string SHOW_INDICATOR_KEY = "ShowBlueIndicator";
    private const string LEVEL_COMPLETE_PREFIX = "LevelComplete_";

    [HideInInspector] public bool invertStick;
    [HideInInspector] public bool showBlueIndicator;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded; // Hook to reacquire buttons
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadSettings();
        SetupUIToggles();
        InitializeLevelPrefs();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void LoadSettings()
    {
        invertStick = PlayerPrefs.GetInt(INVERT_STICK_KEY, 0) == 1;
        showBlueIndicator = PlayerPrefs.GetInt(SHOW_INDICATOR_KEY, 1) == 1; // default: visible
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetInt(INVERT_STICK_KEY, invertStick ? 1 : 0);
        PlayerPrefs.SetInt(SHOW_INDICATOR_KEY, showBlueIndicator ? 1 : 0);
        PlayerPrefs.Save();
    }

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
    /// Ensure all levels have a PlayerPrefs entry; first level is unlocked by default
    /// </summary>
    private void InitializeLevelPrefs()
    {
        if (levelButtons == null) return;

        for (int i = 0; i < levelButtons.Length; i++)
        {
            if (!PlayerPrefs.HasKey(LEVEL_COMPLETE_PREFIX + i))
            {
                PlayerPrefs.SetInt(LEVEL_COMPLETE_PREFIX + i, i == 0 ? 1 : 0); // first level unlocked
            }
        }
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Update which level buttons are interactable based on previous completions
    /// </summary>
    public void UpdateLevelSelectLock()
    {
        if (levelButtons == null) return;

        for (int i = 0; i < levelButtons.Length; i++)
        {
            if (i == 0)
            {
                levelButtons[i].interactable = true;
            }
            else
            {
                bool prevLevelComplete = PlayerPrefs.GetInt(LEVEL_COMPLETE_PREFIX + (i - 1), 0) == 1;
                levelButtons[i].interactable = prevLevelComplete;
            }
        }
    }

    /// <summary>
    /// Call this when a level is beaten
    /// </summary>
    public void MarkLevelComplete(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levelButtons.Length) return;

        PlayerPrefs.SetInt(LEVEL_COMPLETE_PREFIX + levelIndex, 1);
        PlayerPrefs.Save();

        UpdateLevelSelectLock();
    }

    /// <summary>
    /// Reacquire buttons every scene load
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Attempt to find all buttons if they exist in the new scene
        if (levelButtons == null || levelButtons.Length == 0)
        {
            levelButtons = GameObject.FindObjectsOfType<Button>();
        }

        UpdateLevelSelectLock();
    }

    public static bool IsStickInverted() => Instance != null && Instance.invertStick;
    public static bool IsBlueIndicatorVisible() => Instance != null && Instance.showBlueIndicator;
}
