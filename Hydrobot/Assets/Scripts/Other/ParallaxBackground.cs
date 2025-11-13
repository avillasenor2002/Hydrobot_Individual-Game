using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ParallaxBackground : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;   // The camera transform to follow
    [SerializeField] private Vector2 parallaxEffectMultiplier;  // The multiplier for the parallax effect
    private Vector3 lastCameraPosition;

    private void Start()
    {
        lastCameraPosition = cameraTransform.position;  // Store the initial position of the camera
    }

    private void LateUpdate()
    {
        Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;  // Calculate the movement of the camera
        transform.position += new Vector3(deltaMovement.x * parallaxEffectMultiplier.x, deltaMovement.y * parallaxEffectMultiplier.y, 0);  // Apply parallax effect
        lastCameraPosition = cameraTransform.position;  // Update the last camera position
    }
}