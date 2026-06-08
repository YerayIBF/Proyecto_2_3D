using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TareaCables : MonoBehaviour
{
   [Header("Configuración")]
    public int conexionesTotales = 4;   // cuántos cables hay que conectar para ganar
    public int conexionesActuales = 0;
 
    [Header("Referencia al panel")]
    public PanelElectricoInteraccion panel; // arrastra el PanelElectrico
 
    public void ComprobarVictoria()
    {
        Debug.Log($"[Cables] Conexiones: {conexionesActuales}/{conexionesTotales}");
 
        if (conexionesActuales >= conexionesTotales)
        {
            Debug.Log("[Cables] ¡Minijuego completado!");
 
            if (panel != null)
                panel.MinijuegoCompletado();
        }
    }
}
