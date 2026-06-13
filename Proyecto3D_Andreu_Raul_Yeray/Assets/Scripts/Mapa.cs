using UnityEngine;
using UnityEngine.InputSystem;

public class Mapa : MonoBehaviour
{
    public GameObject panelMapa;
    public InputActionReference accionAlternarMapa;

    private void OnEnable()
    {
        // Llamar al evento de cuando se pulsa el botón
        if (accionAlternarMapa != null)
        {
            accionAlternarMapa.action.started += AlPulsarMapa;
            accionAlternarMapa.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (accionAlternarMapa != null)
        {
            accionAlternarMapa.action.started -= AlPulsarMapa;
        }
    }

    private void AlPulsarMapa(InputAction.CallbackContext context)
    {
        if (panelMapa != null)
        {
            // Invierte el estado actual del panel, si esta cerrado se abre, y si esta abierto se cierra
            bool estadoActual = panelMapa.activeSelf;
            panelMapa.SetActive(!estadoActual);
        }
    }
}