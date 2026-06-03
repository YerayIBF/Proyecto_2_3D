using UnityEngine;
 
public class enemigoaire : MonoBehaviour
{
    public enum EnemyState { Wander, WanderSound, Searching, Attacking, Stunned }
    public EnemyState currentState;
 
    [Header("Configuración de Capas")]
    public LayerMask capasQueBloqueanVista;
 
    [Header("Sensores")]
    public Transform jugador;
    public bool cannotSee;
    public bool JugadorDentroRango;
    public PlayerStateMachine PlayerState;
 
    [Header("Ataque por Proyectil")]
    public GameObject prefabProyectil;
    public Transform puntoDisparo;
    public float fuerzaDisparo = 20f;
    public float tiempoEntreAtaques = 1.5f;
    private float tiempoSiguienteAtaque = 0f;
 
    [Header("Detección")]
    public float radioDeAudicion = 10f;
    public float anguloDeVision = 90f;
 
    [Header("Stun")]
    public float duracionDelStun = 10f;
    private float cronometroStun;
 
    void Update()
    {
        ManejarEstados();
    }
 
    void ManejarEstados()
    {
        if (currentState == EnemyState.Stunned)
        {
            cronometroStun -= Time.deltaTime;
            if (cronometroStun <= 0)
                currentState = EnemyState.Wander;
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
 
        // Detección por sonido (jugador corriendo)
        if (PlayerState != null
            && PlayerState.CurrentState == PlayerStateMachine.PlayerState.Running
            && distanciaAlJugador <= radioDeAudicion)
        {
            Debug.DrawRay(origen, direccion, Color.yellow);
            OrientarseHacia(direccion);
            return true;
        }
 
        // Detección por visión
        float anguloAlJugador = Vector3.Angle(transform.forward, direccion);
        if (anguloAlJugador <= anguloDeVision / 2f)
        {
            if (Physics.Raycast(origen, direccion, out RaycastHit hit, distanciaAlJugador, capasQueBloqueanVista)
                && hit.collider.CompareTag("Player"))
            {
                Debug.DrawRay(origen, direccion, Color.green);
                OrientarseHacia(direccion);
 
                if (Time.time >= tiempoSiguienteAtaque)
                {
                    LanzarBola(direccion.normalized);
                    tiempoSiguienteAtaque = Time.time + tiempoEntreAtaques;
                }
                return true;
            }
        }
 
        Debug.DrawRay(origen, direccion, Color.red);
        return false;
    }
 
    void OrientarseHacia(Vector3 direccion)
    {
        if (direccion == Vector3.zero) return;
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(direccion),
            Time.deltaTime * 5f);
    }
 
    void LanzarBola(Vector3 direccionHaciaJugador)
    {
        Vector3 spawn = puntoDisparo != null
            ? puntoDisparo.position
            : transform.position + transform.forward;
 
        GameObject bola = Instantiate(prefabProyectil, spawn, Quaternion.identity);
        Rigidbody rb = bola.GetComponent<Rigidbody>();
        if (rb != null)
            rb.AddForce(direccionHaciaJugador * fuerzaDisparo, ForceMode.Impulse);
 
        Destroy(bola, 4f);
    }
 
    // ─── Linterna ─────────────────────────────────────────────────────────────
 
    public void AplicarStun(float duracion)
    {
        currentState = EnemyState.Stunned;
        cronometroStun = duracion;
        Debug.Log($"[{gameObject.name}] Stunneado {duracion}s");
    }
 
    // ─── Triggers ─────────────────────────────────────────────────────────────
 
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))   JugadorDentroRango = true;
        if (other.CompareTag("ZonaOscura")) cannotSee = true;
    }
 
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))   JugadorDentroRango = false;
        if (other.CompareTag("ZonaOscura")) cannotSee = false;
    }
}
 