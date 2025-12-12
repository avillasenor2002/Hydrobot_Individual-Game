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

    // --- NEW ARM SPRITES ---
    [Header("Arm Sprite Renderers")]
    [SerializeField] private SpriteRenderer leftArmRend;
    [SerializeField] private SpriteRenderer rightArmRend;

    [Header("Arm Sprites")]
    [SerializeField] private Sprite armNormalIdle;
    [SerializeField] private Sprite armNormalMove;
    [SerializeField] private Sprite armFreezeIdle;
    [SerializeField] private Sprite armFreezeMove;
    // ------------------------

    [Header("Sprite angle")]
    [SerializeField] private float angleMultiplier = 15f;

    [Header("Triangle Indicator")]
    [SerializeField] private GameObject triangleIndicator;

    // --- NEW: object that moves to X = 0 when idle ---
    [Header("X Reset Object")]
    [SerializeField] private Transform armXObject;

    private float originalX;
    [SerializeField] private float xLerpSpeed = 100f;
    // -------------------------------------------------

    // --- NEW: particle effect to move X and angle ---
    [Header("Arm Particle Effect")]
    [SerializeField] private Transform armParticle;
    [SerializeField] private float particleLerpSpeed = 10f;

    private Vector3 originalParticlePos;
    private float originalParticleAngle;
    // -------------------------------------------------

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

        // store original X position
        if (armXObject != null)
            originalX = armXObject.localPosition.x;

        // store original particle position and angle
        if (armParticle != null)
        {
            originalParticlePos = armParticle.localPosition;
            originalParticleAngle = armParticle.localEulerAngles.z;
        }
    }

    private void Update()
    {
        if (targetTransform != null && objectToMove != null)
        {
            objectToMove.position = targetTransform.position;
        }

        bool isMoving = rotation.GetMoveInput().sqrMagnitude > 0.01f;

        // HYDRO SPRITES
        if (rotation.isFreeze)
            hydroRend.sprite = isMoving ? hydroFreezeMove : hydroFreezeIdle;
        else
            hydroRend.sprite = isMoving ? hydroNormalMove : hydroNormalIdle;

        // ARM SPRITES
        if (leftArmRend != null)
        {
            leftArmRend.sprite = rotation.isFreeze
                ? (isMoving ? armFreezeMove : armFreezeIdle)
                : (isMoving ? armNormalMove : armNormalIdle);
        }

        if (rightArmRend != null)
        {
            rightArmRend.sprite = rotation.isFreeze
                ? (isMoving ? armFreezeMove : armFreezeIdle)
                : (isMoving ? armNormalMove : armNormalIdle);
        }

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

        // Triangle indicator update
        if (triangleIndicator != null)
        {
            triangleIndicator.SetActive(PlayerSettingsManager.IsBlueIndicatorVisible());
        }

        // --- UPDATED: EASE THE X POSITION ONLY ---
        if (armXObject != null)
        {
            float targetX = isMoving ? originalX : 0f;
            Vector3 pos = armXObject.localPosition;
            pos.x = Mathf.Lerp(pos.x, targetX, Time.deltaTime * xLerpSpeed);
            armXObject.localPosition = pos;
        }

        // --- NEW: EASE PARTICLE EFFECT X AND ANGLE ---
        if (armParticle != null)
        {
            Vector3 targetPos = armParticle.localPosition;
            float targetAngle = armParticle.localEulerAngles.z;

            if (isMoving)
            {
                targetPos.x = originalParticlePos.x;
                targetPos.y = originalParticlePos.y;
                targetPos.z = originalParticlePos.z;
                targetAngle = originalParticleAngle;
            }
            else
            {
                targetPos.x = 0f;
                targetPos.y = originalParticlePos.y;
                targetPos.z = originalParticlePos.z;
                targetAngle = 0f;
            }

            armParticle.localPosition = Vector3.Lerp(armParticle.localPosition, targetPos, Time.deltaTime * particleLerpSpeed);

            Vector3 euler = armParticle.localEulerAngles;
            euler.z = Mathf.LerpAngle(euler.z, targetAngle, Time.deltaTime * particleLerpSpeed);
            armParticle.localEulerAngles = euler;
        }
        // ---------------------------------------------------
    }

    public void SetHydroSprites(Sprite normalIdle, Sprite normalMove, Sprite freezeIdle, Sprite freezeMove)
    {
        hydroNormalIdle = normalIdle;
        hydroNormalMove = normalMove;
        hydroFreezeIdle = freezeIdle;
        hydroFreezeMove = freezeMove;
    }

    public void SetArmSprites(Sprite normalIdle, Sprite normalMove, Sprite freezeIdle, Sprite freezeMove)
    {
        armNormalIdle = normalIdle;
        armNormalMove = normalMove;
        armFreezeIdle = freezeIdle;
        armFreezeMove = freezeMove;
    }
}
