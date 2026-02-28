using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SpongeBehavior", menuName = "Enemy Behaviors/Sponge")]
public class SpongeBehavior : EnemyBehavior
{
    [Header("Movement")]
    public float speed = 1.5f;
    public float pursuitRange = 6f;

    [Header("Notice Feedback")]
    public AudioClip noticeSound;
    public float noticePulseScale = 0.25f;
    public float noticePulseDuration = 0.25f;

    [Header("Growth Settings")]
    public int hpLossPerGrowth = 3;
    public float growthPerStep = 0.15f;

    [Header("Growth Feedback")]
    public AudioClip growthSound;
    public float growthPulseScale = 0.18f;

    [Header("Growth Animation")]
    public float growthAnimDuration = 0.35f;
    public float overshootPercent = 0.1f;

    [Header("Replacement")]
    public GameObject replacementPrefab;

    [Header("Color Shift")]
    [Range(0f, 1f)]
    public float blueShiftPerStep = 0.1f;

    [Header("Squish Settings")]
    public float idleSquishAmount = 0.02f;
    public float moveSquishAmount = 0.05f;
    public float idleSquishSpeed = 3f;
    public float moveSquishSpeed = 8f;

    private Dictionary<Enemy, int> startingHealth = new();
    private Dictionary<Enemy, int> lastGrowthStep = new();
    private Dictionary<Enemy, bool> isGrowing = new();
    private Dictionary<Enemy, bool> hasNoticed = new();

    private Dictionary<Enemy, List<Color>> originalColors = new();
    private Dictionary<Enemy, List<Vector3>> originalSpriteScales = new();

    public override void Execute(Enemy enemy)
    {
        if (enemy.isDead || enemy.isBeingInhaled) return;

        InitializeEnemy(enemy);
        HandleGrowth(enemy);

        bool isMoving = HandleMovement(enemy);
        HandleSquish(enemy, isMoving);
    }

    // ---------- INITIALIZATION ----------

    private void InitializeEnemy(Enemy enemy)
    {
        if (startingHealth.ContainsKey(enemy)) return;

        startingHealth[enemy] = enemy.health;
        lastGrowthStep[enemy] = 0;
        isGrowing[enemy] = false;
        hasNoticed[enemy] = false;

        SpriteRenderer[] sprites = enemy.GetComponentsInChildren<SpriteRenderer>();

        List<Color> colors = new();
        List<Vector3> scales = new();

        foreach (var s in sprites)
        {
            colors.Add(s.color);
            scales.Add(s.transform.localScale);
        }

        originalColors[enemy] = colors;
        originalSpriteScales[enemy] = scales;

        enemy.OnEnemyDeath += OnEnemyDeath;
    }

    // ---------- MOVEMENT + NOTICE ----------

    private bool HandleMovement(Enemy enemy)
    {
        Transform player = enemy.GetPlayerTransform();
        if (player == null) return false;

        Vector2 direction = player.position - enemy.transform.position;
        float distance = direction.magnitude;

        if (distance <= pursuitRange)
        {
            if (!hasNoticed[enemy])
            {
                hasNoticed[enemy] = true;

                if (noticeSound && enemy.audioSource)
                    enemy.audioSource.PlayOneShot(noticeSound);

                enemy.StartCoroutine(Pulse(enemy, noticePulseScale, noticePulseDuration));
            }

            direction.Normalize();
            enemy.transform.position += (Vector3)(direction * speed * Time.fixedDeltaTime);
            return true;
        }
        else
        {
            hasNoticed[enemy] = false;
        }

        return false;
    }

    // ---------- GROWTH ----------

    private void HandleGrowth(Enemy enemy)
    {
        int hpLost = startingHealth[enemy] - enemy.health;
        if (hpLost < 0) hpLost = 0;

        int growthStep = hpLost / hpLossPerGrowth;

        if (growthStep > lastGrowthStep[enemy] && !isGrowing[enemy])
        {
            int steps = growthStep - lastGrowthStep[enemy];
            float multiplier = 1f + (growthPerStep * steps);

            enemy.StartCoroutine(AnimateGrowth(enemy, multiplier));

            ApplyBlueShift(enemy, growthStep);

            if (growthSound && enemy.audioSource)
                enemy.audioSource.PlayOneShot(growthSound);

            enemy.StartCoroutine(Pulse(enemy, growthPulseScale, 0.2f));

            lastGrowthStep[enemy] = growthStep;
        }
    }

