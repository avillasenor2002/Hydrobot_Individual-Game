using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileShoot : MonoBehaviour
{
    [System.Serializable]
    public class ProjectileMode
    {
        public string modeName;
        public GameObject projectilePrefab;
        public float projectileSpeed;
        public float waterLoss;

        // Updated: four sprite variants
        public Sprite hydroNormalIdle;
        public Sprite hydroNormalMove;
        public Sprite hydroFreezeIdle;
        public Sprite hydroFreezeMove;

        public float maxSpeed;
        public float maxDashSpeed;
    }

    [SerializeField] private List<ProjectileMode> projectileModes;
    [SerializeField] private Transform shootingPoint;
    [SerializeField] private WaterTank waterTotal;
    [SerializeField] private PlayerRotation player;
    [SerializeField] private PlayerSprite playerSprite;
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioSource audioSource;

    private int currentModeIndex = 0;

    private void Start()
    {
        if (shootingPoint == null)
        {
            shootingPoint = new GameObject("Shooting Point").transform;
            shootingPoint.parent = transform;
            shootingPoint.localPosition = Vector2.down;
        }

        UpdatePlayerSprite();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            SwitchMode();
        }
    }

    public void Shoot()
    {
        if (projectileModes.Count <= 0) return;

        ProjectileMode currentMode = projectileModes[currentModeIndex];

        if (waterTotal.currentWater >= 0)
        {
            waterTotal.currentWater -= currentMode.waterLoss;
            GameObject projectile = Instantiate(currentMode.projectilePrefab, shootingPoint.position, transform.rotation);

            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = transform.up * currentMode.projectileSpeed;
            }

            if (audioSource != null && shootSound != null)
            {
                audioSource.PlayOneShot(shootSound);
            }
        }
    }

    public void SwitchMode()
    {
        currentModeIndex = (currentModeIndex + 1) % projectileModes.Count;
        UpdatePlayerSprite();

        if (player != null && projectileModes.Count > 0)
        {
            ProjectileMode currentMode = projectileModes[currentModeIndex];
            player.maxSpeed = currentMode.maxSpeed;
            player.maxDashSpeed = currentMode.maxDashSpeed;
        }
    }

    private void UpdatePlayerSprite()
    {
        if (playerSprite != null && projectileModes.Count > 0)
        {
            ProjectileMode currentMode = projectileModes[currentModeIndex];

            playerSprite.SetHydroSprites(
                currentMode.hydroNormalIdle,
                currentMode.hydroNormalMove,
                currentMode.hydroFreezeIdle,
                currentMode.hydroFreezeMove
            );
        }
    }
}
