using UnityEngine;

public abstract class EnemyBehavior : ScriptableObject
{
    /// <summary>
    /// Called by the Enemy during FixedUpdate.
    /// </summary>
    public abstract void Execute(Enemy enemy);
}
