using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum LevelObjectiveType
{
    DefeatEnemies,
    DestroyObjects
}

[System.Serializable]
public class LevelObjective
{
    public LevelObjectiveType objectiveType;

    [Header("Specific Enemy Target (Optional)")]
    public Enemy targetEnemyPrefab; // Assign a prefab if only a specific enemy needs to be defeated
    public int specificEnemyCount = 1;

    [Header("Generic Enemy Count Override")]
    public bool useGenericEnemyCount = false;
    public int genericEnemyCount = 1;

    [Header("Objects to Destroy")]
    public List<Enemy> objectsToDestroy = new List<Enemy>(); // Drag Enemy objects with isObject = true here

    [Header("UI Display")]
    public string objectiveName;
}

public class LevelObjectiveManager : MonoBehaviour
{
    [Header("Objective Settings")]
    public LevelObjective currentObjective;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI objectiveNameText;
    [SerializeField] private TextMeshProUGUI objectiveProgressText;

    [Header("Level End Reference")]
    [SerializeField] private LevelEndManager levelEndManager;

    private int currentCount = 0;
    private int targetCount = 0;

    private void Start()
    {
        // Initialize target count
        switch (currentObjective.objectiveType)
        {
            case LevelObjectiveType.DefeatEnemies:
                if (currentObjective.useGenericEnemyCount)
                    targetCount = currentObjective.genericEnemyCount;
                else if (currentObjective.targetEnemyPrefab != null)
                    targetCount = currentObjective.specificEnemyCount;
                else
                {
                    Enemy[] allEnemies = FindObjectsOfType<Enemy>();
                    targetCount = 0;
                    foreach (var e in allEnemies)
                        if (!e.isObject) targetCount++;
                }

                // Subscribe to all existing enemies
                Enemy[] enemies = FindObjectsOfType<Enemy>();
                foreach (var e in enemies)
                    e.OnEnemyDeath += HandleEnemyDeath;
                break;

            case LevelObjectiveType.DestroyObjects:
                targetCount = currentObjective.objectsToDestroy.Count;

                // Subscribe to death events for listed objects
                foreach (var obj in currentObjective.objectsToDestroy)
                    if (obj != null)
                        obj.OnEnemyDeath += HandleEnemyDeath;
                break;
        }

        UpdateObjectiveUI();
    }

    private void HandleEnemyDeath(Enemy deadEnemy)
    {
        if (currentObjective.objectiveType == LevelObjectiveType.DefeatEnemies)
        {
            if (currentObjective.useGenericEnemyCount)
            {
                currentCount++;
            }
            else if (currentObjective.targetEnemyPrefab != null)
            {
                if (deadEnemy.name.StartsWith(currentObjective.targetEnemyPrefab.name))
                    currentCount++;
            }
            else
            {
                currentCount++;
            }
        }
        else if (currentObjective.objectiveType == LevelObjectiveType.DestroyObjects)
        {
            if (currentObjective.objectsToDestroy.Contains(deadEnemy))
            {
                currentCount++;
                currentObjective.objectsToDestroy.Remove(deadEnemy);
            }
        }

        UpdateObjectiveUI();

        if (currentCount >= targetCount)
            CompleteObjective();
    }

    private void UpdateObjectiveUI()
    {
        string displayName = currentObjective.objectiveName;

        if (string.IsNullOrEmpty(displayName))
        {
            switch (currentObjective.objectiveType)
            {
                case LevelObjectiveType.DefeatEnemies:
                    displayName = targetCount > 1 ? $"Defeat {targetCount} Enemies" : $"Defeat {targetCount} Enemy";
                    break;
                case LevelObjectiveType.DestroyObjects:
                    displayName = targetCount > 1 ? $"Rescue {targetCount} Objects" : $"Rescue {targetCount} Object";
                    break;
            }
        }

        if (objectiveNameText != null)
            objectiveNameText.text = displayName;

        if (objectiveProgressText != null)
            objectiveProgressText.text = $"{currentCount:00} / {targetCount:00}";
    }

    private void CompleteObjective()
    {
        if (objectiveProgressText != null)
            objectiveProgressText.text = "Complete!";

        if (levelEndManager != null)
            levelEndManager.TriggerLevelEnd();
    }
}
