using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target; // The GameObject to keep on screen
    [SerializeField] private Tilemap tilemap; // The Tilemap to determine bounds
    [SerializeField] private TileBase boundaryTile; // The tile used as the boundary marker

    [SerializeField] private float smoothTime = 0.2f; // Time for the camera to catch up to the target position
    [SerializeField] private float edgeBuffer = 0.4f; // Buffer percentage of the camera's view near the edges before it starts moving

    private Vector3 velocity = Vector3.zero;
    private Camera cam;
    private float leftBound, rightBound, topBound, bottomBound;

    void Start()
    {
        cam = Camera.main;
        CalculateTilemapBounds();
    }

    void LateUpdate()
    {
        Vector3 targetPosition = target.position;
        Vector3 viewportPosition = cam.WorldToViewportPoint(targetPosition);
        Vector3 desiredPosition = transform.position;

        if (viewportPosition.x < edgeBuffer)
        {
            desiredPosition.x = targetPosition.x - (cam.orthographicSize * cam.aspect);
        }
        else if (viewportPosition.x > 1f - edgeBuffer)
        {
            desiredPosition.x = targetPosition.x + (cam.orthographicSize * cam.aspect);
        }

        if (viewportPosition.y < edgeBuffer)
        {
            desiredPosition.y = targetPosition.y - cam.orthographicSize;
        }
        else if (viewportPosition.y > 1f - edgeBuffer)
        {
            desiredPosition.y = targetPosition.y + cam.orthographicSize;
        }

        float cameraHalfWidth = cam.orthographicSize * cam.aspect;
        float cameraHalfHeight = cam.orthographicSize;

        float clampedX = Mathf.Clamp(desiredPosition.x, leftBound + cameraHalfWidth, rightBound - cameraHalfWidth);
        float clampedY = Mathf.Clamp(desiredPosition.y, bottomBound + cameraHalfHeight, topBound - cameraHalfHeight);

        Vector3 clampedPosition = new Vector3(clampedX, clampedY, transform.position.z);
        transform.position = Vector3.SmoothDamp(transform.position, clampedPosition, ref velocity, smoothTime);
    }

    private void CalculateTilemapBounds()
    {
        if (tilemap == null || boundaryTile == null)
        {
            Debug.LogError("Tilemap or boundary tile not assigned!");
            return;
        }

        BoundsInt bounds = tilemap.cellBounds;
        bool boundsSet = false;

        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            if (tilemap.GetTile(pos) == boundaryTile)
            {
                Vector3 worldPos = tilemap.GetCellCenterWorld(pos);
                if (!boundsSet)
                {
                    leftBound = rightBound = worldPos.x;
                    bottomBound = topBound = worldPos.y;
                    boundsSet = true;
                }
                else
                {
                    leftBound = Mathf.Min(leftBound, worldPos.x);
                    rightBound = Mathf.Max(rightBound, worldPos.x);
                    bottomBound = Mathf.Min(bottomBound, worldPos.y);
                    topBound = Mathf.Max(topBound, worldPos.y);
                }
            }
        }

        if (!boundsSet)
        {
            Debug.LogError("No boundary tiles found in the tilemap!");
            return;
        }

        // Adjust bounds inward to ensure boundary tiles are never visible
        float tileSize = tilemap.cellSize.x; // Assuming square tiles
        leftBound += tileSize;
        rightBound -= tileSize;
        bottomBound += tileSize;
        topBound -= tileSize;
    }
}