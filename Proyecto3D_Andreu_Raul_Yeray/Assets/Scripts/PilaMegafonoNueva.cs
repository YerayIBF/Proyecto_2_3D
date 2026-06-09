using UnityEngine;

/// <summary>
/// Pila de megáfono recogible.
/// El jugador tiene que acercarse Y pulsar Interact (X / E) para recogerla.
/// Funciona igual que las llaves: se detecta por tag "PilaMegafonoNueva"
/// en el CogerObjeto y se procesa al recogerla.
/// </summary>
public class PilaMegafonoNueva : MonoBehaviour
{
    [Header("Cantidad de pilas que añade")]
    public int cantidad = 1;

    [Header("Icono de recogida (opcional)")]
    public GameObject pickupIcon;

    /// <summary>
    /// Se llama desde CogerObjeto.AnimatorCogerObjeto() cuando se recoge.
    /// </summary>
    public void Recoger()
    {
        if (PlayerStateMachine.Instance != null)
        {
            PlayerStateMachine.Instance.AddMegafonoBattery(cantidad);
        }

        Debug.Log($"[PilaMegafonoNueva] +{cantidad} pila de megáfono.");
        gameObject.SetActive(false);
    }

    public void MostrarIcono(bool mostrar)
    {
        if (pickupIcon != null) pickupIcon.SetActive(mostrar);
    }
}
