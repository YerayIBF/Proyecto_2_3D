using UnityEngine;

/// <summary>
/// Rota el modelo de la linterna cuando el jugador apunta, para que visualmente
/// quede recta y apuntando hacia adelante.
///
/// Coloca este script en el GameObject "Linterna" (el modelo visual).
/// </summary>
[DefaultExecutionOrder(10000)]
public class FlashlightModelAimRotation : MonoBehaviour
{
    [Header("Referencias")]
    public FlashlightSystem flashlight;

    [Header("Rotación al apuntar")]
    [Tooltip("Rotación LOCAL que tendrá la linterna al apuntar")]
    public Vector3 aimingLocalRotation = Vector3.zero;

    [Header("Transición")]
    public float transitionSpeed = 8f;

    private Quaternion _restRotation;     // Rotación normal (de la animación)
    private Quaternion _currentExtra = Quaternion.identity;

    private void Awake()
    {
        if (flashlight == null)
            flashlight = GetComponentInParent<FlashlightSystem>();
    }

    private void LateUpdate()
    {
        // Guardar la rotación que viene de la animación/IK en este frame
        _restRotation = transform.localRotation;

        // Calcular la rotación objetivo
        Quaternion targetExtra = (flashlight != null && flashlight.IsAiming)
            ? Quaternion.Euler(aimingLocalRotation)
            : Quaternion.identity;

        // Interpolar suavemente
        _currentExtra = Quaternion.Slerp(
            _currentExtra, targetExtra,
            Time.deltaTime * transitionSpeed);

        // Aplicar SOBRE la rotación base
        transform.localRotation = _restRotation * _currentExtra;
    }
}
