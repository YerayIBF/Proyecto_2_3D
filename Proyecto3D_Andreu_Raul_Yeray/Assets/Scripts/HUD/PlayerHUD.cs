using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD unificado del jugador.
/// - Vida: viñeta roja estilo Call of Duty
/// - Stamina: efecto de máscara de gas empañada (overlay con vaho)
/// - Baterías inventario: número X/5
/// - Batería linterna: barra horizontal
/// - Carga de stun (opcional): imagen Filled que se llena al apuntar
/// </summary>
public class PlayerHUD : MonoBehaviour
{
    [Header("Vida — Viñeta roja estilo CoD")]
    public Image vignetteRed;
    public float maxVignetteAlpha = 0.85f;
    public float vignetteStartThreshold = 0.85f;
    public float vignetteFadeSpeed = 4f;

    [Header("Stamina — Máscara empañada")]
    [Tooltip("Image con la textura de vaho/condensación a pantalla completa")]
    public Image maskFogOverlay;
    [Tooltip("Cuánto vaho aparece al máximo (stamina = 0)")]
    public float maxFogAlpha = 0.7f;
    [Tooltip("Por debajo de este % de stamina empieza a verse el vaho")]
    public float fogStartThreshold = 0.7f;
    [Tooltip("Velocidad de transición del vaho (más bajo = se difumina más lento)")]
    public float fogFadeSpeed = 2f;

    [Header("Batería de linterna")]
    public Slider flashlightBar;
    public CanvasGroup flashlightGroup;
    public bool hideFlashlightUntilPickup = true;

    [Header("Baterías inventario")]
    public TextMeshProUGUI batteryText;

    [Header("Pantalla daño rápido (golpe)")]
    public Image damageFlash;
    public float damageFlashDuration = 0.3f;
    public float damageFlashAlpha    = 0.5f;
    private float _damageFlashTimer  = 0f;

    [Header("Carga de stun (opcional)")]
    [Tooltip("Image con Image Type = Filled, Method = Radial 360. Se llena al apuntar a un enemigo.")]
    public Image stunChargeCircle;

    // ─── Estado ──────────────────────────────────────────────────────────────

    private float _targetVignetteAlpha = 0f;
    private float _targetFogAlpha      = 0f;
    private float _previousHealth      = 1f;

    private void Awake()
    {
        // Ocultar todo al inicio
        if (vignetteRed != null)
        {
            Color c = vignetteRed.color;
            c.a = 0f;
            vignetteRed.color = c;
            vignetteRed.raycastTarget = false;
        }

        if (maskFogOverlay != null)
        {
            Color c = maskFogOverlay.color;
            c.a = 0f;
            maskFogOverlay.color = c;
            maskFogOverlay.raycastTarget = false;
        }

        if (damageFlash != null)
        {
            Color c = damageFlash.color;
            c.a = 0f;
            damageFlash.color = c;
            damageFlash.raycastTarget = false;
        }

        if (flashlightGroup != null && hideFlashlightUntilPickup)
            flashlightGroup.alpha = 0f;

        if (stunChargeCircle != null)
        {
            stunChargeCircle.fillAmount = 0f;
            stunChargeCircle.raycastTarget = false;
        }
    }

    private void Start()
    {
        if (PlayerStateMachine.Instance != null)
        {
            PlayerStateMachine.Instance.OnHealthChanged      += OnHealthChanged;
            PlayerStateMachine.Instance.OnStaminaChanged     += OnStaminaChanged;
            PlayerStateMachine.Instance.OnBatteryCountChanged += OnBatteryCountChanged;

            OnHealthChanged(PlayerStateMachine.Instance.HealthPercent);
            OnStaminaChanged(PlayerStateMachine.Instance.StaminaPercent);
            OnBatteryCountChanged(PlayerStateMachine.Instance.BatteryCount);
        }

        FlashlightSystem fs = FindFirstObjectByType<FlashlightSystem>();
        if (fs != null)
        {
            fs.OnBatteryChanged       += OnFlashlightBatteryChanged;
            OnFlashlightBatteryChanged(fs.BatteryPercent);

            if (fs.HasFlashlight && flashlightGroup != null)
                flashlightGroup.alpha = 1f;
        }
    }

    private void OnDestroy()
    {
        if (PlayerStateMachine.Instance != null)
        {
            PlayerStateMachine.Instance.OnHealthChanged      -= OnHealthChanged;
            PlayerStateMachine.Instance.OnStaminaChanged     -= OnStaminaChanged;
            PlayerStateMachine.Instance.OnBatteryCountChanged -= OnBatteryCountChanged;
        }

        FlashlightSystem fs = FindFirstObjectByType<FlashlightSystem>();
        if (fs != null)
        {
            fs.OnBatteryChanged     -= OnFlashlightBatteryChanged;
        }
    }

    private void Update()
    {
        // Viñeta roja
        if (vignetteRed != null)
        {
            Color c = vignetteRed.color;
            c.a = Mathf.Lerp(c.a, _targetVignetteAlpha, Time.deltaTime * vignetteFadeSpeed);
            vignetteRed.color = c;
        }

        // Vaho de máscara (stamina)
        if (maskFogOverlay != null)
        {
            Color c = maskFogOverlay.color;
            c.a = Mathf.Lerp(c.a, _targetFogAlpha, Time.deltaTime * fogFadeSpeed);
            maskFogOverlay.color = c;
        }

        // Damage flash
        if (damageFlash != null && _damageFlashTimer > 0f)
        {
            _damageFlashTimer -= Time.deltaTime;
            float t = _damageFlashTimer / damageFlashDuration;
            Color c = damageFlash.color;
            c.a = Mathf.Lerp(0f, damageFlashAlpha, t);
            damageFlash.color = c;
        }
    }

    // ─── Eventos ─────────────────────────────────────────────────────────────

    private void OnHealthChanged(float healthPercent)
    {
        if (healthPercent < _previousHealth - 0.01f && damageFlash != null)
            _damageFlashTimer = damageFlashDuration;
        _previousHealth = healthPercent;

        if (healthPercent >= vignetteStartThreshold)
            _targetVignetteAlpha = 0f;
        else
        {
            float t = 1f - (healthPercent / vignetteStartThreshold);
            _targetVignetteAlpha = t * maxVignetteAlpha;
        }
    }

    private void OnStaminaChanged(float staminaPercent)
    {
        // Vaho: más vaho cuanto menos stamina
        // 100% stamina = sin vaho
        // 0% stamina = vaho máximo (cuesta respirar)
        if (staminaPercent >= fogStartThreshold)
        {
            _targetFogAlpha = 0f;
        }
        else
        {
            float t = 1f - (staminaPercent / fogStartThreshold);
            _targetFogAlpha = t * maxFogAlpha;
        }
    }

    private void OnBatteryCountChanged(int count)
    {
        if (batteryText != null)
        {
            int max = PlayerStateMachine.Instance != null
                ? PlayerStateMachine.Instance.maxBatteries
                : 5;
            batteryText.text = $"{count}/{max}";
        }
    }

    private void OnFlashlightBatteryChanged(float batteryPercent)
    {
        if (flashlightBar != null)
            flashlightBar.value = batteryPercent;

        if (flashlightGroup != null && flashlightGroup.alpha < 0.5f)
            flashlightGroup.alpha = 1f;
    }
}