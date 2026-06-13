using UnityEngine;

/// <summary>
/// Convierte un LineRenderer en una línea discontinua (- - - - - -).
/// Genera la textura procedural al iniciar y la asigna al material.
///
/// Setup:
/// 1. Añade este componente al mismo GameObject que tiene el LineRenderer.
/// 2. Ajusta la densidad y color desde el Inspector.
///
/// Si quieres que las rayas estén más separadas, baja "dashes per meter".
/// Si quieres que sean más densas, súbelo.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class DashedLineRenderer : MonoBehaviour
{
    [Header("Color de las rayas")]
    public Color dashColor = Color.white;

    [Header("Densidad de las rayas")]
    [Tooltip("Cuántas rayas por metro de línea")]
    public float dashesPerMeter = 4f;

    [Tooltip("Porcentaje que ocupa la parte sólida (0 = solo huecos, 1 = solo sólido)")]
    [Range(0.1f, 0.9f)]
    public float dashRatio = 0.5f;

    [Header("Calidad de textura")]
    public int textureWidth = 64;
    public int textureHeight = 8;

    private LineRenderer _lineRenderer;
    private Material _material;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        SetupDashedMaterial();
    }

    private void SetupDashedMaterial()
    {
        // Crear textura procedural discontinua
        Texture2D dashTexture = CreateDashTexture();

        // Crear material con shader URP compatible
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        _material = new Material(shader);
        _material.mainTexture = dashTexture;
        _material.color = dashColor;

        // Configurar para transparencia
        if (_material.HasProperty("_Surface")) _material.SetFloat("_Surface", 1f); // 1 = Transparent
        if (_material.HasProperty("_Blend")) _material.SetFloat("_Blend", 0f);     // 0 = Alpha
        if (_material.HasProperty("_ZWrite")) _material.SetFloat("_ZWrite", 0f);
        _material.renderQueue = 3000;

        _lineRenderer.material = _material;

        // Ajustar tiling según el largo del LineRenderer
        UpdateTiling();
    }

    private Texture2D CreateDashTexture()
    {
        Texture2D tex = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;

        int dashEnd = Mathf.RoundToInt(textureWidth * dashRatio);

        for (int x = 0; x < textureWidth; x++)
        {
            // Parte sólida (blanco) | Parte transparente (alpha 0)
            Color color = x < dashEnd ? new Color(1, 1, 1, 1) : new Color(1, 1, 1, 0);

            for (int y = 0; y < textureHeight; y++)
            {
                tex.SetPixel(x, y, color);
            }
        }

        tex.Apply();
        return tex;
    }

    private void Update()
    {
        // Actualizar tiling cada frame para que las rayas se mantengan al mismo tamaño
        // independientemente del largo de la línea
        if (_material != null && _lineRenderer != null && _lineRenderer.positionCount > 1)
        {
            UpdateTiling();
        }
    }

    private void UpdateTiling()
    {
        if (_lineRenderer == null || _material == null) return;

        // Calcular largo total de la línea
        float totalLength = 0f;
        for (int i = 1; i < _lineRenderer.positionCount; i++)
        {
            totalLength += Vector3.Distance(
                _lineRenderer.GetPosition(i - 1),
                _lineRenderer.GetPosition(i));
        }

        float tiling = totalLength * dashesPerMeter;
        _material.mainTextureScale = new Vector2(tiling, 1f);
    }

    private void OnValidate()
    {
        if (Application.isPlaying && _material != null)
        {
            _material.color = dashColor;
            UpdateTiling();
        }
    }
}
