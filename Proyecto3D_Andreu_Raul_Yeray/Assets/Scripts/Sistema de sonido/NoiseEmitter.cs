using UnityEngine;
using System;

/// <summary>
/// Sistema de ruido central. Cualquier script emite ruido llamando NoiseEmitter.Emit().
/// El NoiseSensor del enemigo se suscribe al evento OnNoiseEmitted.
/// </summary>
public static class NoiseEmitter
{
    /// <summary>
    /// Evento global: (posición del ruido, radio, intensidad 0-1)
    /// </summary>
    public static event Action<Vector3, float, float> OnNoiseEmitted;

    /// <summary>
    /// Emite un ruido en una posición.
    /// </summary>
    /// <param name="position">Origen del ruido</param>
    /// <param name="radius">Radio máximo de detección en metros</param>
    /// <param name="intensity">Intensidad 0-1 — afecta a la precisión de localización del enemigo</param>
    public static void Emit(Vector3 position, float radius, float intensity = 1f)
    {
        OnNoiseEmitted?.Invoke(position, radius, intensity);
        DebugDrawSphere(position, radius, Color.red, 2f);
        Debug.Log($"[NoiseEmitter] Ruido | Radio: {radius}m | Intensidad: {intensity:F2}");
    }

    private static void DebugDrawSphere(Vector3 center, float radius, Color color, float duration)
    {
        int segments = 16;
        float step = 360f / segments;
        for (int i = 0; i < segments; i++)
        {
            float a1 = Mathf.Deg2Rad * (i * step);
            float a2 = Mathf.Deg2Rad * ((i + 1) * step);
            Debug.DrawLine(
                center + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * radius,
                center + new Vector3(Mathf.Cos(a2), 0, Mathf.Sin(a2)) * radius,
                color, duration);
        }
    }
}
