using UnityEngine;

/// <summary>
/// Llave final del nivel con identificador.
/// Al recogerse, registra la llave y le pide a la CamaraTemporal
/// de la puerta que se muestre unos segundos.
/// </summary>
public class KeyEnd : MonoBehaviour
{
    [Header("Identificador")]
    [Tooltip("ID de esta llave final. Debe coincidir con el de la puerta que abre.")]
    public string keyID = "final";

    [Header("Cámara de la puerta")]
    [Tooltip("El componente CamaraTemporal que vive en la cámara de la puerta")]
    public CamaraTemporal camaraPuerta;

    /// <summary>
    /// Llamado por CogerObjeto.AnimatorCogerObjeto() cuando se recoge la llave final.
    /// </summary>
    public void MostrarPuerta(float duration)
    {
        if (GameManager.instance != null)
            GameManager.instance.AddKey(keyID);

        if (camaraPuerta != null)
            camaraPuerta.Mostrar(duration);
    }
}