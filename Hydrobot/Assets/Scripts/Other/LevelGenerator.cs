using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    public Texture2D levelImage;
    public GameObject groundPrefab;
    public GameObject backgroundPrefab;
    public float tileSize = 1.0f;

    // Define colors for ground and background elements
    private Color groundColor = new Color(0.55f, 0.27f, 0.07f); // Hex: #8C4412 (Brown for ground)
    private Color backgroundColor = new Color(0.0f, 0.5f, 1.0f); // Hex: #0080FF (Blue for background)
    private float colorTolerance = 0.1f; // Allow small variations in color values

    void Start()
    {
        if (levelImage != null)
        {
            Texture2D readableTexture = MakeTextureReadable(levelImage);
            GenerateLevel(readableTexture);
        }
        else
        {
            Debug.LogError("Level image not assigned!");
        }
    }

    void GenerateLevel(Texture2D image)
    {
        for (int x = 0; x < image.width; x++)
        {
            for (int y = 0; y < image.height; y++)
            {
                Color pixelColor = image.GetPixel(x, y);
                Vector2 position = new Vector2(x * tileSize, y * tileSize);

                if (IsColorMatch(pixelColor, groundColor)) // Ground
                {
                    Instantiate(groundPrefab, position, Quaternion.identity);
                }
                else if (IsColorMatch(pixelColor, backgroundColor)) // Background
                {
                    Instantiate(backgroundPrefab, position, Quaternion.identity);
                }
                // White spaces remain blank
            }
        }
    }

    Texture2D MakeTextureReadable(Texture2D source)
    {
        RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height);
        Graphics.Blit(source, rt);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D readableTexture = new Texture2D(source.width, source.height);
        readableTexture.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
        readableTexture.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        return readableTexture;
    }

    bool IsColorMatch(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < colorTolerance &&
               Mathf.Abs(a.g - b.g) < colorTolerance &&
               Mathf.Abs(a.b - b.b) < colorTolerance;
    }
}
