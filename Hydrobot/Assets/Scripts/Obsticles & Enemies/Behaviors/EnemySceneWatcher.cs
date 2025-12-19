using UnityEngine;

public class EnemySceneWatcher : MonoBehaviour
{
    [Header("Level End")]
    [SerializeField] private LevelEndManager levelEndManager;

    [Header("Enemy Detection")]
    [SerializeField] private bool useEnemyComponent = true;
    [SerializeField] private bool useTag = false;
    [SerializeField] private string enemyTag = "Enemy";

    private bool hasTriggeredEnd = false;

    private void Update()
    {
        if (hasTriggeredEnd) return;

        bool enemiesRemain = false;

        if (useEnemyComponent)
        {
            Enemy[] enemies = FindObjectsOfType<Enemy>();
            foreach (Enemy e in enemies)
            {
                if (!e.isDead)
                {
                    enemiesRemain = true;
                    break;
                }
            }
        }

        if (!enemiesRemain && useTag)
        {
            if (GameObject.FindGameObjectsWithTag(enemyTag).Length > 0)
                enemiesRemain = true;
        }

        if (!enemiesRemain)
        {
            hasTriggeredEnd = true;

            if (levelEndManager != null)
                levelEndManager.TriggerLevelEnd();
            else
                Debug.LogError("[EnemySceneWatcher] LevelEndManager not assigned.");
        }
    }
}
