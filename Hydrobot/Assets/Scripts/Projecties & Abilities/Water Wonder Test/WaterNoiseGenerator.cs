// Place this file inside an Editor/ folder (e.g. Assets/WonderWater/Editor/)
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Generates a tileable noise texture suitable for the _DistortionTex slot of WonderWater.shader.
/// Open via  Tools ▶ Wonder Water ▶ Generate Distortion Texture
/// </summary>
public class WaterNoiseGenerator : EditorWindow
{
    private int   _resolution   = 256;
    private float _frequency    = 4f;
    private int   _octaves      = 4;
    private float _persistence  = 0.5f;
    private float _lacunarity   = 2.1f;
    private string _savePath    = "Assets/WonderWater/DistortionNoise.png";

    [MenuItem("Tools/Wonder Water/Generate Distortion Texture")]
    public static void ShowWindow()
    {
        GetWindow<WaterNoiseGenerator>("Water Noise Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("Distortion Noise Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        _resolution  = EditorGUILayout.IntPopup("Resolution",  _resolution,
            new[]{"64","128","256","512","1024"}, new[]{64,128,256,512,1024});
        _frequency   = EditorGUILayout.Slider("Frequency",   _frequency,   1f, 12f);
        _octaves     = EditorGUILayout.IntSlider("Octaves",   _octaves,     1,  8);
        _persistence = EditorGUILayout.Slider("Persistence", _persistence, 0.1f, 0.9f);
        _lacunarity  = EditorGUILayout.Slider("Lacunarity",  _lacunarity,  1.5f, 3f);

        EditorGUILayout.Space();
        _savePath = EditorGUILayout.TextField("Save Path", _savePath);

        EditorGUILayout.Space();
        if (GUILayout.Button("Generate & Save"))
            GenerateAndSave();
    }

    void GenerateAndSave()
    {
        var tex = new Texture2D(_resolution, _resolution, TextureFormat.RGBA32, false);

        for (int y = 0; y < _resolution; y++)
        for (int x = 0; x < _resolution; x++)
        {
            float u = (float)x / _resolution;
            float v = (float)y / _resolution;

            float r = FractalNoise(u, v, _frequency,          _octaves, _persistence, _lacunarity, seed: 0);
            float g = FractalNoise(u, v, _frequency * 0.97f,  _octaves, _persistence, _lacunarity, seed: 137);

            tex.SetPixel(x, y, new Color(r, g, 0f, 1f));
        }

        tex.Apply();
        var bytes = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes(_savePath, bytes);
        AssetDatabase.Refresh();

        // Set import settings: no compression, linear, wrap mode repeat
        var importer = (TextureImporter)AssetImporter.GetAtPath(_savePath);
        if (importer != null)
        {
            importer.textureType    = TextureImporterType.Default;
            importer.sRGBTexture    = false;
            importer.wrapMode       = TextureWrapMode.Repeat;
            importer.filterMode     = FilterMode.Bilinear;
            importer.mipmapEnabled  = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        Debug.Log($"[WaterNoise] Saved to {_savePath}");
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Texture2D>(_savePath);
    }

    // ── Noise helpers ──────────────────────────────────────────────────────

    static float FractalNoise(float x, float y, float freq, int oct,
                               float pers, float lac, int seed)
    {
        float value = 0f, amplitude = 1f, maxVal = 0f;
        for (int i = 0; i < oct; i++)
        {
            value  += ValueNoiseTileable(x * freq + seed, y * freq + seed) * amplitude;
            maxVal += amplitude;
            amplitude *= pers;
            freq      *= lac;
        }
        return value / maxVal;
    }

    static float ValueNoiseTileable(float x, float y)
    {
        // Uses frac-based hash so it tiles at integer boundaries
        int ix = Mathf.FloorToInt(x);
        int iy = Mathf.FloorToInt(y);
        float fx = x - ix, fy = y - iy;
        float ux = fx * fx * (3 - 2 * fx);
        float uy = fy * fy * (3 - 2 * fy);

        float v00 = Hash(ix,   iy);
        float v10 = Hash(ix+1, iy);
        float v01 = Hash(ix,   iy+1);
        float v11 = Hash(ix+1, iy+1);

        return Mathf.Lerp(Mathf.Lerp(v00, v10, ux), Mathf.Lerp(v01, v11, ux), uy);
    }

    static float Hash(int x, int y)
    {
        int n = x * 1619 + y * 31337;
        n = (n << 13) ^ n;
        return 1f - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824f;
    }
}
#endif
