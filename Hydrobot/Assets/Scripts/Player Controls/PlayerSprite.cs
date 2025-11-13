using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSprite : MonoBehaviour
{
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Transform objectToMove;
    public PlayerRotation rotation;
    public SpriteRenderer hydroRend;
    private Sprite hydroNormal;
    private Sprite hydroFreeze;

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

        hydroRend.sprite = rotation.isFreeze ? hydroFreeze : hydroNormal;
    }

    public void SetHydroSprites(Sprite normalSprite, Sprite freezeSprite)
    {
        hydroNormal = normalSprite;
        hydroFreeze = freezeSprite;
        hydroRend.sprite = rotation.isFreeze ? hydroFreeze : hydroNormal;
    }
}
