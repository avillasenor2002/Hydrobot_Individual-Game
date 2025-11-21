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

    [Header("Exclude Enemies From Count")]
    public List<Enemy> excludedEnemyPrefabs = new List<Enemy>(); // Prefabs to NOT count toward the enemy counter

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
        switch (currentObjective.objectiveType)
        {
            case LevelObjectiveType.DefeatEnemies:
                if (currentObjective.useGenericEnemyCount)
                {
                    targetCount = currentObjective.genericEnemyCount;
                }
                else if (currentObjective.targetEnemyPrefab != null)
                {
                    targetCount = currentObjective.specificEnemyCount;
                }
                else
                {
                    // Count only enemies that are NOT objects and not excluded
                    Enemy[] allEnemies = FindObjectsOfType<Enemy>();
                    targetCount = 0;
                    foreach (var e in allEnemies)
                    {
                        if (!e.isObject && !currentObjective.excludedEnemyPrefabs.Contains(e))
                            targetCount++;
                    }
                }

                // Subscribe to death events for enemies that are NOT objects and not excluded
                Enemy[] enemies = FindObjectsOfType<Enemy>();
                foreach (var e in enemies)
                {
                    if (!e.isObject && !currentObjective.excludedEnemyPrefabs.Contains(e))
                        e.OnEnemyDeath += HandleEnemyDeath;
                }
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
            // Only count enemies that are NOT objects and NOT excluded
            if (!deadEnemy.isObject && !currentObjective.excludedEnemyPrefabs.Contains(deadEnemy))
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
