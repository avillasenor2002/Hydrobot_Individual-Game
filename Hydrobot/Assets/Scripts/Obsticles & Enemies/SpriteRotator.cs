using UnityEngine;

public class SpriteRotator : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 90f; // Degrees per second

    void Update()
    {
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }
}
