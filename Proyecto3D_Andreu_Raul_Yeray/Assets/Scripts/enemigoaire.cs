using UnityEngine;
using System.Collections;

public class enemigoaire : MonoBehaviour
{
    public enum EnemyState { Wander, WanderSound, Searching, Attacking, Stunned }
    public EnemyState currentState;

    [Header("Configuración de Capas")]
    // IMPORTANTE: En el Inspector, selecciona "Default" y "Player". 
    // NO selecciones la capa donde esté el enemigo.
    public LayerMask capasQueBloqueanVista; 

    [Header("Configuración de Estados")]
    public float tiempoStun = 6.0f;
    private float cronometroStun;
    public float duracionDelStun = 3.0f;

    [Header("Sensores")]
    public Transform jugador;
    public bool cannotSee;
    public bool puedoAtacar; 

    [Header("Tiempo para Stunearlo")]
    public float tiempoParaStunearlo = 2.0f;
    private Coroutine cuentaAtrasStun;

    void Update()
    {
        ManejarEstados();
    }

    void ManejarEstados()
    {
        if (currentState == EnemyState.Stunned)
        {
            if (cronometroStun > 0)
            {
                cronometroStun -= Time.deltaTime;
            }
            else  
            {
                currentState = EnemyState.Wander;
            }
        }
        else if (!cannotSee && puedoAtacar) 
        {
            PuedeVerAlJugador();
        }
    }

    bool PuedeVerAlJugador()
    {
        if (jugador == null) return false;

        Vector3 origen = transform.position ; 
        Vector3 direccion = (jugador.position + Vector3.up) - origen;

        RaycastHit hit;

        if (Physics.Raycast(origen, direccion, out hit, direccion.magnitude, capasQueBloqueanVista))
        {
            if (hit.collider.CompareTag("Player"))
            {
                Debug.DrawRay(origen, direccion, Color.green);
                Vector3 direccionMirada = new Vector3(direccion.x, direccion.y, direccion.z);
                if (direccionMirada != Vector3.zero) 
                {
                    Quaternion rotacionObjetivo = Quaternion.LookRotation(direccionMirada);
                    
                    transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, Time.deltaTime * 5f);
                    if (hit.transform.CompareTag("Player"))
                    {
                        Debug.Log("Hit");
                        //TODO: QUITAR VIDA
                    }
                }
                return true;
            }
        }
        else 
        {
        Debug.DrawRay(origen, direccion, Color.red);

        }
        
        return false;
    }

    // --- TRIGGERS ---
    private void OnTriggerEnter(Collider other)
    {
        // 1. Detección de rango (Cápsula Grande)
        if (other.CompareTag("Player"))
        {
            puedoAtacar = true;
            Debug.Log("Jugador en rango.");
        }

        // 2. Detección de linterna (Cuerpo)

        if (other.CompareTag("ZonaOscura")) cannotSee = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            puedoAtacar = false;
        }

        if (other.CompareTag("Flashlight"))
        {
            if (cuentaAtrasStun != null)
            {
                StopCoroutine(cuentaAtrasStun);
                cuentaAtrasStun = null;
                Debug.Log("Stun cancelado: El enemigo salió de la luz.");
            }
        }

        if (other.CompareTag("ZonaOscura")) cannotSee = false;
    }

    void AtacoAlEnemigo() 
    { 
         
    }

  /*  IEnumerator DuracionDeStun()
    {        
        currentState = EnemyState.Stunned;
        cronometroStun = tiempoStun;
        Debug.Log("¡Enemigo STUNNEADO!");
        cuentaAtrasStun = null;
    }*/
}