using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "TrackingBehavior", menuName = "Enemy Behaviors/Tracking")]
public class TrackingBehavior : EnemyBehavior
{
    [Header("Tracking Settings")]
    public float detectionRange = 6f;
    public float detectionAngle = 45f;
    public float rotationSpeed = 10f;
    public float returnSpeed = 3f;

    [Header("Line of Sight")]
    public string allowedObstacleTag = "SeeThrough";

    [Header("Notice Effects")]
    public AudioClip noticeSound;
    public float scalePulseAmount = 0.2f;
    public float scalePulseDuration = 0.2f;

    [Header("Projectile Firing")]
    public ProjectileBehavior projectileBehavior;
    public GameObject projectilePrefab;
    public float chargeDuration = 1.5f;
    public float chargeScaleAmount = 0.3f;
    public float fireRate = 2f;

    public override void Execute(Enemy enemy)
    {
        if (enemy.isDead || enemy.isBeingInhaled) return;

        // Runtime reference to the player (per enemy)
        Transform playerTransform = enemy.GetPlayerTransform();
        if (playerTransform == null) return;

        Vector2 toPlayer = playerTransform.position - enemy.transform.position;
        bool detected = IsPlayerDetected(enemy, toPlayer);

        HandleNoticeEffects(enemy, detected);

        // Rotation updates continuously
        HandleRotation(enemy, detected, toPlayer);

        // Fire loop is started independently per enemy
        if (detected && !enemy.IsFiring && projectilePrefab != null)
        {
            enemy.StartCoroutine(FireLoop(enemy, playerTransform));
        }
    }

    private bool IsPlayerDetected(Enemy enemy, Vector2 toPlayer)
    {
        if (toPlayer.magnitude > detectionRange) return false;
        if (Vector2.Angle(enemy.transform.up, toPlayer) > detectionAngle) return false;

        RaycastHit2D[] hits = Physics2D.RaycastAll(enemy.transform.position, toPlayer.normalized, toPlayer.magnitude);
        foreach (var hit in hits)
        {
            if (hit.collider == null || hit.collider.gameObject == enemy.gameObject) continue;
            if (hit.collider.CompareTag("Player")) break;
            if (hit.collider.CompareTag(allowedObstacleTag)) continue;
            return false;
        }

        return true;
    }

    private void HandleNoticeEffects(Enemy enemy, bool detected)
    {
        if (detected && !enemy.HasNoticedPlayer)
        {
            if (noticeSound != null && enemy.audioSource != null)
                enemy.audioSource.PlayOneShot(noticeSound);

            enemy.StartCoroutine(PulseScale(enemy.transform));
            enemy.HasNoticedPlayer = true;
        }
        else if (!detected)
        {
            enemy.HasNoticedPlayer = false;
        }
    }

    private void HandleRotation(Enemy enemy, bool detected, Vector2 toPlayer)
    {
        float targetAngle = detected ? Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg - 90f : enemy.initialZRotation;
        enemy.transform.rotation = Quaternion.Lerp(
            enemy.transform.rotation,
            Quaternion.Euler(0, 0, targetAngle),
            (detected ? rotationSpeed : returnSpeed) * Time.fixedDeltaTime
        );
    }

    private IEnumerator FireLoop(Enemy enemy, Transform playerTransform)
    {
        enemy.IsFiring = true;

        // Small random delay to desync multiple enemies
        yield return new WaitForSeconds(Random.Range(0f, 0.3f));

        Transform chargeParticles = enemy.transform.Find("Charge Particles");
        Transform fireParticles = enemy.transform.Find("Dirt Fired");

        while (!enemy.isDead)
        {
            Vector2 toPlayer = playerTransform.position - enemy.transform.position;
            bool detected = toPlayer.magnitude <= detectionRange && Vector2.Angle(enemy.transform.up, toPlayer) <= detectionAngle;

            if (!detected) break; // stop firing if player out of range

            // --- Charge animation ---
            if (chargeParticles != null) chargeParticles.gameObject.SetActive(true);

            Vector3 originalScale = enemy.transform.localScale;
            Vector3 targetScale = originalScale * (1f + chargeScaleAmount);
            float timer = 0f;
            while (timer < chargeDuration)
            {
                enemy.transform.localScale = Vector3.Lerp(originalScale, targetScale, timer / chargeDuration);
                timer += Time.deltaTime;
                yield return null;
            }
            enemy.transform.localScale = originalScale;
            if (chargeParticles != null) chargeParticles.gameObject.SetActive(false);

            // --- Fire projectile ---
            if (projectilePrefab != null)
            {
                GameObject projGO = Object.Instantiate(projectilePrefab, enemy.transform.position, enemy.transform.rotation);
                ProjectileMono projMono = projGO.GetComponent<ProjectileMono>();
                if (projMono != null && projectileBehavior != null)
                    projMono.Initialize(enemy.transform.up, projectileBehavior, enemy);
            }

            // Fire particles
            if (fireParticles != null)
            {
                fireParticles.gameObject.SetActive(true);
                yield return new WaitForSeconds(0.5f);
                fireParticles.gameObject.SetActive(false);
            }

            yield return new WaitForSeconds(fireRate);
        }

        enemy.IsFiring = false;
    }

    private IEnumerator PulseScale(Transform enemyTransform)
    {
        Vector3 originalScale = enemyTransform.localScale;
        Vector3 biggerScale = originalScale * (1f + scalePulseAmount);
        float half = scalePulseDuration / 2f;
        float timer = 0f;

        while (timer < half)
        {
            enemyTransform.localScale = Vector3.Lerp(originalScale, biggerScale, timer / half);
            timer += Time.deltaTime;
            yield return null;
        }

        timer = 0f;
        while (timer < half)
        {
            enemyTransform.localScale = Vector3.Lerp(biggerScale, originalScale, timer / half);
            timer += Time.deltaTime;
            yield return null;
        }

        enemyTransform.localScale = originalScale;
    }
}
