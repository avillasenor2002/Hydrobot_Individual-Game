using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "TrackingBehavior", menuName = "Enemy Behaviors/Tracking")]
public class TrackingBehavior : EnemyBehavior
{
    [Header("Tracking Settings")]
    public float detectionRange = 6f;
    public float detectionAngle = 45f;
    public float rotationSpeed = 6f;
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

    private Transform playerTransform;

    public override void Execute(Enemy enemy)
    {
        if (enemy.isDead || enemy.isBeingInhaled) return;

        // Find player if not assigned
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj == null) return;
            playerTransform = playerObj.transform;
        }

        Vector2 toPlayer = playerTransform.position - enemy.transform.position;
        float distance = toPlayer.magnitude;
        bool detectedThisFrame = false;

        // ------------------------------
        // Player detection (cone + LOS)
        // ------------------------------
        if (distance <= detectionRange)
        {
            Vector2 forward = enemy.transform.up;
            float angle = Vector2.Angle(forward, toPlayer);

            if (angle <= detectionAngle)
            {
                RaycastHit2D[] hits = Physics2D.RaycastAll(enemy.transform.position, toPlayer.normalized, distance);
                bool blocked = false;

                foreach (var hit in hits)
                {
                    if (hit.collider == null) continue;
                    if (hit.collider.gameObject == enemy.gameObject) continue;
                    if (hit.collider.CompareTag("Player")) break;
                    if (hit.collider.CompareTag(allowedObstacleTag)) continue;

                    blocked = true;
                    break;
                }

                if (!blocked)
                    detectedThisFrame = true;
            }
        }

        // ------------------------------
        // Notice effects
        // ------------------------------
        if (detectedThisFrame && !enemy.HasNoticedPlayer)
        {
            if (noticeSound != null && enemy.audioSource != null)
                enemy.audioSource.PlayOneShot(noticeSound);

            enemy.StartCoroutine(PulseScale(enemy.transform));
            enemy.HasNoticedPlayer = true;
        }
        else if (!detectedThisFrame)
        {
            enemy.HasNoticedPlayer = false;
        }

        // ------------------------------
        // Rotation
        // ------------------------------
        if (detectedThisFrame)
        {
            Vector3 dir = playerTransform.position - enemy.transform.position;
            float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            enemy.transform.rotation = Quaternion.Lerp(enemy.transform.rotation,
                                                      Quaternion.Euler(0, 0, targetAngle),
                                                      rotationSpeed * Time.fixedDeltaTime);

            // Start firing loop per enemy
            if (!enemy.IsFiring && projectilePrefab != null)
            {
                enemy.StartCoroutine(FireLoop(enemy));
            }
        }
        else
        {
            // Smooth return to initial rotation
            enemy.transform.rotation = Quaternion.Lerp(enemy.transform.rotation,
                                                      Quaternion.Euler(0, 0, enemy.initialZRotation),
                                                      returnSpeed * Time.fixedDeltaTime);
        }
    }

    // ------------------------------
    // Repeating firing coroutine
    // ------------------------------
    private IEnumerator FireLoop(Enemy enemy)
    {
        enemy.IsFiring = true;

        Transform chargeParticles = enemy.transform.Find("Charge Particles");
        Transform fireParticles = enemy.transform.Find("Dirt Fired");

        while (!enemy.isDead && Vector2.Distance(playerTransform.position, enemy.transform.position) <= detectionRange)
        {
            // --- Charge ---
            if (chargeParticles != null)
                chargeParticles.gameObject.SetActive(true);

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

            if (chargeParticles != null)
                chargeParticles.gameObject.SetActive(false);

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

    // ------------------------------
    // Pulse scale effect
    // ------------------------------
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
