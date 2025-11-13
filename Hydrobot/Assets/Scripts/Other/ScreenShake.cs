using System.Collections;
using UnityEngine;

public class ScreenShake : MonoBehaviour
{
    private Vector3 originalPosition;

    [SerializeField] private float shakeIntensity = 0.5f;
    [SerializeField] private float shakeDuration = 0.5f;

    private CameraFollow2D cameraFollow;

    void Start()
    {
        originalPosition = transform.position;
        cameraFollow = GetComponent<CameraFollow2D>();  // Get the CameraFollow2D script
    }

    public void ShakeCamera()
    {
        StartCoroutine(ShakeCoroutine());
    }

    private IEnumerator ShakeCoroutine()
    {
        float elapsed = 0f;
        Vector3 originalCameraPosition = transform.position;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-shakeIntensity, shakeIntensity);
            float y = Random.Range(-shakeIntensity, shakeIntensity);

            // Apply shake to the camera position
            transform.position = originalCameraPosition + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Reset camera position after shaking
        transform.position = originalCameraPosition;
    }
}
