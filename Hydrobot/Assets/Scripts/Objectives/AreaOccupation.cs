using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreaOccupation : MonoBehaviour
{
    [SerializeField] private GameObject targetObject;
    [SerializeField] private float requiredTime = 3f;
    [SerializeField] private Color targetColor = Color.red;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private ObjectiveActivation otherScript;

    private float occupationTimer = 0f;
    private Color initialColor;
    private bool isOccupied = false;

    private void Start()
    {

        initialColor = spriteRenderer.color;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject == targetObject)
        {
            isOccupied = true;
            occupationTimer += Time.deltaTime;

            float t = Mathf.Clamp01(occupationTimer / requiredTime);
            spriteRenderer.color = Color.Lerp(initialColor, targetColor, t);

            if (occupationTimer >= requiredTime)
            {
                otherScript.nextObjective();
                occupationTimer = 0f;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject == targetObject)
        {
            isOccupied = false;
            occupationTimer = 0f;
            spriteRenderer.color = initialColor;
        }
    }
}