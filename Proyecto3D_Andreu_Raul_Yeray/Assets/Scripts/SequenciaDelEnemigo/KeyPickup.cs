using UnityEngine;

/// <summary>
/// Llave del nivel con identificador único.
/// Cada llave tiene un keyID que coincide con el de la puerta que abre.
/// </summary>
public class KeyPickup : MonoBehaviour
{
    [Header("Identificador")]
    [Tooltip("ID único de esta llave (ej: 'almacen', 'oficina', 'sotano'). Debe coincidir con el de la puerta correspondiente.")]
    public string keyID = "default";

    [Header("Referencias")]
    [Tooltip("Secuencia del enemigo que se activará al recoger la llave (opcional)")]
    public EnemyIntroSequence enemySequence;

    [Tooltip("GameObject que engloba todos los paneles de cables (se activa al coger la llave, opcional)")]
    public GameObject paneles;

    [Header("Opciones")]
    [Tooltip("Si está marcado, activa la cinemática del timeline de llave del GameManager")]
    public bool activarCinematica = true;

    [Header("Subtítulo personalizado")]
    [Tooltip("Mensaje a mostrar al recoger la llave (opcional)")]
    public string mensajeAlRecoger = "";

    /// <summary>
    /// Llamado por CogerObjeto.AnimatorCogerObjeto() cuando se recoge la llave.
    /// </summary>
    public void OnPickedUp()
    {
        Debug.Log($"[KeyPickup] Llave recogida: ID = '{keyID}'");

        // Registrar la llave en el GameManager
        if (GameManager.instance != null)
        {
            GameManager.instance.AddKey(keyID);
        }

        // Activar la cinemática (solo si está marcado)
        if (activarCinematica && GameManager.instance != null)
        {
            GameManager.instance.ActivarTimelineLlave();
        }

        // Activar la secuencia del enemigo si está asignada
        if (enemySequence != null)
            enemySequence.StartSequence();

        // Activar los paneles si están asignados
        if (paneles != null)
            paneles.SetActive(true);

        // Subtítulo personalizado
        if (!string.IsNullOrEmpty(mensajeAlRecoger) && GameManager.instance != null)
        {
            GameManager.instance.ReproducirVoz(mensajeAlRecoger, 3f);
        }
    }
}