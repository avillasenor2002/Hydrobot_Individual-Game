using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerSettingsManager : MonoBehaviour
{
    public static PlayerSettingsManager Instance;

    [Header("UI Toggles")]
    [SerializeField] private Toggle invertStickToggle;
    [SerializeField] public Toggle showIndicatorToggle;

    [Header("Level Select Buttons (ONLY assign in Level Select scene)")]
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
            SceneManager.sceneLoaded += OnSceneLoaded;
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
        showBlueIndicator = PlayerPrefs.GetInt(SHOW_INDICATOR_KEY, 1) == 1;
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
            invertStickToggle.onValueChanged.AddListener(value =>
            {
                invertStick = value;
                SaveSettings();
            });
        }

        if (showIndicatorToggle != null)
        {
            showIndicatorToggle.isOn = showBlueIndicator;
            showIndicatorToggle.onValueChanged.AddListener(value =>
            {
                showBlueIndicator = value;
                SaveSettings();
            });
        }
    }

    private void InitializeLevelPrefs()
    {
        int buttonCount = levelButtons != null ? levelButtons.Length : 0;

        for (int i = 0; i < buttonCount; i++)
        {
            if (!PlayerPrefs.HasKey(LEVEL_COMPLETE_PREFIX + i))
                PlayerPrefs.SetInt(LEVEL_COMPLETE_PREFIX + i, i == 0 ? 1 : 0);
        }

        PlayerPrefs.Save();
    }

    // =========================
    // LEVEL LOCKING (SAFE)
    // =========================
    public void UpdateLevelSelectLock()
    {
        if (levelButtons == null || levelButtons.Length == 0)
            return;

        for (int i = 0; i < levelButtons.Length; i++)
        {
            if (levelButtons[i] == null)
                continue;

            if (i == 0)
            {
                levelButtons[i].interactable = true;
            }
            else
            {
                bool prevComplete =
                    PlayerPrefs.GetInt(LEVEL_COMPLETE_PREFIX + (i - 1), 0) == 1;

                levelButtons[i].interactable = prevComplete;
            }
        }
    }

    public void MarkLevelComplete(int levelIndex)
    {
        PlayerPrefs.SetInt(LEVEL_COMPLETE_PREFIX + levelIndex, 1);
        PlayerPrefs.Save();

        UpdateLevelSelectLock();
    }

    // =========================
    // SCENE HANDLING
    // =========================
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // IMPORTANT:
        // Do NOT auto-find buttons in gameplay scenes.
        // Level select scene must explicitly assign them.
        UpdateLevelSelectLock();
    }

    public static bool IsStickInverted() => Instance != null && Instance.invertStick;
    public static bool IsBlueIndicatorVisible() => Instance != null && Instance.showBlueIndicator;
}
