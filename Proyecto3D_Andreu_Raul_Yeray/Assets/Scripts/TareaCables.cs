using UnityEngine;

public class TareaCables : MonoBehaviour
{
    [Header("Configuración")]
    public int conexionesTotales = 4;
    public int conexionesActuales = 0;

    [Header("Panel")]
    public PanelElectricoInteraccion panel;

    [Header("Panel especial (evento propio, opcional)")]
    public PanelEspecial panelEspecial; // si se asigna, este panel NO cuenta para la puerta

    public void ComprobarVictoria()
    {
        Debug.Log($"[Cables] Conexiones: {conexionesActuales}/{conexionesTotales}");

        if (conexionesActuales >= conexionesTotales)
        {
            Debug.Log("[Cables] ¡Minijuego completado!");

            if (panelEspecial != null)
            {
                // Panel especial: ejecuta su evento y cierra sin contar para la puerta
                panelEspecial.CompletarPanelEspecial();
                if (panel != null)
                    panel.CompletarSinContar();
            }
            else
            {
                // Panel normal: cuenta para la puerta
                if (panel != null)
                    panel.MinijuegoCompletado();
            }
        }
    }
}