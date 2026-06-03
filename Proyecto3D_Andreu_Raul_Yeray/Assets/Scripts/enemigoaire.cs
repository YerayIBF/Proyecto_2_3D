using UnityEngine;
 
public class enemigoaire : MonoBehaviour
{
    public enum EnemyState { Wander, Attacking, Stunned }
    public EnemyState currentState;
 
    [Header("Referencias")]
    public Transform jugador;
    public Transform eyes;
    public PlayerStateMachine PlayerState;
    public FlashlightSystem linternaJugador; // arrastra aquí el FlashlightSystem del jugador
 
    [Header("Detección visual")]
    public float     detectionRange = 12f;
    public float     detectionAngle = 90f;
    public LayerMask visionBlockMask;
    public float     visionMemoryDuration = 2f;
    private float    _visionMemoryTimer = 0f;
 
    [Header("Detección por sonido")]
    public float radioDeAudicion = 10f;
 
    [Header("Zona Oscura")]
    public bool cannotSee;
    private Vector3 _ultimaPosicionSonido;
    private bool    _tieneObjetivoSonido = false;
 
    [Header("Ataque por Proyectil")]
    public GameObject prefabProyectil;
    public Transform  puntoDisparo;
    public float      fuerzaDisparo = 20f;
    public float      tiempoEntreAtaques = 1.5f;
    private float     tiempoSiguienteAtaque = 0f;
 
    [Header("Stun")]
    public float duracionDelStun = 10f;
    private float cronometroStun;
 
    void Update()
    {
        if (_visionMemoryTimer > 0f) _visionMemoryTimer -= Time.deltaTime;
 
        ComprobarSonido();
        ManejarEstados();
    }
 
    void ComprobarSonido()
    {
        if (currentState == EnemyState.Stunned) return;
        if (jugador == null || PlayerState == null) return;
        if (PlayerState.CurrentState != PlayerStateMachine.PlayerState.Running) return;
 
        float distancia = Vector3.Distance(transform.position, jugador.position);
        if (distancia <= radioDeAudicion)
            OnHeardNoise(jugador.position);
    }
 
    void ManejarEstados()
    {
        if (currentState == EnemyState.Stunned)
        {
            cronometroStun -= Time.deltaTime;
            if (cronometroStun <= 0)
                currentState = EnemyState.Wander;
            return;
        }
 
        bool seesPlayer = CanSeePlayer();
 
        if (seesPlayer)
        {
            // Visión normal o linterna encendida en oscuridad
            _visionMemoryTimer = visionMemoryDuration;
            currentState = EnemyState.Attacking;
        }
        else if (cannotSee && _tieneObjetivoSonido)
        {
            // Oscuridad + linterna apagada + hay sonido guardado → atacar a ciegas
            currentState = EnemyState.Attacking;
        }
        else if (_visionMemoryTimer <= 0f)
        {
            currentState = EnemyState.Wander;
        }
 
        if (currentState == EnemyState.Attacking)
        {
            Vector3 direccion;
 
            if (cannotSee && _tieneObjetivoSonido && !PuedeVerLinterna())
            {
                // Oscuridad sin linterna: apunta a la última posición de sonido
                direccion = (_ultimaPosicionSonido + Vector3.up) - transform.position;
            }
            else
            {
                // Visión normal o linterna encendida: apunta al jugador real
                direccion = (jugador.position + Vector3.up) - transform.position;
            }
 
            OrientarseHacia(direccion);
 
            // En oscuridad dispara a ciegas, con visión comprueba pared
            bool puedeDisparar = cannotSee ? _tieneObjetivoSonido || PuedeVerLinterna()
                                           : TieneLineaDeDisparo();
 
            if (Time.time >= tiempoSiguienteAtaque && puedeDisparar)
            {
                LanzarBola(direccion.normalized);
                tiempoSiguienteAtaque = Time.time + tiempoEntreAtaques;
            }
        }
    }
 
    // ─── Detección ────────────────────────────────────────────────────────────
 
    bool CanSeePlayer()
    {
        if (jugador == null || eyes == null) return false;
 
        // En zona oscura: solo ve al jugador si lleva la linterna encendida
        if (cannotSee)
            return PuedeVerLinterna();
 
        // Visión normal: 3 puntos de test, cono y raycast
        Vector3[] testPoints =
        {
            jugador.position + Vector3.up * 0.3f,
            jugador.position + Vector3.up * 1.0f,
            jugador.position + Vector3.up * 1.7f,
        };
 
        foreach (Vector3 targetPoint in testPoints)
        {
            float dist = Vector3.Distance(eyes.position, targetPoint);
            if (dist > detectionRange) continue;
 
            Vector3 dir = (targetPoint - eyes.position).normalized;
            if (Vector3.Angle(eyes.forward, dir) > detectionAngle * 0.5f) continue;
 
            if (Physics.Raycast(eyes.position, dir, out RaycastHit hit, dist, visionBlockMask))
            {
                if (!hit.collider.transform.IsChildOf(jugador) && hit.collider.transform != jugador)
                    continue;
            }
 
            return true;
        }
 
        return false;
    }
 
    // Detecta la luz de la linterna en zona oscura
    bool PuedeVerLinterna()
    {
        if (linternaJugador == null || !linternaJugador.IsOn) return false;
 
        float dist = Vector3.Distance(eyes.position, jugador.position);
        if (dist > detectionRange) return false;
 
        Vector3 dir = (jugador.position - eyes.position).normalized;
        if (Vector3.Angle(eyes.forward, dir) > detectionAngle * 0.5f) return false;
 
        return true;
    }
 
    bool TieneLineaDeDisparo()
    {
        if (jugador == null) return false;
 
        Vector3 origen = puntoDisparo != null ? puntoDisparo.position : transform.position;
        Vector3 dir    = (jugador.position + Vector3.up) - origen;
 
        if (Physics.Raycast(origen, dir.normalized, out RaycastHit hit, dir.magnitude, visionBlockMask))
            return hit.collider.transform.IsChildOf(jugador) || hit.collider.transform == jugador;
 
        return true;
    }
 
    public void OnHeardNoise(Vector3 noisePosition)
    {
        if (currentState == EnemyState.Stunned) return;
 
        float distancia = Vector3.Distance(transform.position, noisePosition);
        if (distancia > radioDeAudicion) return;
 
        _ultimaPosicionSonido = noisePosition;
        _tieneObjetivoSonido  = true;
 
        OrientarseHacia(noisePosition - transform.position);
        _visionMemoryTimer = visionMemoryDuration;
    }
 
    // ─── Helpers ──────────────────────────────────────────────────────────────
 
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
        if (other.CompareTag("ZonaOscura")) cannotSee = true;
    }
 
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("ZonaOscura"))
        {
            cannotSee = false;
            _tieneObjetivoSonido = false;
        }
    }
 
    // ─── Gizmos ───────────────────────────────────────────────────────────────
 
    private void OnDrawGizmos()
    {
        if (eyes != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(eyes.position, detectionRange);
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(eyes.position, eyes.forward * detectionRange);
        }
 
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, radioDeAudicion);
 
        if (_tieneObjetivoSonido)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_ultimaPosicionSonido, 0.2f);
            Gizmos.DrawLine(transform.position, _ultimaPosicionSonido);
        }
    }
}