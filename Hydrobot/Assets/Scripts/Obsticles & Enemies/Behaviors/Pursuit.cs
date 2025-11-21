using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "PursuitBehavior", menuName = "Enemy Behaviors/Pursuit")]
public class PursuitBehavior : EnemyBehavior
{
    [Header("Pursuit Settings")]
    public float speed = 2f;               // Movement speed of the enemy
    public float pursuitRange = 5f;        // Distance within which the enemy pursues the player
    public AudioClip noticeSound;          // Sound played once when the player is noticed
    public float scalePulseAmount = 0.2f;  // How much bigger the enemy grows temporarily
    public float scalePulseDuration = 0.2f; // How long the pulse animation lasts

    public override void Execute(Enemy enemy)
    {
        if (enemy.isDead || enemy.isBeingInhaled) return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        Vector2 direction = player.transform.position - enemy.transform.position;
        float distance = direction.magnitude;

        if (distance <= pursuitRange)
        {
            // Play notice sound and pulse only once per enemy
            if (!enemy.HasNoticedPlayer)
            {
                if (noticeSound != null && enemy.audioSource != null)
                    enemy.audioSource.PlayOneShot(noticeSound);

                if (!enemy.IsFiring) // reuse IsFiring as "isPulsing"
                {
                    enemy.IsFiring = true;
                    enemy.StartCoroutine(PulseScale(enemy));
                }

                enemy.HasNoticedPlayer = true;
            }

            direction.Normalize();
            enemy.transform.position += (Vector3)(direction * speed * Time.fixedDeltaTime);
        }
        else
        {
            // Reset notice state if player leaves range
            enemy.HasNoticedPlayer = false;
        }
    }

    private IEnumerator PulseScale(Enemy enemy)
    {
        Transform enemyTransform = enemy.transform;
        Vector3 originalScale = enemyTransform.localScale;
        Vector3 targetScale = originalScale * (1f + scalePulseAmount);

        float timer = 0f;

        // Grow phase
        while (timer < scalePulseDuration / 2f)
        {
            enemyTransform.localScale = Vector3.Lerp(originalScale, targetScale, timer / (scalePulseDuration / 2f));
            timer += Time.deltaTime;
            yield return null;
        }

        // Shrink phase
        timer = 0f;
        while (timer < scalePulseDuration / 2f)
        {
            enemyTransform.localScale = Vector3.Lerp(targetScale, originalScale, timer / (scalePulseDuration / 2f));
            timer += Time.deltaTime;
            yield return null;
        }

        enemyTransform.localScale = originalScale;
        enemy.IsFiring = false; // pulse finished
    }
}
