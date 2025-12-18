using UnityEngine;

public class EnemySceneWatcher : MonoBehaviour
{
    [Header("Object to Zero HP When No Enemies Remain")]
    public Enemy targetEnemy; // The enemy whose HP will be set to 0

    [Header("Enemy Detection")]
    public bool useEnemyComponent = true; // If true, checks for Enemy component
    public bool useTag = false;           // Optional: check by tag
    public string enemyTag = "Enemy";     // Tag to check if useTag is true

    private void Update()
    {
        bool anyEnemies = false;

        if (useEnemyComponent)
        {
            Enemy[] enemies = FindObjectsOfType<Enemy>();
            if (enemies.Length > 0)
                anyEnemies = true;
        }

        if (useTag)
        {
            GameObject[] enemiesByTag = GameObject.FindGameObjectsWithTag(enemyTag);
            if (enemiesByTag.Length > 0)
                anyEnemies = true;
        }

        if (!anyEnemies && targetEnemy != null && !targetEnemy.isDead)
        {
            targetEnemy.TakeDamage(targetEnemy.health); // Reduce HP to 0
        }
    }
}
