using UnityEngine;

/// <summary>
/// Componente para items recogibles que NO son lanzables (linterna, megáfono, llave, pila).
/// Muestra:
/// - Un icono flotante sobre el item cuando te acercas
/// - Un outline blanco en el mesh (opcional)
/// </summary>
public class PickupIcon : MonoBehaviour
{
    [Header("Icono flotante")]
    [Tooltip("GameObject del icono que aparece sobre el item al acercarte")]
    public GameObject icono;

    [Tooltip("Si está activo, el icono mira siempre a la cámara")]
    public bool billboardToCamera = true;

    [Tooltip("Si está activo, muestra logs en consola cuando se activa/desactiva")]
    public bool showDebugLogs = false;

    [Header("Outline (resaltado del mesh)")]
    [Tooltip("Si está activo, también activa el outline blanco al acercarte")]
    public bool usarOutline = true;

    [Tooltip("Distancia para mostrar el outline")]
    public float rangoDeteccion = 4f;

    [Tooltip("Tamaño del outline cuando se activa")]
    public float tamañoOutline = 1.1f;

    [Tooltip("Nombre de la propiedad del shader (suele ser _OutlineSize)")]
    public string propiedadSize = "_OutlineSize";

    private Renderer _renderer;
    private Material _materialOutline;
    private Camera _cam;
    private Transform _player;

    private void Start()
    {
        // Asegurarnos de que el icono empieza desactivado
        if (icono != null) icono.SetActive(false);

        BuscarCamara();
        BuscarJugador();

        // Buscar el outline material
        if (usarOutline)
        {
            _renderer = GetComponent<Renderer>();
            if (_renderer == null)
                _renderer = GetComponentInChildren<Renderer>();

            if (_renderer != null)
            {
                Material[] materials = _renderer.materials;
                if (materials.Length > 1)
                    _materialOutline = materials[1];
                else if (showDebugLogs)
                    Debug.Log($"[PickupIcon] {gameObject.name} no tiene material outline (se necesitan 2 materiales)");
            }
        }
    }

    private void BuscarCamara()
    {
        _cam = Camera.main;
        if (_cam == null)
        {
            // Fallback: buscar cualquier cámara con tag MainCamera
            GameObject camObj = GameObject.FindGameObjectWithTag("MainCamera");
            if (camObj != null) _cam = camObj.GetComponent<Camera>();
        }
    }

    private void BuscarJugador()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;
    }

    private void LateUpdate()
    {
        // Re-buscar cámara si la perdió (cinemáticas cambian la principal)
        if (_cam == null || !_cam.isActiveAndEnabled)
        {
            BuscarCamara();
        }

        // Billboard del icono
        if (billboardToCamera && icono != null && icono.activeSelf && _cam != null)
        {
            // El icono apunta hacia la cámara (forward del icono mira al jugador)
            Vector3 directionToCamera = _cam.transform.position - icono.transform.position;
            if (directionToCamera.sqrMagnitude > 0.01f)
            {
                icono.transform.rotation = Quaternion.LookRotation(-directionToCamera);
            }
        }
    }

    private void Update()
    {
        // Re-buscar jugador si lo perdió (por respawn, por ejemplo)
        if (_player == null) BuscarJugador();

        // Outline por distancia
        if (!usarOutline || _materialOutline == null || _player == null) return;

        float distancia = Vector3.Distance(transform.position, _player.position);

        if (distancia < rangoDeteccion)
            _materialOutline.SetFloat(propiedadSize, tamañoOutline);
        else
            _materialOutline.SetFloat(propiedadSize, 0f);
    }

    /// <summary>
    /// Mostrar/ocultar el icono. Llamado por CogerObjeto cada frame.
    /// </summary>
    public void MostrarIcono(bool estado)
    {
        if (icono != null && icono.activeSelf != estado)
        {
            icono.SetActive(estado);

            if (showDebugLogs)
                Debug.Log($"[PickupIcon] {gameObject.name} icono → {(estado ? "MOSTRAR" : "OCULTAR")}");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);
    }
}