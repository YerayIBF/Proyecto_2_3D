using UnityEngine;

/// <summary>
/// Componente para items recogibles que NO son lanzables (linterna, megáfono).
/// Muestra:
/// - Un icono flotante sobre el item cuando te acercas
/// - Un outline blanco en el mesh (mismo sistema que ThrowObject del compañero)
///
/// Setup:
/// - El item necesita 2 materiales en su Renderer: el normal y el outline
/// - Asigna el GameObject del icono al campo "icono"
/// - El outline se activa cuando el jugador está a menos de "rangoDeteccion" de distancia
/// </summary>
public class PickupIcon : MonoBehaviour
{
    [Header("Icono flotante")]
    [Tooltip("GameObject del icono que aparece sobre el item al acercarte")]
    public GameObject icono;

    [Tooltip("Si está activo, el icono mira siempre a la cámara")]
    public bool billboardToCamera = true;

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
        if (icono != null) icono.SetActive(false);
        _cam = Camera.main;

        // Buscar el jugador
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;

        // Buscar el outline material (igual que en ThrowObject)
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
                else
                    Debug.Log($"[PickupIcon] {gameObject.name} no tiene material outline (se necesitan 2 materiales)");
            }
        }
    }

    private void LateUpdate()
    {
        // Billboard del icono
        if (billboardToCamera && icono != null && icono.activeSelf && _cam != null)
        {
            icono.transform.rotation = Quaternion.LookRotation(
                icono.transform.position - _cam.transform.position);
        }
    }

    private void Update()
    {
        // Outline por distancia (igual que ThrowObject)
        if (!usarOutline || _materialOutline == null || _player == null) return;

        float distancia = Vector3.Distance(transform.position, _player.position);

        if (distancia < rangoDeteccion)
            _materialOutline.SetFloat(propiedadSize, tamañoOutline);
        else
            _materialOutline.SetFloat(propiedadSize, 0f);
    }

    /// <summary>
    /// Mostrar/ocultar el icono. Mismo nombre que ThrowObject.MostrarIcono
    /// para que CogerObjeto pueda llamarla igual.
    /// </summary>
    public void MostrarIcono(bool estado)
    {
        if (icono != null)
            icono.SetActive(estado);
    }
}
