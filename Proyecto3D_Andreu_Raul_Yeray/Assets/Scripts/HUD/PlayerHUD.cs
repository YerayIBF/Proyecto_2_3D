using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD unificado del jugador.
/// - Vida: viñeta roja estilo Call of Duty (más roja cuanto menos vida)
/// - Stamina: barra horizontal en esquina inferior izquierda
/// - Baterías: número X/5
/// - Batería de linterna: barra horizontal
///
/// Se suscribe a los eventos de PlayerStateMachine y FlashlightSystem.
///
/// Setup en Canvas:
/// - Image "VignetteRed" a pantalla completa (anclada stretch/stretch), alpha 0
/// - Slider "StaminaBar" en esquina inferior izquierda
/// - Slider "FlashlightBar" en esquina inferior izquierda (debajo de stamina)
/// - TextMeshPro "BatteryText" con texto "0/5"
/// </summary>
public class PlayerHUD : MonoBehaviour
{
    [Header("Vida — Viñeta roja estilo CoD")]
    [Tooltip("Image rojo a pantalla completa (anclado stretch/stretch). Alpha se controla por código.")]
    public Image vignetteRed;
    [Tooltip("Cuánto rojo aparece según la vida. 1 = totalmente rojo cuando vida = 0")]
    public float maxVignetteAlpha = 0.85f;
    [Tooltip("Por debajo de este % de vida empieza a verse el rojo")]
    public float vignetteStartThreshold = 0.85f;
    [Tooltip("Velocidad de transición del rojo")]
    public float vignetteFadeSpeed = 4f;

    [Header("Stamina")]
    public Slider staminaBar;
    [Tooltip("CanvasGroup de la stamina (para ocultarla cuando está llena)")]
    public CanvasGroup staminaGroup;
    [Tooltip("Si está marcado, la stamina se oculta cuando está al 100%")]
    public bool hideStaminaWhenFull = true;
    public float staminaFadeSpeed = 4f;

    [Header("Batería de linterna")]
    public Slider flashlightBar;
    public CanvasGroup flashlightGroup;
    [Tooltip("Si está marcado, la barra de linterna solo se ve cuando la tienes recogida")]
    public bool hideFlashlightUntilPickup = true;

    [Header("Baterías inventario")]
    public TextMeshProUGUI batteryText;

    [Header("Pantalla daño rápido (golpe)")]
    [Tooltip("Imagen que parpadea al recibir golpe (más intenso que la viñeta)")]
    public Image damageFlash;
    public float damageFlashDuration = 0.3f;
    public float damageFlashAlpha    = 0.5f;
    private float _damageFlashTimer  = 0f;

    // ─── Estado ──────────────────────────────────────────────────────────────

    private float _targetVignetteAlpha = 0f;
    private float _targetStaminaAlpha  = 0f;
    private float _previousHealth      = 1f;

    // ─── Init ────────────────────────────────────────────────────────────────

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

        if (damageFlash != null)
        {
            Color c = damageFlash.color;
            c.a = 0f;
            damageFlash.color = c;
            damageFlash.raycastTarget = false;
        }

        if (staminaGroup != null)
            staminaGroup.alpha = 0f;

        if (flashlightGroup != null && hideFlashlightUntilPickup)
            flashlightGroup.alpha = 0f;
    }

    private void Start()
    {
        // Suscribirse a eventos del PlayerStateMachine
        if (PlayerStateMachine.Instance != null)
        {
            PlayerStateMachine.Instance.OnHealthChanged      += OnHealthChanged;
            PlayerStateMachine.Instance.OnStaminaChanged     += OnStaminaChanged;
            PlayerStateMachine.Instance.OnBatteryCountChanged += OnBatteryCountChanged;

            // Inicializar valores actuales
            OnHealthChanged(PlayerStateMachine.Instance.HealthPercent);
            OnStaminaChanged(PlayerStateMachine.Instance.StaminaPercent);
            OnBatteryCountChanged(PlayerStateMachine.Instance.BatteryCount);
        }

        // Suscribirse al evento de batería de la linterna
        FlashlightSystem fs = FindFirstObjectByType<FlashlightSystem>();
        if (fs != null)
        {
            fs.OnBatteryChanged += OnFlashlightBatteryChanged;
            OnFlashlightBatteryChanged(fs.BatteryPercent);

            // Mostrar la barra si ya tiene linterna
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
            fs.OnBatteryChanged -= OnFlashlightBatteryChanged;
    }

    // ─── Update ──────────────────────────────────────────────────────────────

    private void Update()
    {
        // Animar suavemente la viñeta roja hacia el target
        if (vignetteRed != null)
        {
            Color c = vignetteRed.color;
            c.a = Mathf.Lerp(c.a, _targetVignetteAlpha, Time.deltaTime * vignetteFadeSpeed);
            vignetteRed.color = c;
        }

        // Animar suavemente la stamina
        if (staminaGroup != null)
        {
            staminaGroup.alpha = Mathf.Lerp(staminaGroup.alpha, _targetStaminaAlpha, Time.deltaTime * staminaFadeSpeed);
        }

        // Animar damage flash (golpe puntual)
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
        // Si bajó la vida, parpadeo de daño
        if (healthPercent < _previousHealth - 0.01f && damageFlash != null)
        {
            _damageFlashTimer = damageFlashDuration;
        }
        _previousHealth = healthPercent;

        // Viñeta roja: más visible cuanto menos vida
        if (healthPercent >= vignetteStartThreshold)
        {
            _targetVignetteAlpha = 0f;
        }
        else
        {
            // De 0..vignetteStartThreshold → maxAlpha..0
            float t = 1f - (healthPercent / vignetteStartThreshold);
            _targetVignetteAlpha = t * maxVignetteAlpha;
        }
    }

    private void OnStaminaChanged(float staminaPercent)
    {
        if (staminaBar != null)
            staminaBar.value = staminaPercent;

        // Ocultar/mostrar según esté llena o no
        if (hideStaminaWhenFull)
            _targetStaminaAlpha = staminaPercent >= 0.99f ? 0f : 1f;
        else
            _targetStaminaAlpha = 1f;
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

        // Mostrar la barra cuando se recoge la linterna
        if (flashlightGroup != null && flashlightGroup.alpha < 0.5f)
            flashlightGroup.alpha = 1f;
    }
}
