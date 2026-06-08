using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Script para la llave del nivel.
///
/// Esta clase NO gestiona la detección ni el input. Eso lo hace CogerObjeto
/// (por consistencia con linterna y megáfono).
///
/// CogerObjeto detecta la llave por tag "Llave", reproduce la animación
/// de coger, y al terminar llama a OnPickedUp() para activar la cinemática.
/// </summary>
public class KeyPickup : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Secuencia del enemigo que se activará al recoger la llave (opcional)")]
    public EnemyIntroSequence enemySequence;

    [Tooltip("GameObject que engloba todos los paneles de cables (se activa al coger la llave)")]
    public GameObject paneles;

    /// <summary>
    /// Llamado por CogerObjeto.AnimatorCogerObjeto() cuando se recoge la llave.
    /// </summary>
    public void OnPickedUp()
    {
        Debug.Log("[KeyPickup] Llave recogida, activando cinemática del enemigo.");

        if (enemySequence != null)
            enemySequence.StartSequence();

        // Activa todos los paneles de cables
        if (paneles != null)
            paneles.SetActive(true);

        GameManager.instance.ActivarTimelineLlave();
    }
}