using UnityEngine;
using System.Collections;

/// <summary>
/// Efecto visual de los ojos del enemigo.
/// </summary>
public class EnemyEyeFlash : MonoBehaviour
{
    [Header("Referencias")]
    public Light[] eyeLights;
    public EnemyBehaviourTree behaviourTree;

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
    [Tooltip("Si está activo, muestra logs en consola")]
    public bool showDebugLogs = true;

    private Coroutine _stunRoutine;
    private bool _stunRoutineActive = false;

    private void Awake()
    {
        if (behaviourTree == null)
            behaviourTree = GetComponent<EnemyBehaviourTree>();
        if (behaviourTree == null)
            behaviourTree = GetComponentInParent<EnemyBehaviourTree>();

        if (showDebugLogs)
        {
            Debug.Log($"[EyeFlash] Awake — {gameObject.name} | BT: {(behaviourTree != null ? "OK" : "NULL")} | Lights: {(eyeLights != null ? eyeLights.Length : 0)}");
        }

        // Inicializar luces con color stun y APAGADAS
        SetLightsColor(stunColor, stunIntensity);
        SetLightsActive(false);
    }

    private void Start()
    {
        // PREWARM URP: encender brevemente las luces para que URP las registre
        // Sin esto, la primera vez puede no encenderse en URP
        StartCoroutine(PrewarmLights());
    }

    private IEnumerator PrewarmLights()
    {
        SetLightsActive(true);
        yield return null; // esperar 1 frame
        SetLightsActive(false);

        if (showDebugLogs)
            Debug.Log($"[EyeFlash] Prewarm completado — {gameObject.name}");
    }

    private void Update()
    {
        if (behaviourTree == null) return;

        bool isStunned = behaviourTree.IsCurrentlyStunned;
        bool canBeStunned = behaviourTree.CanBeStunned;
        bool inCooldown = !isStunned && !canBeStunned;

        if (isStunned)
        {
            if (!_stunRoutineActive)
            {
                if (showDebugLogs)
                    Debug.Log($"[EyeFlash] Detectado stun en Update — disparando coroutine");
                TriggerStunVisual();
            }
            return;
        }

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
    /// Disparar el efecto visual del stun directamente.
    /// </summary>
    public void TriggerStunVisual()
    {
        if (showDebugLogs)
            Debug.Log($"[EyeFlash] TriggerStunVisual() — {gameObject.name}");

        _stunRoutineActive = true;
        if (_stunRoutine != null) StopCoroutine(_stunRoutine);
        _stunRoutine = StartCoroutine(StunFlashRoutine());
    }

    private IEnumerator StunFlashRoutine()
    {
        if (showDebugLogs)
            Debug.Log($"[EyeFlash] Iniciando StunFlashRoutine — {gameObject.name}");

        SetLightsColor(stunColor, stunIntensity);

        for (int i = 0; i < blinkCount; i++)
        {
            SetLightsActive(true);
            SetLightsColor(stunColor, stunIntensity);
            yield return new WaitForSeconds(blinkOnDuration);

            SetLightsActive(false);
            yield return new WaitForSeconds(blinkOffDuration);
        }

        SetLightsColor(stunColor, stunIntensity);
        SetLightsActive(true);

        if (showDebugLogs)
            Debug.Log($"[EyeFlash] Stun visual finalizado — {gameObject.name}");
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