    private IEnumerator AnimateGrowth(Enemy enemy, float multiplier)
    {
        isGrowing[enemy] = true;

        Transform t = enemy.transform;

        Vector3 start = t.localScale;
        Vector3 target = start * multiplier;
        Vector3 overshoot = target * (1f + overshootPercent);
        Vector3 undershoot = target * (1f - overshootPercent * 0.5f);

        float time = 0f;

        while (time < growthAnimDuration * 0.5f)
        {
            t.localScale = Vector3.Lerp(start, overshoot, Ease(time / (growthAnimDuration * 0.5f)));
            time += Time.deltaTime;
            yield return null;
        }

        time = 0f;

        while (time < growthAnimDuration * 0.3f)
        {
            t.localScale = Vector3.Lerp(overshoot, undershoot, Ease(time / (growthAnimDuration * 0.3f)));
            time += Time.deltaTime;
            yield return null;
        }

        time = 0f;

        while (time < growthAnimDuration * 0.2f)
        {
            t.localScale = Vector3.Lerp(undershoot, target, Ease(time / (growthAnimDuration * 0.2f)));
            time += Time.deltaTime;
            yield return null;
        }

        t.localScale = target;
        isGrowing[enemy] = false;
    }

    private float Ease(float t) => t * t * (3f - 2f * t);

    // ---------- COLOR ----------

    private void ApplyBlueShift(Enemy enemy, int growthStep)
    {
        var sprites = enemy.GetComponentsInChildren<SpriteRenderer>();
        var baseColors = originalColors[enemy];

        float blueAmount = Mathf.Clamp01(growthStep * blueShiftPerStep);

        for (int i = 0; i < sprites.Length && i < baseColors.Count; i++)
        {
            Color baseColor = baseColors[i];

            sprites[i].color = Color.Lerp(
                baseColor,
                new Color(baseColor.r * 0.5f, baseColor.g * 0.5f, 1f, baseColor.a),
                blueAmount
            );
        }
    }

    // ---------- SQUISH ----------

    private void HandleSquish(Enemy enemy, bool moving)
    {
        var sprites = enemy.GetComponentsInChildren<SpriteRenderer>();
        var baseScales = originalSpriteScales[enemy];

        float amount = moving ? moveSquishAmount : idleSquishAmount;
        float speedVal = moving ? moveSquishSpeed : idleSquishSpeed;

        float wave = Mathf.Sin(Time.time * speedVal) * amount;

        for (int i = 0; i < sprites.Length && i < baseScales.Count; i++)
        {
            Vector3 baseScale = baseScales[i];
            sprites[i].transform.localScale =
                new Vector3(baseScale.x + wave, baseScale.y - wave, baseScale.z);
        }
    }

    // ---------- PULSE ----------

    private IEnumerator Pulse(Enemy enemy, float scaleAmount, float duration)
    {
        Transform t = enemy.transform;
        Vector3 original = t.localScale;
        Vector3 enlarged = original * (1f + scaleAmount);

        float timer = 0f;

        while (timer < duration / 2f)
        {
            t.localScale = Vector3.Lerp(original, enlarged, timer / (duration / 2f));
            timer += Time.deltaTime;
            yield return null;
        }

        timer = 0f;

        while (timer < duration / 2f)
        {
            t.localScale = Vector3.Lerp(enlarged, original, timer / (duration / 2f));
            timer += Time.deltaTime;
            yield return null;
        }

        t.localScale = original;
    }

    // ---------- DEATH ----------

    private void OnEnemyDeath(Enemy enemy)
    {
        if (replacementPrefab != null)
            GameObject.Instantiate(replacementPrefab, enemy.transform.position, enemy.transform.rotation);

        startingHealth.Remove(enemy);
        lastGrowthStep.Remove(enemy);
        originalColors.Remove(enemy);
        originalSpriteScales.Remove(enemy);
        isGrowing.Remove(enemy);
        hasNoticed.Remove(enemy);

        enemy.OnEnemyDeath -= OnEnemyDeath;
    }
}