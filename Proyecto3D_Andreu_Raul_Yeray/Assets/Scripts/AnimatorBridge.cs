using UnityEngine;

/// <summary>
/// Puente entre Animation Events y los scripts del jugador.
/// Va en el GameObject del Animator (PlayerArmature).
///
/// Los Animation Events de las animaciones llaman a estos métodos públicos.
/// </summary>
public class AnimatorBridge : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Script CogerObjeto del jugador (en un hijo del PlayerArmature)")]
    public CogerObjeto handScript;

    // ─── Animation Events ────────────────────────────────────────────────────

    /// <summary>
    /// Llamado por el Animation Event de la animación "Coger".
    /// Mete el objeto cercano en la mano del jugador.
    /// </summary>
    public void EventCoger()
    {
        if (handScript != null)
            handScript.AnimatorCogerObjeto();
    }

    /// <summary>
    /// Llamado por el Animation Event de la animación "Throw" / "ThrowCrouched".
    /// Lanza el objeto que tiene en la mano.
    /// </summary>
    public void LanzarObjeto()
    {
        if (handScript != null)
            handScript.LanzarObjeto();
    }

    /// <summary>
    /// Llamado por el Animation Event de la animación "Reload" / "ReloadCrouched".
    /// Distingue automáticamente si recarga linterna o megáfono según lo que tenga en mano.
    /// </summary>
    public void EventReload()
    {
        if (PlayerEquipmentManager.Instance == null)
        {
            Debug.LogWarning("[AnimatorBridge] No hay PlayerEquipmentManager.");
            return;
        }

        if (PlayerEquipmentManager.Instance.IsFlashlightInHand)
        {
            Debug.Log("[AnimatorBridge] Recargando LINTERNA.");
            PlayerStateMachine.Instance?.TryReloadFlashlight();
        }
        else if (PlayerEquipmentManager.Instance.IsMegaphoneInHand)
        {
            Debug.Log("[AnimatorBridge] Recargando MEGÁFONO.");
            PlayerStateMachine.Instance?.TryReloadMegafono();
        }
        else
        {
            Debug.LogWarning("[AnimatorBridge] No hay nada que recargar en la mano.");
        }
    }
}