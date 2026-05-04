using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD de la linterna. Coloca en un Canvas (Screen Space Overlay).
///
/// Setup Inspector:
///   flashlightSystem  → el FlashlightSystem del jugador
///   batteryFill       → Image con Fill Method = Filled (barra de batería)
///   batteryIcon       → Image del icono de linterna/batería
///   batteryText       → TextMeshProUGUI con el % (opcional)
///   lowBatteryColor   → color cuando la batería está baja (rojo)
///   normalColor       → color normal (verde o blanco)
///   flashlightOffIcon → sprite cuando la linterna está apagada
///   flashlightOnIcon  → sprite cuando está encendida
/// </summary>
public class FlashlightHUD : MonoBehaviour
{
    [Header("Referencias")]
    public FlashlightSystem flashlightSystem;

    [Header("UI — Batería")]
    [Tooltip("Image con Image Type = Filled para mostrar la barra")]
    public Image    batteryFill;
    [Tooltip("Icono de linterna o batería")]
    public Image    batteryIcon;
    [Tooltip("Texto opcional con el porcentaje")]
    public TextMeshProUGUI batteryText;

    [Header("Colores")]
    public Color normalColor     = Color.green;
    public Color midColor        = Color.yellow;
    public Color lowBatteryColor = Color.red;

    [Tooltip("Porcentaje a partir del cual el color cambia a mid (0-1)")]
    public float midThreshold  = 0.5f;
    [Tooltip("Porcentaje a partir del cual el color cambia a low (0-1)")]
    public float lowThreshold  = 0.2f;

    [Header("Iconos de estado")]
    public Sprite flashlightOnSprite;
    public Sprite flashlightOffSprite;

    [Header("Parpadeo del HUD cuando batería crítica")]
    public float hudFlickerSpeed = 3f;
    private float _hudFlickerTimer = 0f;

    // ─── Init ────────────────────────────────────────────────────────────────

    private void Start()
    {
        if (flashlightSystem == null)
            flashlightSystem = Object.FindFirstObjectByType<FlashlightSystem>();

        if (flashlightSystem != null)
        {
            // Suscribirse a los eventos
            flashlightSystem.OnBatteryChanged    += UpdateBatteryUI;
            flashlightSystem.OnFlashlightToggled += UpdateFlashlightIcon;

            // Estado inicial
            UpdateBatteryUI(flashlightSystem.BatteryPercent);
            UpdateFlashlightIcon(flashlightSystem.IsOn);
        }
    }

    private void OnDestroy()
    {
        if (flashlightSystem != null)
        {
            flashlightSystem.OnBatteryChanged    -= UpdateBatteryUI;
            flashlightSystem.OnFlashlightToggled -= UpdateFlashlightIcon;
        }
    }

    // ─── Update ──────────────────────────────────────────────────────────────

    private void Update()
    {
        if (flashlightSystem == null) return;

        // Parpadeo del HUD cuando la batería está crítica
        float percent = flashlightSystem.BatteryPercent;
        if (percent <= lowThreshold && flashlightSystem.IsOn)
        {
            _hudFlickerTimer += Time.deltaTime * hudFlickerSpeed;
            float alpha = Mathf.PingPong(_hudFlickerTimer, 1f);

            if (batteryFill != null)
            {
                Color c = batteryFill.color;
                c.a = Mathf.Lerp(0.3f, 1f, alpha);
                batteryFill.color = c;
            }
        }
        else
        {
            // Asegurar alpha completo cuando no parpadea
            if (batteryFill != null)
            {
                Color c = batteryFill.color;
                c.a = 1f;
                batteryFill.color = c;
            }
        }
    }

    // ─── Callbacks ───────────────────────────────────────────────────────────

    private void UpdateBatteryUI(float percent)
    {
        // Actualizar barra
        if (batteryFill != null)
        {
            batteryFill.fillAmount = percent;

            // Color según nivel
            if (percent <= lowThreshold)
                batteryFill.color = new Color(lowBatteryColor.r, lowBatteryColor.g, lowBatteryColor.b, batteryFill.color.a);
            else if (percent <= midThreshold)
                batteryFill.color = new Color(midColor.r, midColor.g, midColor.b, batteryFill.color.a);
            else
                batteryFill.color = new Color(normalColor.r, normalColor.g, normalColor.b, batteryFill.color.a);
        }

        // Texto opcional
        if (batteryText != null)
            batteryText.text = $"{Mathf.RoundToInt(percent * 100f)}%";

        // Ocultar HUD si no tiene linterna
        if (flashlightSystem != null && !flashlightSystem.HasFlashlight)
            gameObject.SetActive(false);
    }

    private void UpdateFlashlightIcon(bool isOn)
    {
        if (batteryIcon == null) return;

        if (isOn && flashlightOnSprite != null)
            batteryIcon.sprite = flashlightOnSprite;
        else if (!isOn && flashlightOffSprite != null)
            batteryIcon.sprite = flashlightOffSprite;

        // Atenuar icono cuando está apagada
        Color c = batteryIcon.color;
        c.a = isOn ? 1f : 0.4f;
        batteryIcon.color = c;
    }
}
