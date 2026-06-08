using UnityEngine;

public class AnimatorBridge : MonoBehaviour
{
    public CogerObjeto handScript;
    public FlashlightSystem flashlightSystem;

    void Start()
    {

    }

    void Update()
    {

    }

    public void EventCoger()
    {
        handScript.AnimatorCogerObjeto();
    }

    public void LanzarObjeto()
    {
        handScript.LanzarObjeto();
    }

    /// <summary>
    /// Llamado por el Animation Event de la animación "Reload" o "ReloadCrouched"
    /// en el frame en que el jugador inserta/cambia la batería.
    /// Usa TryReloadFlashlight para consumir una batería del inventario.
    /// </summary>
    public void EventReload()
    {
        if (PlayerStateMachine.Instance != null)
            PlayerStateMachine.Instance.TryReloadFlashlight();
        else if (flashlightSystem != null)
            flashlightSystem.Reload();
    }
}