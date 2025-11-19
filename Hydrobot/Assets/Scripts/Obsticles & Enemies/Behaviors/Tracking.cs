using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "TrackingBehavior", menuName = "Enemy Behaviors/Tracking")]
public class TrackingBehavior : EnemyBehavior
{
    [Header("Detection")]
    public float detectionRange = 6f;
    public float noticeBufferDistance = 1.5f; // extra “stickiness” range
    public float loseTargetDelay = 0.3f;
    public string allowedObstacleTag = "SeeThrough";

    [Header("Rotation")]
    public float rotationSpeed = 6f;
    public float returnSpeed = 3f;

    [Header("Notice Effects")]
    public AudioClip noticeSound;
    public float scalePulseAmount = 0.2f;
    public float scalePulseDuration = 0.2f;

    [Header("Firing")]
    public ProjectileBehavior projectileBehavior;
    public GameObject projectilePrefab;
    public float chargeDuration = 1.5f;
    public float chargeScaleAmount = 0.3f;
    public float fireRate = 2f;

    private Transform playerTransform;

    public override void Execute(Enemy enemy)
    {
        if (!enemy || enemy.isDead || enemy.isBeingInhaled) return;

        // cache player
        if (playerTransform == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (!p) return;
            playerTransform = p.transform;
        }

        // distance check only
        Vector2 toPlayer = playerTransform.position - enemy.transform.position;
        float dist = toPlayer.magnitude;

        bool insideDetectRange = dist <= detectionRange;
        bool insideStickyRange = dist <= detectionRange + noticeBufferDistance;

        bool detectedThisFrame = false;

        // -------- RANGE BASED DETECTION --------
        if (insideDetectRange && HasLineOfSight(enemy, toPlayer.normalized, dist))
            detectedThisFrame = true;

        // -------- STICKY TRACKING --------
        if (detectedThisFrame)
        {
            if (!enemy.HasNoticedPlayer)
            {
                if (noticeSound && enemy.audioSource)
                    enemy.audioSource.PlayOneShot(noticeSound);

                enemy.StartCoroutine(PulseScale(enemy.transform));
            }

            enemy.HasNoticedPlayer = true;
            enemy.noticeCooldown = loseTargetDelay;
        }
        else
        {
            if (enemy.HasNoticedPlayer && insideStickyRange)
            {
                // player is out of LOS but close enough – still track a bit
                enemy.noticeCooldown -= Time.deltaTime;
                if (enemy.noticeCooldown > 0) { }
                else enemy.HasNoticedPlayer = false;
            }
            else
            {
                enemy.HasNoticedPlayer = false;
            }
        }

        // -------- ROTATION LOGIC --------
        if (enemy.HasNoticedPlayer)
        {
            RotateTowardPlayer(enemy);

            if (!enemy.IsFiring && projectilePrefab)
                enemy.StartCoroutine(FireLoop(enemy));
        }
        else
        {
            enemy.transform.rotation = Quaternion.Lerp(
                enemy.transform.rotation,
                Quaternion.Euler(0, 0, enemy.initialZRotation),
                returnSpeed * Time.deltaTime
            );
        }
    }

    // ----------- LOS CHECK -----------
    private bool HasLineOfSight(Enemy enemy, Vector2 dir, float distance)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(enemy.transform.position, dir, distance);

        foreach (var h in hits)
        {
            if (!h.collider) continue;
            if (h.collider.gameObject == enemy.gameObject) continue;
            if (h.collider.isTrigger) continue;

            if (h.collider.CompareTag("Player"))
                return true;

            if (h.collider.CompareTag(allowedObstacleTag))
                continue;

            return false;
        }

        return false;
    }

    // ----------- ROTATION TOWARD PLAYER -----------
    private void RotateTowardPlayer(Enemy enemy)
    {
        Vector3 dir = playerTransform.position - enemy.transform.position;
        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

        enemy.transform.rotation = Quaternion.Lerp(
            enemy.transform.rotation,
            Quaternion.Euler(0, 0, targetAngle),
            rotationSpeed * Time.deltaTime
        );
    }

    // ----------- FIRING LOOP -----------
    private IEnumerator FireLoop(Enemy enemy)
    {
        enemy.IsFiring = true;

        Transform chargeT = enemy.transform.Find("Charge Particles");
        Transform fireT = enemy.transform.Find("Dirt Fired");

        ParticleSystem chargePS = chargeT ? chargeT.GetComponent<ParticleSystem>() : null;
        ParticleSystem firePS = fireT ? fireT.GetComponent<ParticleSystem>() : null;

        while (!enemy.isDead && enemy.HasNoticedPlayer)
        {
            // charge
            if (chargeT) chargeT.gameObject.SetActive(true);
            if (chargePS)
            {
                chargePS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                chargePS.Play();
            }

            Vector3 original = enemy.transform.localScale;
            Vector3 target = original * (1f + chargeScaleAmount);

            float t = 0f;
            while (t < chargeDuration)
            {
                enemy.transform.localScale = Vector3.Lerp(original, target, t / chargeDuration);
                t += Time.deltaTime;
                yield return null;
            }

            enemy.transform.localScale = original;

            if (chargePS) chargePS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (chargeT) chargeT.gameObject.SetActive(false);

            // fire
            if (projectilePrefab)
            {
                GameObject proj = Object.Instantiate(projectilePrefab, enemy.transform.position, enemy.transform.rotation);
                ProjectileMono pm = proj.GetComponent<ProjectileMono>();
                if (pm && projectileBehavior)
                    pm.Initialize(enemy.transform.up, projectileBehavior, enemy);
            }

            if (fireT)
            {
                fireT.gameObject.SetActive(true);
                if (firePS)
                {
                    firePS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    firePS.Play();
                }
            }

            yield return new WaitForSeconds(0.4f);

            if (firePS) firePS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (fireT) fireT.gameObject.SetActive(false);

            // wait until next shot
            float timer = 0f;
            while (timer < fireRate && enemy.HasNoticedPlayer && !enemy.isDead)
            {
                timer += Time.deltaTime;
                yield return null;
            }
        }

        enemy.IsFiring = false;
    }

    // ----------- PULSE EFFECT -----------
    private IEnumerator PulseScale(Transform t)
    {
        Vector3 orig = t.localScale;
        Vector3 bigger = orig * (1f + scalePulseAmount);

        float half = scalePulseDuration / 2f;
        float timer = 0f;

        while (timer < half)
        {
            t.localScale = Vector3.Lerp(orig, bigger, timer / half);
            timer += Time.deltaTime;
            yield return null;
        }

        timer = 0f;
        while (timer < half)
        {
            t.localScale = Vector3.Lerp(bigger, orig, timer / half);
            timer += Time.deltaTime;
            yield return null;
        }

        t.localScale = orig;
    }
}
