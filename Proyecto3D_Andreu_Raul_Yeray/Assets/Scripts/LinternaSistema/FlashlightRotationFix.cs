using UnityEngine;

[DefaultExecutionOrder(10000)] // Después de todo, incluido IK
public class FlashlightLookAtTarget : MonoBehaviour
{
    [Tooltip("El punto al que debe mirar la linterna (ej. la mira del arma o un punto en el mundo)")]
    public Transform aimTarget;

    [Tooltip("Offset de rotación local para ajustes finos (ej. inclinar un poco hacia abajo)")]
    public Vector3 localRotationOffset;

    [Tooltip("Velocidad de suavizado (0 = instantáneo, 10 = rápido, 2 = lento)")]
    public float smoothSpeed = 15f;

    private void LateUpdate()
    {
        if (aimTarget == null)
            return;

        // Dirección deseada en espacio mundo
        Vector3 desiredWorldDirection = (aimTarget.position - transform.position).normalized;

        // Rotación objetivo: mira hacia adelante (eje Z) en dirección al target
        Quaternion targetWorldRotation = Quaternion.LookRotation(desiredWorldDirection, Vector3.up);

        // Aplicar offset local (por ejemplo, inclinación extra)
        targetWorldRotation *= Quaternion.Euler(localRotationOffset);

        // Suavizar (opcional, evita temblores)
        if (smoothSpeed > 0f)
            transform.rotation = Quaternion.Slerp(transform.rotation, targetWorldRotation, Time.deltaTime * smoothSpeed);
        else
            transform.rotation = targetWorldRotation;
    }
}