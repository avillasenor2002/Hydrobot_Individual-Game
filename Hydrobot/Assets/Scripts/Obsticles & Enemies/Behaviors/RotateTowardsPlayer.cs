using UnityEngine;

public class RotateTowardsPlayer : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 5f;

    private Transform playerTransform;

    private void Start()
    {
        PlayerRotation player = FindObjectOfType<PlayerRotation>();

        if (player != null)
            playerTransform = player.transform;
        else
            Debug.LogWarning("[RotateTowardsPlayer] No object with PlayerRotation found in scene.");
    }

    private void Update()
    {
        if (playerTransform == null) return;

        Vector2 direction = playerTransform.position - transform.position;
        float angle = Mathf.Atan2(-direction.y, -direction.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, angle);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}