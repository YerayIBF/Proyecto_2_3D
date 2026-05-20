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
    public bool JugadorDentroRango; 
    public PlayerStateMachine PlayerState; //pillar si esta corriendo

    [Header("Tiempo para Stunearlo")]
    public float tiempoParaStunearlo = 2.0f;
    private Coroutine cuentaAtrasStun;

    [Header("Ataque por Proyectil")]
    public GameObject prefabProyectil;   // Arrastra aquí el Prefab de tu bola
    public Transform puntoDisparo;       // Un objeto vacío hijo del enemigo (donde nacerá la bola)
    public float fuerzaDisparo = 20f;    // Qué tan rápido vuela la bola
    public float tiempoEntreAtaques = 1.5f; // Cooldown en segundos
    private float tiempoSiguienteAtaque = 0f; // Cronómetro interno

public float radioDeAudicion = 10f;    // Distancia máxima para escuchar
public float anguloDeVision = 90f;
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
        else if (!cannotSee && JugadorDentroRango) 
        {
            PuedeVerAlJugador();
        }
    }

    bool PuedeVerAlJugador()
    {
        if (jugador == null) return false;

        Vector3 origen = transform.position; 
        Vector3 direccion = (jugador.position + Vector3.up) - origen;
        float distanciaAlJugador = direccion.magnitude;

        if (PlayerState != null && PlayerState.CurrentState == PlayerStateMachine.PlayerState.Running && distanciaAlJugador <= radioDeAudicion)    
        {
            Debug.DrawRay(origen, direccion, Color.yellow); // Amarillo = Alerta por sonido
            if (direccion != Vector3.zero) 
                    {
                        Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion);
                        transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, Time.deltaTime * 5f);
                    }
            
            Debug.Log("Te escucho a través de la pared: Ataque");
            
            return true;
        }

        float anguloAlJugador = Vector3.Angle(transform.forward, direccion);

        if (anguloAlJugador <= anguloDeVision / 2f && !cannotSee)
        {
            RaycastHit hit;
            if (Physics.Raycast(origen, direccion, out hit, distanciaAlJugador, capasQueBloqueanVista))
            {
                if (hit.collider.CompareTag("Player"))
                {
                    Debug.DrawRay(origen, direccion, Color.green);
                    if (direccion != Vector3.zero) 
                    {
                        Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion);
                        transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, Time.deltaTime * 5f);
                    }
                    
                    Debug.Log("Te veo de frente con mis ojos: Ataque");
                    if (Time.time >= tiempoSiguienteAtaque)
                    {
                        LanzarBola(direccion.normalized); // Enviamos la dirección hacia el jugador
                        tiempoSiguienteAtaque = Time.time + tiempoEntreAtaques; // Aplicamos cooldown
                    }
                    return true;
                }
            }
        }

    // Si estás detrás de él, o fuera de su cono de visión en silencio: no te ve
    Debug.DrawRay(origen, direccion, Color.red);
    return false;
}

    // --- TRIGGERS ---
    private void OnTriggerEnter(Collider other)
    {
        // 1. Detección de rango (Cápsula Grande)
        if (other.CompareTag("Player"))
        {
            JugadorDentroRango = true;

            Debug.Log("Jugador en rango.");
        }

        // 2. Detección de linterna (Cuerpo)

        if (other.CompareTag("ZonaOscura")) cannotSee = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            JugadorDentroRango = false;
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

    void LanzarBola(Vector3 direccionHaciaJugador)
{
    // Si olvidaste asignar el punto de disparo, usamos la posición del enemigo
    Vector3 posicionSpawn = puntoDisparo != null ? puntoDisparo.position : transform.position + transform.forward;
    
    // 1. Clonar el prefab de la bola en la posición de disparo
    GameObject bolaClonada = Instantiate(prefabProyectil, posicionSpawn, Quaternion.identity);
    
    // 2. Conseguir el Rigidbody de la bola para darle fuerza
    Rigidbody rb = bolaClonada.GetComponent<Rigidbody>();
    
    if (rb != null)
    {
        // Empujamos la bola en dirección al jugador
        rb.AddForce(direccionHaciaJugador * fuerzaDisparo, ForceMode.Impulse);
    }
    
    // 3. Auto-destruir la bola después de 4 segundos para que no llene la escena de basura
    Destroy(bolaClonada, 4f);
}

  /*  IEnumerator DuracionDeStun()
    {        
        currentState = EnemyState.Stunned;
        cronometroStun = tiempoStun;
        Debug.Log("¡Enemigo STUNNEADO!");
        cuentaAtrasStun = null;
    }*/
}