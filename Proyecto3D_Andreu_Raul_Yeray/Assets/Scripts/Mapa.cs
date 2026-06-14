using UnityEngine;
using UnityEngine.InputSystem;

public class Mapa : MonoBehaviour
{
    public GameObject panelMapa;
    public InputActionReference accionAlternarMapa;
    public GameObject panelPausa;

    private bool abiertoDesdePausa = false;

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
        EstadoMapa();
    }

    public void EstadoMapa()
    {
        if (panelMapa != null)
        {
            bool mapaEstabaActivo = panelMapa.activeSelf;

            if (!mapaEstabaActivo)
            {
                if (panelPausa != null && panelPausa.activeSelf)
                {
                    abiertoDesdePausa = true;  
                    panelPausa.SetActive(false); 
                }
                else
                {
                    abiertoDesdePausa = false; 
                }

                panelMapa.SetActive(true);
                Time.timeScale = 1f; 
            }
            else
            {
                panelMapa.SetActive(false); 

                if (panelPausa != null && abiertoDesdePausa)
                {
                    panelPausa.SetActive(true);

                    Time.timeScale = 0f; 
                }
                
                abiertoDesdePausa = false;
            }
        }
    }
}