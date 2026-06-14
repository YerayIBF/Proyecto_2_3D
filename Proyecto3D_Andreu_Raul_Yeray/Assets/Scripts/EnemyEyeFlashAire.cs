using UnityEngine;
using System.Collections;

/// <summary>
/// Efecto visual de los ojos del enemigoaire (ciego).
///
/// Estados:
/// - Normal (puede stunear): ojos apagados
/// - Aturdido: parpadeo + ojos blancos
/// - En cooldown: ojos rojos intensos (aviso)
/// </summary>
public class EnemyEyeFlashAire : MonoBehaviour
{
    [Header("Referencias")]
    public Light[] eyeLights;
    public enemigoaire enemy;

    [Header("Color al stunear")]
    public Color stunColor = Color.white;
    public float stunIntensity = 3f;

    [Header("Color en cooldown (no se puede stunear)")]
    public Color cooldownColor = Color.red;
    public float cooldownIntensity = 4f;

    [Header("Parpadeo inicial al stunear")]
    public int blinkCount = 4;
    public float blinkOnDuration = 0.08f;
    public float blinkOffDuration = 0.06f;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private Coroutine _stunRoutine;
    private bool _stunRoutineActive = false;

    private void Awake()
    {
        if (enemy == null)
            enemy = GetComponent<enemigoaire>();
        if (enemy == null)
            enemy = GetComponentInParent<enemigoaire>();

        // Inicializar luces con color stun y APAGADAS
        SetLightsColor(stunColor, stunIntensity);
        SetLightsActive(false);
    }

    private void Start()
    {
        // PREWARM URP: encender brevemente las luces para que URP las registre
        // Sin esto, la primera vez no se encienden en URP
        StartCoroutine(PrewarmLights());
    }

    private IEnumerator PrewarmLights()
    {
        SetLightsActive(true);
        yield return null; // esperar 1 frame
        SetLightsActive(false);

        if (showDebugLogs)
            Debug.Log($"[EyeFlashAire] Prewarm completado — {gameObject.name}");
    }

    private void Update()
    {
        if (enemy == null) return;

        bool isStunned = enemy.IsCurrentlyStunned;
        bool canBeStunned = enemy.CanBeStunned;
        bool inCooldown = !isStunned && !canBeStunned;

        if (isStunned)
        {
            if (!_stunRoutineActive)
            {
                TriggerStunVisual();
            }
            // Durante el stun la coroutine controla TODO, el Update no toca nada
            return;
        }

        // Si la coroutine seguía activa pero ya no está stuneado, pararla
        if (_stunRoutineActive)
        {
            _stunRoutineActive = false;
            if (_stunRoutine != null)
            {
                StopCoroutine(_stunRoutine);
                _stunRoutine = null;
            }
        }

        if (inCooldown)
        {
            SetLightsColor(cooldownColor, cooldownIntensity);
            SetLightsActive(true);
        }
        else
        {
            SetLightsActive(false);
        }
    }

    /// <summary>
    /// Disparar el efecto visual del stun directamente (sin esperar al Update).
    /// </summary>
    public void TriggerStunVisual()
    {
        if (showDebugLogs)
            Debug.Log($"[EyeFlashAire] TriggerStunVisual() — {gameObject.name}");

        _stunRoutineActive = true;
        if (_stunRoutine != null) StopCoroutine(_stunRoutine);
        _stunRoutine = StartCoroutine(StunFlashRoutine());
    }

    private IEnumerator StunFlashRoutine()
    {
        // Asegurar color correcto desde el inicio
        SetLightsColor(stunColor, stunIntensity);

        // Fase de parpadeo
        for (int i = 0; i < blinkCount; i++)
        {
            SetLightsActive(true);
            SetLightsColor(stunColor, stunIntensity);
            yield return new WaitForSeconds(blinkOnDuration);

            SetLightsActive(false);
            yield return new WaitForSeconds(blinkOffDuration);
        }

        // Tras el parpadeo, dejar encendido fijo el resto del stun
        SetLightsColor(stunColor, stunIntensity);
        SetLightsActive(true);

        // Mantener encendido mientras siga stuneado
        while (enemy != null && enemy.IsCurrentlyStunned)
        {
            // Re-asegurar el estado cada cierto tiempo (no cada frame)
            SetLightsColor(stunColor, stunIntensity);
            SetLightsActive(true);
            yield return new WaitForSeconds(0.1f);
        }

        // El stun terminó: liberar el control
        _stunRoutineActive = false;
        _stunRoutine = null;
    }

    private void SetLightsActive(bool active)
    {
        if (eyeLights == null) return;
        for (int i = 0; i < eyeLights.Length; i++)
        {
            if (eyeLights[i] != null)
                eyeLights[i].enabled = active;
        }
    }

    private void SetLightsColor(Color color, float intensity)
    {
        if (eyeLights == null) return;
        for (int i = 0; i < eyeLights.Length; i++)
        {
            if (eyeLights[i] != null)
            {
                eyeLights[i].color = color;
                eyeLights[i].intensity = intensity;
            }
        }
    }
}