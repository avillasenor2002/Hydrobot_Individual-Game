using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Spawns TrackingEnemy prefabs at specified positions, snaps them to the main Tilemap, then destroys itself.
/// </summary>
public class TrackingEnemySpawner : MonoBehaviour
{
    [Header("Enemies to Spawn")]
    public GameObject[] enemyPrefabs;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    [Header("Tilemap")]
    [Tooltip("The Tilemap to snap spawned enemies to. If none assigned, will search for 'Main Tilemap'.")]
    public Tilemap targetTilemap;

    private void Start()
    {
        if (targetTilemap == null)
        {
            FindMainTilemap();
        }

        SpawnEnemies();
        Destroy(gameObject); // remove spawner after spawning
    }

    private void SpawnEnemies()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0 || spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("TrackingEnemySpawner: No enemies or spawn points assigned.");
            return;
        }

        int prefabIndex = 0;

        foreach (var spawnPoint in spawnPoints)
        {
            if (spawnPoint == null) continue;

            GameObject prefabToSpawn = enemyPrefabs[prefabIndex % enemyPrefabs.Length];
            GameObject enemyInstance = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
            enemyInstance.name = prefabToSpawn.name;

            // Snap to tilemap if available
            if (targetTilemap != null)
            {
                Vector3Int cell = targetTilemap.WorldToCell(enemyInstance.transform.position);
                enemyInstance.transform.position = targetTilemap.GetCellCenterWorld(cell);
            }

            prefabIndex++;
        }
    }

    private void FindMainTilemap()
    {
        Tilemap[] maps = FindObjectsOfType<Tilemap>();
        foreach (var map in maps)
        {
            if (map.gameObject.name == "Main Tilemap")
            {
                targetTilemap = map;
                return;
            }
        }

        Debug.LogWarning("TrackingEnemySpawner: Could not find a Tilemap named 'Main Tilemap'. Enemies will not snap.");
    }
}
