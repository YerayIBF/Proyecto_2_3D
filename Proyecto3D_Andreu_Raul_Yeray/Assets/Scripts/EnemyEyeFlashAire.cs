using UnityEngine;
using System.Collections;

/// <summary>
/// Efecto visual de los ojos del enemigoaire (ciego).
/// Mismo comportamiento que EnemyEyeFlash pero adaptado a enemigoaire.
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

    private Coroutine _stunRoutine;
    private bool _stunRoutineActive = false;

    private void Awake()
    {
        if (enemy == null)
            enemy = GetComponent<enemigoaire>();

        // Apagar al inicio
        SetLightsActive(false);
    }

    private void Update()
    {
        if (enemy == null) return;

        bool isStunned = enemy.IsCurrentlyStunned;
        bool canBeStunned = enemy.CanBeStunned;
        bool inCooldown = !isStunned && !canBeStunned;

        if (isStunned)
        {
            // Si la coroutine de stun no se ha lanzado todavía, lánzala
            if (!_stunRoutineActive)
            {
                _stunRoutineActive = true;
                if (_stunRoutine != null) StopCoroutine(_stunRoutine);
                _stunRoutine = StartCoroutine(StunFlashRoutine());
            }
            // Durante el stun, la coroutine gestiona los lights
            return;
        }

        // Ya no está stuneado → parar coroutine si seguía activa
        if (_stunRoutineActive)
        {
            _stunRoutineActive = false;
            if (_stunRoutine != null)
            {
                StopCoroutine(_stunRoutine);
                _stunRoutine = null;
            }
        }

        // Aplicar el estado correcto cada frame
        if (inCooldown)
        {
            // Cooldown: ojos rojos intensos
            SetLightsColor(cooldownColor, cooldownIntensity);
            SetLightsActive(true);
        }
        else
        {
            // Normal: apagados
            SetLightsActive(false);
        }
    }

    // ─── Coroutine de parpadeo durante stun ─────────────────────────────────

    private IEnumerator StunFlashRoutine()
    {
        SetLightsColor(stunColor, stunIntensity);

        // Parpadeo rápido
        for (int i = 0; i < blinkCount; i++)
        {
            SetLightsActive(true);
            yield return new WaitForSeconds(blinkOnDuration);
            SetLightsActive(false);
            yield return new WaitForSeconds(blinkOffDuration);
        }

        // Encendidas el resto del stun
        SetLightsActive(true);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private void SetLightsActive(bool active)
    {
        if (eyeLights == null || eyeLights.Length == 0) return;
        foreach (var light in eyeLights)
        {
            if (light != null) light.enabled = active;
        }
    }

    private void SetLightsColor(Color color, float intensity)
    {
        if (eyeLights == null) return;
        foreach (var light in eyeLights)
        {
            if (light != null)
            {
                light.color = color;
                light.intensity = intensity;
            }
        }
    }
}
