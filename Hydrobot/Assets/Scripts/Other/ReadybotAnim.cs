using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReadybotAnim : MonoBehaviour
{
    [Header("Rotation")]
    [Tooltip("Degrees per second")]
    public float rotationSpeed = 45f;

    [Header("Respawn")]
    [Tooltip("Prefab to spawn when this object is destroyed")]
    public GameObject respawnPrefab;

    [Tooltip("Optional: Spawn at a custom position. If false, spawns where this object was.")]
    public bool useCustomSpawnPoint = false;
    public Transform customSpawnPoint;

    private bool hasSpawned = false;

    void Update()
    {
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }

    private void OnDestroy()
    {
        // Prevent double-spawning during scene unload
        if (hasSpawned) return;
        hasSpawned = true;

        if (respawnPrefab == null) return;

        Vector3 spawnPos = transform.position;
        Quaternion spawnRot = transform.rotation;

        if (useCustomSpawnPoint && customSpawnPoint != null)
        {
            spawnPos = customSpawnPoint.position;
            spawnRot = customSpawnPoint.rotation;
        }

        Instantiate(respawnPrefab, spawnPos, spawnRot);
    }
}