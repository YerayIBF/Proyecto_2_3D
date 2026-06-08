using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD de la linterna.
/// - Usa CanvasGroup para ocultarse/mostrarse (no SetActive)
/// - Esto permite que el script siga corriendo aunque el HUD esté "invisible"
///
/// LAYOUT (esquina inferior izquierda):
///   [icono linterna] [════ barra ════] 87%
///                    [icono pila] x2
/// </summary>
public class FlashlightHUD : MonoBehaviour
{
    [Header("Canvas Group del HUD")]
    [Tooltip("CanvasGroup que controla la visibilidad. Asignar el CanvasGroup del propio HUD.")]
    public CanvasGroup canvasGroup;

    [Header("Icono de la linterna")]
    [Tooltip("Image del icono de la linterna (se atenúa cuando la linterna está apagada)")]
    public Image flashlightIcon;
    [Tooltip("Alpha del icono cuando la linterna está apagada (0-1). Por defecto 200/255 ≈ 0.78")]
    [Range(0f, 1f)]
    public float iconAlphaOff = 0.4f;

    [Header("Barra de batería")]
    public Slider batterySlider;
    public Image batterySliderFill;

    [Header("Porcentaje")]
    public TextMeshProUGUI batteryPercentText;
    public bool showPercentage = true;

    [Header("Contador de pilas (x2)")]
    public TextMeshProUGUI batteryCountText;
    public Image batteryCountIcon;
    public string countFormat = "x{0}";

    [Header("Gradiente de colores de la barra")]
    public Color colorFull = new Color(0.3f, 1f, 0.3f);
    public Color colorMid  = new Color(1f, 0.85f, 0.2f);
    public Color colorLow  = new Color(1f, 0.3f, 0.3f);

    [Header("Contador de pilas - colores")]
    public Color countNormalColor = Color.white;
    public Color countEmptyColor  = new Color(1f, 0.4f, 0.4f);

    [Header("Comportamiento")]
    public bool hideUntilFlashlightPickup = true;

    [Header("Delay inicial (cinemática)")]
    [Tooltip("Si está marcado, el HUD permanece oculto durante X segundos al empezar")]
    public bool hideAtStart = false;
    public float initialHideDuration = 5f;

    private bool _initialHideFinished = false;
    private FlashlightSystem _flashlight;

    // ─── Init ────────────────────────────────────────────────────────────────

    private void Start()
    {
        _flashlight = FindFirstObjectByType<FlashlightSystem>();

        if (_flashlight != null)
        {
            _flashlight.OnBatteryChanged += UpdateFlashlightBattery;
            _flashlight.OnFlashlightToggled += UpdateFlashlightIcon;
            UpdateFlashlightBattery(_flashlight.BatteryPercent);
            UpdateFlashlightIcon(_flashlight.IsOn);
        }

        if (PlayerStateMachine.Instance != null)
        {
            PlayerStateMachine.Instance.OnBatteryCountChanged += UpdateBatteryCount;
            UpdateBatteryCount(PlayerStateMachine.Instance.BatteryCount);
        }

        // Estado inicial
        if (hideAtStart)
        {
            SetVisible(false);
            _initialHideFinished = false;
            Invoke(nameof(EndInitialHide), initialHideDuration);
        }
        else
        {
            _initialHideFinished = true;

            // Mostrar o no según si tiene linterna
            if (hideUntilFlashlightPickup)
            {
                bool hasFlashlight = _flashlight != null && _flashlight.HasFlashlight;
                SetVisible(hasFlashlight);
            }
            else
            {
                SetVisible(true);
            }
        }
    }

    private void EndInitialHide()
    {
        _initialHideFinished = true;

        // Decidir si se muestra según condiciones normales
        if (hideUntilFlashlightPickup)
        {
            bool hasFlashlight = _flashlight != null && _flashlight.HasFlashlight;
            SetVisible(hasFlashlight);
        }
        else
        {
            SetVisible(true);
        }

        Debug.Log("[FlashlightHUD] Cinemática terminada.");
    }

    /// <summary>Para llamar desde fuera cuando termine la cinemática.</summary>
    public void ShowHUDNow()
    {
        CancelInvoke(nameof(EndInitialHide));
        EndInitialHide();
    }

    private void Update()
    {
        if (!_initialHideFinished) return;

        // Mostrar el HUD cuando se recoge la linterna
        if (hideUntilFlashlightPickup
            && _flashlight != null
            && _flashlight.HasFlashlight
            && canvasGroup != null
            && canvasGroup.alpha < 0.5f)
        {
            SetVisible(true);
        }
    }

    private void OnDestroy()
    {
        if (_flashlight != null)
        {
            _flashlight.OnBatteryChanged -= UpdateFlashlightBattery;
            _flashlight.OnFlashlightToggled -= UpdateFlashlightIcon;
        }

        if (PlayerStateMachine.Instance != null)
            PlayerStateMachine.Instance.OnBatteryCountChanged -= UpdateBatteryCount;
    }

    // ─── Visibilidad ─────────────────────────────────────────────────────────

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    // ─── Actualizar visuales ─────────────────────────────────────────────────

    private void UpdateFlashlightBattery(float batteryPercent)
    {
        if (batterySlider != null)
            batterySlider.value = batteryPercent;

        if (batterySliderFill != null)
            batterySliderFill.color = GetBatteryColor(batteryPercent);

        if (batteryPercentText != null && showPercentage)
        {
            int percent = Mathf.RoundToInt(batteryPercent * 100f);
            batteryPercentText.text = $"{percent}%";
            batteryPercentText.color = GetBatteryColor(batteryPercent);
        }
    }

    private Color GetBatteryColor(float percent)
    {
        if (percent > 0.5f)
        {
            float t = (percent - 0.5f) / 0.5f;
            return Color.Lerp(colorMid, colorFull, t);
        }
        else
        {
            float t = percent / 0.5f;
            return Color.Lerp(colorLow, colorMid, t);
        }
    }

    private void UpdateBatteryCount(int count)
    {
        if (batteryCountText != null)
        {
            batteryCountText.text = string.Format(countFormat, count);
            batteryCountText.color = (count <= 0) ? countEmptyColor : countNormalColor;
        }

        if (batteryCountIcon != null)
        {
            batteryCountIcon.color = (count <= 0) ? countEmptyColor : countNormalColor;
        }
    }

    /// <summary>
    /// Cambia el alpha del icono de la linterna según si está encendida o no.
    /// </summary>
    private void UpdateFlashlightIcon(bool isOn)
    {
        if (flashlightIcon == null) return;

        Color c = flashlightIcon.color;
        c.a = isOn ? 1f : iconAlphaOff;
        flashlightIcon.color = c;
    }
}