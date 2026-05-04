using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Crosshair siempre visible en el centro de la pantalla.
/// Cambia de color/tamaño según el contexto:
///   - Normal        → punto blanco pequeño
///   - Linterna ON   → círculo amarillo
///   - Objeto en mano → punto con indicador de lanzamiento
///   - Interactuable → punto verde con icono E
///
/// Setup: Canvas (Screen Space Overlay) → Image centrada → CrosshairHUD.cs
/// </summary>
public class CrosshairHUD : MonoBehaviour
{
    public static CrosshairHUD Instance { get; private set; }

    [Header("Referencias")]
    public Image crosshairImage;

    [Header("Sprites por estado")]
    public Sprite normalSprite;       // Punto normal
    public Sprite flashlightSprite;   // Círculo cuando la linterna está ON
    public Sprite throwSprite;        // Cuando lleva un objeto para lanzar
    public Sprite interactSprite;     // Cuando hay algo interactuable

    [Header("Colores por estado")]
    public Color normalColor      = Color.white;
    public Color flashlightColor  = new Color(1f, 0.9f, 0.2f, 1f);  // Amarillo
    public Color throwColor       = new Color(1f, 0.5f, 0f, 1f);     // Naranja
    public Color interactColor    = Color.green;

    [Header("Tamaños")]
    public float normalSize      = 8f;
    public float flashlightSize  = 16f;
    public float throwSize       = 12f;
    public float interactSize    = 14f;

    [Tooltip("Velocidad de transición entre estados")]
    public float transitionSpeed = 10f;

    // Estado actual
    public enum CrosshairState { Normal, Flashlight, Throw, Interact }
    private CrosshairState _currentState = CrosshairState.Normal;
    private float _targetSize;

    // ─── Init ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (crosshairImage == null)
            crosshairImage = GetComponent<Image>();

        _targetSize = normalSize;
        ApplyState(CrosshairState.Normal, instant: true);
    }

    // ─── Update ──────────────────────────────────────────────────────────────

    private void Update()
    {
        // Transición suave de tamaño
        RectTransform rt = crosshairImage.rectTransform;
        float current = rt.sizeDelta.x;
        float next    = Mathf.Lerp(current, _targetSize, Time.deltaTime * transitionSpeed);
        rt.sizeDelta  = new Vector2(next, next);
    }

    // ─── API pública ─────────────────────────────────────────────────────────

    public void SetState(CrosshairState state)
    {
        if (_currentState == state) return;
        _currentState = state;
        ApplyState(state);
    }

    private void ApplyState(CrosshairState state, bool instant = false)
    {
        Sprite targetSprite;
        Color  targetColor;

        switch (state)
        {
            case CrosshairState.Flashlight:
                targetSprite = flashlightSprite != null ? flashlightSprite : normalSprite;
                targetColor  = flashlightColor;
                _targetSize  = flashlightSize;
                break;

            case CrosshairState.Throw:
                targetSprite = throwSprite != null ? throwSprite : normalSprite;
                targetColor  = throwColor;
                _targetSize  = throwSize;
                break;

            case CrosshairState.Interact:
                targetSprite = interactSprite != null ? interactSprite : normalSprite;
                targetColor  = interactColor;
                _targetSize  = interactSize;
                break;

            default: // Normal
                targetSprite = normalSprite;
                targetColor  = normalColor;
                _targetSize  = normalSize;
                break;
        }

        if (crosshairImage == null) return;

        if (targetSprite != null)
            crosshairImage.sprite = targetSprite;

        crosshairImage.color = targetColor;

        if (instant)
        {
            RectTransform rt = crosshairImage.rectTransform;
            rt.sizeDelta = new Vector2(_targetSize, _targetSize);
        }
    }
}
