using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class Blur : MonoBehaviour
{
    [Tooltip("Blur Shader")]
    public Shader blurShader;

    [Tooltip("Blur Material")]
    public Material blurMaterial;

    [Tooltip("Default Blur Strength")]
    [Range(0f, 10f)]
    public float defaultRadius = 3.0f;

    [Tooltip("Iterations of Blur Calculation")]
    [Range(1, 6)]
    public int qualityIterations = 2;

    [Tooltip("Downsampling Factor")]
    [Range(0, 3)]
    public int filter = 1;

    [HideInInspector]
    public float radius = 0f; // Modified by PauseManager

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (blurShader == null || blurMaterial == null || radius <= 0f)
        {
            Graphics.Blit(source, destination);
            return;
        }

        float widthModification = 1.0f / (1.0f * (1 << filter));

        blurMaterial.SetVector("_Param", new Vector4(radius * widthModification, -radius * widthModification, 0f, 0f));
        source.filterMode = FilterMode.Bilinear;

        int renderTextureWidth = source.width >> filter;
        int renderTextureHeight = source.height >> filter;

        RenderTexture renderTexture = RenderTexture.GetTemporary(renderTextureWidth, renderTextureHeight, 0, source.format);
        renderTexture.filterMode = FilterMode.Bilinear;
        Graphics.Blit(source, renderTexture, blurMaterial, 0);

        for (int i = 0; i < qualityIterations; i++)
        {
            float iterationOffset = i * 1.0f;
            blurMaterial.SetVector("_Param", new Vector4(radius * widthModification + iterationOffset, -radius * widthModification - iterationOffset, 0.0f, 0.0f));

            RenderTexture temp = RenderTexture.GetTemporary(renderTextureWidth, renderTextureHeight, 0, source.format);
            temp.filterMode = FilterMode.Bilinear;
            Graphics.Blit(renderTexture, temp, blurMaterial, 1);
            RenderTexture.ReleaseTemporary(renderTexture);
            renderTexture = temp;

            temp = RenderTexture.GetTemporary(renderTextureWidth, renderTextureHeight, 0, source.format);
            temp.filterMode = FilterMode.Bilinear;
            Graphics.Blit(renderTexture, temp, blurMaterial, 2);
            RenderTexture.ReleaseTemporary(renderTexture);
            renderTexture = temp;
        }

        Graphics.Blit(renderTexture, destination);
        RenderTexture.ReleaseTemporary(renderTexture);
    }
}
