using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSprite : MonoBehaviour
{
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Transform objectToMove;
    public PlayerRotation rotation;
    public SpriteRenderer hydroRend;

    [Header("Sprites")]
    private Sprite hydroNormalIdle;
    private Sprite hydroNormalMove;
    private Sprite hydroFreezeIdle;
    private Sprite hydroFreezeMove;

    [Header("Sprite angle")]
    [SerializeField] private float angleMultiplier = 15f;

    [Header("Triangle Indicator")]
    [SerializeField] private GameObject triangleIndicator;

    private void Start()
    {
        if (targetTransform != null && objectToMove != null)
        {
            objectToMove.position = targetTransform.position;
        }
        else
        {
            Debug.LogWarning("TargetTransform or ObjectToMove is not assigned.");
        }
    }

    private void Update()
    {
        if (targetTransform != null && objectToMove != null)
        {
            objectToMove.position = targetTransform.position;
        }

        // Choose sprite based on freeze and movement
        bool isMoving = rotation.GetMoveInput().sqrMagnitude > 0.01f;
        if (rotation.isFreeze)
            hydroRend.sprite = isMoving ? hydroFreezeMove : hydroFreezeIdle;
        else
            hydroRend.sprite = isMoving ? hydroNormalMove : hydroNormalIdle;

        // Flip sprite based on horizontal input
        float moveX = rotation.GetMoveInput().x;
        Vector3 scale = objectToMove.localScale;
        if (moveX < -0.01f) scale.x = -1f;
        else if (moveX > 0.01f) scale.x = 1f;
        objectToMove.localScale = scale;

        // Tilt sprite up/down based on vertical input
        float tiltAngle = rotation.GetMoveInput().y * angleMultiplier;
        if (scale.x < 0f) tiltAngle = -tiltAngle;
        objectToMove.localRotation = Quaternion.Euler(0, 0, tiltAngle);

        // Correctly update triangle indicator visibility every frame
        if (triangleIndicator != null)
        {
            triangleIndicator.SetActive(PlayerSettingsManager.IsBlueIndicatorVisible());
        }
    }

    public void SetHydroSprites(Sprite normalIdle, Sprite normalMove, Sprite freezeIdle, Sprite freezeMove)
    {
        hydroNormalIdle = normalIdle;
        hydroNormalMove = normalMove;
        hydroFreezeIdle = freezeIdle;
        hydroFreezeMove = freezeMove;
    }
}
