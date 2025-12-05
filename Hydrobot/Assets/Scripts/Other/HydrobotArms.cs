using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HydrobotArms : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerRotation playerRotation; // Reference to player rotation script
    [SerializeField] private Transform objectToRotate;       // The object that should rotate (arms)
    [SerializeField] private SpriteRenderer spriteRenderer;  // Optional: directly toggle visibility

    [Header("Settings")]
    [SerializeField] private float rotationSpeed = 10f;      // Smooth rotation speed
    [SerializeField] private bool flipBasedOnHorizontal = false; // Optional: flip when aiming left/right

    private void Update()
    {
        if (playerRotation == null || objectToRotate == null) return;

        Vector2 moveInput = playerRotation.GetMoveInput();

        // Show/hide based on input
        bool hasInput = moveInput.sqrMagnitude > 0.01f;

        // Hide the arms if no input
        if (spriteRenderer != null)
            spriteRenderer.enabled = hasInput;
        else
            objectToRotate.gameObject.SetActive(hasInput);

        if (hasInput)
        {
            // Correct angle assuming object faces up (Y+)
            float angle = Mathf.Atan2(moveInput.x, -moveInput.y) * Mathf.Rad2Deg;

            // Smooth rotation
            Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
            objectToRotate.rotation = Quaternion.Slerp(objectToRotate.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // Optional flip
            if (flipBasedOnHorizontal)
            {
                Vector3 scale = objectToRotate.localScale;
                scale.x = moveInput.y < 0 ? -1f : 1f; // flip based on vertical aiming
                objectToRotate.localScale = scale;
            }
        }
    }
}
