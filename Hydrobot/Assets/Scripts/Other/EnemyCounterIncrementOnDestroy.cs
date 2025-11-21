using UnityEngine;

public class EnemyCounterIncrementOnDestroy : MonoBehaviour
{
    private EnemyCounter enemyCounter;

    private void Awake()
    {
        // Try to find the old EnemyCounter in the scene
        enemyCounter = FindObjectOfType<EnemyCounter>();

        if (enemyCounter == null)
        {
            Debug.Log("[EnemyCounterIncrementOnDestroy] No EnemyCounter found in the scene. This script will do nothing.");
        }
    }

    private void OnDestroy()
    {
        // Only increment if we found the counter
        if (enemyCounter != null)
        {
            // Access the HandleEnemyDestroyed function via reflection or make it public
            // Assuming the original EnemyCounter's HandleEnemyDestroyed is public
            enemyCounter.SendMessage("HandleEnemyDestroyed", SendMessageOptions.DontRequireReceiver);
        }
    }
}
