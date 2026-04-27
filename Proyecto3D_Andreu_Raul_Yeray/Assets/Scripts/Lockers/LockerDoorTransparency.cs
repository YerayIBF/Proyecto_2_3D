using UnityEngine;
using System.Collections;

/// <summary>
/// Coloca este script en el mismo GameObject que LockerInteractable.
/// Hace la puerta transparente cuando el jugador está dentro.
///
/// Funciona con URP (Universal Render Pipeline).
/// El script cambia el modo de renderizado del material automáticamente.
/// </summary>
public class LockerDoorTransparency : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El MeshRenderer de la puerta")]
    public Renderer doorRenderer;

    [Header("Transparencia")]
    [Tooltip("Alpha cuando el jugador está dentro (0 = invisible, 1 = opaco)")]
    [Range(0f, 1f)]
    public float hidingAlpha = 0.25f;

    [Tooltip("Velocidad de transición")]
    public float fadeSpeed = 3f;

    // Material instanciado (para no modificar el material original del proyecto)
    private Material _doorMaterial;
    private Color    _originalColor;
    private Color    _targetColor;
    private bool     _isFading = false;

    // ─── Init ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (doorRenderer == null)
        {
            // Intentar encontrar el renderer en el hijo "door"
            Transform doorTransform = transform.Find("door");
            if (doorTransform != null)
                doorRenderer = doorTransform.GetComponentInChildren<Renderer>();
        }

        if (doorRenderer == null)
        {
            Debug.LogWarning("[LockerDoor] No se encontró Renderer en la puerta.");
            return;
        }

        // Instanciar el material para no modificar el original
        _doorMaterial  = doorRenderer.material;
        _originalColor = _doorMaterial.color;
        _targetColor   = _originalColor;
    }

    // ─── API pública ─────────────────────────────────────────────────────────

    /// <summary>Llamado por LockerInteractable al entrar.</summary>
    public void SetTransparent()
    {
        if (_doorMaterial == null) return;
        EnableTransparency();
        _targetColor = new Color(_originalColor.r, _originalColor.g, _originalColor.b, hidingAlpha);
        StartFade();
    }

    /// <summary>Llamado por LockerInteractable al salir.</summary>
    public void SetOpaque()
    {
        if (_doorMaterial == null) return;
        _targetColor = _originalColor;
        StartFade();
        // Restaurar opaco cuando termine el fade
        StartCoroutine(RestoreOpaqueAfterFade());
    }

    // ─── Fade ────────────────────────────────────────────────────────────────

    private void StartFade()
    {
        if (!_isFading)
            StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        _isFading = true;
        while (!ColorApprox(_doorMaterial.color, _targetColor, 0.01f))
        {
            _doorMaterial.color = Color.Lerp(_doorMaterial.color, _targetColor, Time.deltaTime * fadeSpeed);
            yield return null;
        }
        _doorMaterial.color = _targetColor;
        _isFading = false;
    }

    private IEnumerator RestoreOpaqueAfterFade()
    {
        // Esperar a que el fade termine
        yield return new WaitUntil(() => !_isFading);
        DisableTransparency();
    }

    // ─── Modo de renderizado URP ──────────────────────────────────────────────

    private void EnableTransparency()
    {
        // URP: cambiar Surface Type a Transparent
        _doorMaterial.SetFloat("_Surface", 1f);              // 0 = Opaque, 1 = Transparent
        _doorMaterial.SetFloat("_Blend", 0f);                // Alpha blend
        _doorMaterial.SetFloat("_ZWrite", 0f);
        _doorMaterial.renderQueue = 3000;

        _doorMaterial.SetOverrideTag("RenderType", "Transparent");
        _doorMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        // Blend mode estándar para transparencia
        _doorMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _doorMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
    }

    private void DisableTransparency()
    {
        // URP: volver a Opaque
        _doorMaterial.SetFloat("_Surface", 0f);
        _doorMaterial.SetFloat("_ZWrite", 1f);
        _doorMaterial.renderQueue = -1;

        _doorMaterial.SetOverrideTag("RenderType", "Opaque");
        _doorMaterial.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");

        _doorMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        _doorMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
    }

    private bool ColorApprox(Color a, Color b, float threshold)
    {
        return Mathf.Abs(a.r - b.r) < threshold &&
               Mathf.Abs(a.g - b.g) < threshold &&
               Mathf.Abs(a.b - b.b) < threshold &&
               Mathf.Abs(a.a - b.a) < threshold;
    }
}