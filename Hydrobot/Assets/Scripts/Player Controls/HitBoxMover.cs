using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitBoxMover : MonoBehaviour
{
    [SerializeField] private Transform targetTransform; // The Transform whose position you want to match
    [SerializeField] private Transform objectToMove;     // The Transform you want to move to the target

    private void Start()
    {
        // Check if targetTransform and objectToMove are assigned
        if (targetTransform != null && objectToMove != null)
        {
            // Set the position of objectToMove to match targetTransform
            objectToMove.position = targetTransform.position;
        }
        else
        {
            Debug.LogWarning("TargetTransform or ObjectToMove is not assigned.");
        }
    }

    private void Update()
    {
        // Check if targetTransform and objectToMove are assigned
        if (targetTransform != null && objectToMove != null)
        {
            // Set the position of objectToMove to match targetTransform
            objectToMove.position = targetTransform.position;
        }
        else
        {
            Debug.LogWarning("TargetTransform or ObjectToMove is not assigned.");
        }
    }
}
