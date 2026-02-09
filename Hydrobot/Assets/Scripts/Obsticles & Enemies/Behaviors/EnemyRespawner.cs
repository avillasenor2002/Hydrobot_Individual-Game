using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyRespawner : MonoBehaviour
{
    [System.Serializable]
    public class RespawnEntry
    {
        public Enemy enemyInstance;
        public GameObject enemyPrefab;
        public float respawnDelay = 3f;
    }

    [Header("Enemies To Respawn")]
    [SerializeField] private List<RespawnEntry> enemies = new List<RespawnEntry>();

    [Header("Effects")]
    [SerializeField] private AudioClip respawnSound;
    [SerializeField] private ParticleSystem respawnEffect;

    private void Awake()
    {
        foreach (var entry in enemies)
        {
            if (entry.enemyInstance != null)
            {
                entry.enemyInstance.OnEnemyDeath += (deadEnemy) =>
                {
                    HandleEnemyDeath(entry, deadEnemy);
                };
            }
        }
    }

    private void HandleEnemyDeath(RespawnEntry entry, Enemy deadEnemy)
    {
        Vector2 deathPosition = deadEnemy.transform.position;
        StartCoroutine(RespawnCoroutine(entry, deathPosition));
    }

    private IEnumerator RespawnCoroutine(RespawnEntry entry, Vector2 spawnPosition)
    {
        yield return new WaitForSeconds(entry.respawnDelay);

        // Particle
        if (respawnEffect != null)
        {
            Instantiate(respawnEffect, spawnPosition, Quaternion.identity);
        }

        // Enemy
        Enemy newEnemy = null;
        if (entry.enemyPrefab != null)
        {
            GameObject obj = Instantiate(entry.enemyPrefab, spawnPosition, Quaternion.identity);
            newEnemy = obj.GetComponent<Enemy>();
        }

        // Sound
        if (respawnSound != null)
        {
            AudioSource.PlayClipAtPoint(respawnSound, spawnPosition);
        }

        // Re-register the new enemy so it can respawn again
        if (newEnemy != null)
        {
            entry.enemyInstance = newEnemy;
            newEnemy.OnEnemyDeath += (deadEnemy) =>
            {
                HandleEnemyDeath(entry, deadEnemy);
            };
        }
    }
}