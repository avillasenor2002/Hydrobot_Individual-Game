using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Sits on a level select button. Holds a LevelData asset and pushes its
/// info into three TextMeshPro components when the button is highlighted.
/// When the LevelData is locked, the button is disabled for navigation,
/// its children are hidden, and a locked sprite is shown instead.
/// </summary>
[RequireComponent(typeof(Button))]
public class LevelDataHolder : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    ISelectHandler,
    IDeselectHandler
{
    [Header("Level Data")]
    [Tooltip("The LevelData asset this button represents.")]
    public LevelData levelData;

    [Header("Text Output")]
    [Tooltip("TextMeshPro that shows the level name.")]
    public TMP_Text levelNameText;

    [Tooltip("TextMeshPro that shows the objective name.")]
    public TMP_Text objectiveNameText;

    [Tooltip("TextMeshPro that shows the objective number.")]
    public TMP_Text objectiveNumberText;

    [Header("Options")]
    [Tooltip("If true, the button's own label is also updated with the level name.")]
    public bool updateButtonLabel = false;

    [Tooltip("TMP label on the button itself, used when updateButtonLabel is true.")]
    public TMP_Text buttonLabel;

    [Tooltip("If true, the info is also refreshed on Start, not just on highlight.")]
    public bool refreshOnStart = true;

    [Header("Locking")]
    [Tooltip("Sprite shown on the button's Image while the level is locked.")]
    public Sprite lockedSprite;

    [Header("Completion")]
    [Tooltip("Object that is activated when the level's isComplete is true, and deactivated when false. " +
             "Commonly a checkmark or 'completed' badge.")]
    public GameObject completedObject;

    private Button button;
    private Image buttonImage;

    // Cached original state so unlocking restores exactly what was there.
    private Sprite originalSprite;
    private readonly List<GameObject> cachedActiveChildren = new List<GameObject>();
    private readonly List<GameObject> cachedInactiveChildren = new List<GameObject>();

    private void Awake()
    {
        button = GetComponent<Button>();
        buttonImage = button.targetGraphic as Image;

        // Cache the original sprite and which children started active.
        if (buttonImage != null)
            originalSprite = buttonImage.sprite;

        cachedActiveChildren.Clear();
        cachedInactiveChildren.Clear();

        foreach (Transform child in transform)
        {
            if (child.gameObject.activeSelf)
                cachedActiveChildren.Add(child.gameObject);
            else
                cachedInactiveChildren.Add(child.gameObject);
        }
    }

    private void Start()
    {
        ApplyLockState();
        ApplyCompletionState();

        if (refreshOnStart)
            Refresh();

        if (updateButtonLabel && buttonLabel != null && levelData != null)
            buttonLabel.text = levelData.levelName;
    }

    // ------------------------------------------------------------------
    // HIGHLIGHT HOOKS
    // ------------------------------------------------------------------

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Locked buttons are not interactable, but guard anyway.
        if (levelData != null && levelData.locked)
            return;

        Refresh();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Optional: clear the display when the mouse leaves.
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (levelData != null && levelData.locked)
            return;

        Refresh();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        // Optional: clear the display when deselected.
    }

    // ------------------------------------------------------------------
    // DISPLAY
    // ------------------------------------------------------------------

    /// <summary>
    /// Pushes the current LevelData into the three TextMeshPro components.
    /// Call this manually if you ever need to force a refresh.
    /// </summary>
    public void Refresh()
    {
        if (levelData == null)
        {
            Debug.LogWarning("LevelDataHolder: No levelData assigned on " + gameObject.name, this);
            return;
        }

        if (levelNameText != null)
            levelNameText.text = levelData.levelName;

        if (objectiveNameText != null)
            objectiveNameText.text = levelData.objectiveName;

        if (objectiveNumberText != null)
            objectiveNumberText.text = levelData.objectiveNum;

        if (updateButtonLabel && buttonLabel != null)
            buttonLabel.text = levelData.levelName;
    }

    // ------------------------------------------------------------------
    // LOCKING
    // ------------------------------------------------------------------

    /// <summary>
    /// Applies the lock state based on the current LevelData.locked value.
    /// Safe to call at any time; use it after changing locked at runtime.
    /// </summary>
    public void ApplyLockState()
    {
        bool locked = levelData != null && levelData.locked;

        if (locked)
        {
            // 1. Disable navigation and interaction.
            button.interactable = false;

            Navigation nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;

            // 2. Hide the button's children.
            SetChildrenActive(false);

            // 3. Swap in the locked sprite.
            if (buttonImage != null && lockedSprite != null)
                buttonImage.sprite = lockedSprite;
        }
        else
        {
            // 1. Re-enable interaction and navigation.
            button.interactable = true;

            Navigation nav = button.navigation;
            nav.mode = Navigation.Mode.Automatic;
            button.navigation = nav;

            // 2. Restore the children that were active at Start.
            SetChildrenActive(true);

            // 3. Restore the button's original sprite.
            if (buttonImage != null && originalSprite != null)
                buttonImage.sprite = originalSprite;
        }
    }

    private void SetChildrenActive(bool show)
    {
        if (show)
        {
            // Reactivate only the children that were originally active.
            for (int i = 0; i < cachedActiveChildren.Count; i++)
                cachedActiveChildren[i].SetActive(true);

            // Ensure originally inactive children stay off.
            for (int i = 0; i < cachedInactiveChildren.Count; i++)
                cachedInactiveChildren[i].SetActive(false);
        }
        else
        {
            foreach (Transform child in transform)
                child.gameObject.SetActive(false);
        }
    }

    // ------------------------------------------------------------------
    // COMPLETION
    // ------------------------------------------------------------------

    /// <summary>
    /// Activates the completedObject when the level is complete, and
    /// deactivates it when it is not. Safe to call at any time.
    /// </summary>
    public void ApplyCompletionState()
    {
        if (completedObject == null)
            return;

        bool complete = levelData != null && levelData.isComplete;
        completedObject.SetActive(complete);
    }

    // ------------------------------------------------------------------
    // CONVENIENCE
    // ------------------------------------------------------------------

    /// <summary>Returns the LevelData this button is holding.</summary>
    public LevelData GetLevelData() => levelData;

    /// <summary>Swaps out the LevelData and refreshes the display, lock state, and completion state.</summary>
    public void SetLevelData(LevelData newData)
    {
        levelData = newData;
        ApplyLockState();
        ApplyCompletionState();
        Refresh();
    }
}