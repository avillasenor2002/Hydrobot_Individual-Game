using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EnemyCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI enemyCountText;
    [SerializeField] private LevelEndManager levelEndManager; // Reference to LevelEndManager

    private int totalEnemiesAtStart;
    private int currentEnemiesCounted;

    private void Start()
    {
        totalEnemiesAtStart = 0;
        Enemy[] allEnemies = FindObjectsOfType<Enemy>();
        foreach (var enemy in allEnemies)
        {
            if (!enemy.isObject)
            {
                totalEnemiesAtStart++;
            }
        }

        currentEnemiesCounted = 0;
        UpdateEnemyCountText();
    }

    private void OnEnable()
    {
        Enemy.OnEnemyDestroyed += HandleEnemyDestroyed;
    }

    private void OnDisable()
    {
        Enemy.OnEnemyDestroyed -= HandleEnemyDestroyed;
    }

    private void HandleEnemyDestroyed()
    {
        currentEnemiesCounted++;
        UpdateEnemyCountText();

        if (currentEnemiesCounted >= totalEnemiesAtStart)
        {
            enemyCountText.text = "Complete!";

            if (levelEndManager != null)
            {
                levelEndManager.TriggerLevelEnd();
            }
        }
    }

    private void UpdateEnemyCountText()
    {
        enemyCountText.text = $"{currentEnemiesCounted} / {totalEnemiesAtStart}";
    }
}
