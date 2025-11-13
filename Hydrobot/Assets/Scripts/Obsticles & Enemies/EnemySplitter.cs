using UnityEngine;

public class EnemySplitter : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int spawnCount = 3;
    [SerializeField] private float spawnRadius = 0.5f;

    private void OnEnable()
    {
        Enemy.OnEnemyDestroyed += SpawnEnemies;
    }

    private void OnDisable()
    {
        Enemy.OnEnemyDestroyed -= SpawnEnemies;
    }

    private void SpawnEnemies()
    {
        if (enemyPrefab == null) return;

        // Spawn at the last destroyed enemy's position
        Vector3 spawnPosition = transform.position;

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 offset = Random.insideUnitCircle * spawnRadius;
            GameObject newEnemy = Instantiate(enemyPrefab, spawnPosition + offset, Quaternion.identity);

            // Ensure renderers are active
            SpriteRenderer[] renderers = newEnemy.GetComponentsInChildren<SpriteRenderer>();
            foreach (var renderer in renderers)
            {
                renderer.enabled = true;
            }
        }
    }
}
