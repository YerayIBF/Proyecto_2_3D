using UnityEngine;
using UnityEngine.InputSystem;

public class Cable : MonoBehaviour
{
    public SpriteRenderer finalCable;
    public GameObject luz;

    [Header("Configuración")]
    public bool esPuntoAnclaje = false;

    [HideInInspector] public Color colorOriginal;
    public bool Conectado => _conectado;

    private Vector3 posicionLocalOriginal;
    private Quaternion rotacionLocalOriginal;
    private Vector2 tamañoOriginal;
    private TareaCables tareaCables;

    private bool  _conectado          = false;
    private bool  _arrastrandoConMando = false;
    private float _tiempoArrastre     = 0f;

    [Header("Mando")]
    public float velocidadMando = 3f;
    public float tiempoAntesDeConectar = 0.4f;

    [Header("Sonido")]
    public AudioSource audioSource;
    public AudioClip sonidoChispazo;

    void Awake()
    {
        colorOriginal = finalCable.color;
    }

    void Start()
    {
        posicionLocalOriginal = transform.localPosition;
        rotacionLocalOriginal = transform.localRotation;
        tamañoOriginal        = finalCable.size;
        tareaCables           = GetComponentInParent<TareaCables>();
    }

    void Update()
    {
        if (_arrastrandoConMando && !_conectado)
        {
            _tiempoArrastre += Time.deltaTime;
            ActualizarPosicionMando();
            ActualizarRotacion();
            ActualizarTamaño();

            if (_tiempoArrastre > tiempoAntesDeConectar)
                ComprobarConexion();
        }
    }

    public void IniciarArrastreMando()
    {
        if (_conectado) return;
        _arrastrandoConMando = true;
        _tiempoArrastre      = 0f;
    }

    public void SoltarMando()
    {
        _arrastrandoConMando = false;
        Reiniciar();
    }

    private void ActualizarPosicionMando()
    {
        Vector2 mov = LeerStick();
        transform.localPosition += new Vector3(mov.x, mov.y, 0) * Time.deltaTime * velocidadMando;
    }

    public static Vector2 LeerStick()
    {
        Vector2 mov = Vector2.zero;

        if (Gamepad.current != null)
            mov = Gamepad.current.leftStick.ReadValue();

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed) mov.x = -1;
            if (Keyboard.current.dKey.isPressed) mov.x =  1;
            if (Keyboard.current.wKey.isPressed) mov.y =  1;
            if (Keyboard.current.sKey.isPressed) mov.y = -1;
        }

        return mov;
    }

    private void ActualizarRotacion()
    {
        Vector3 dirLocal = transform.localPosition - posicionLocalOriginal;
        if (dirLocal.sqrMagnitude < 0.0001f) return;

        float angulo = Mathf.Atan2(dirLocal.y, dirLocal.x) * Mathf.Rad2Deg;
        transform.localRotation = Quaternion.Euler(0, 0, angulo);
    }

    private void ActualizarTamaño()
    {
        float distancia = Vector3.Distance(transform.localPosition, posicionLocalOriginal);
        finalCable.size = new Vector2(distancia, finalCable.size.y);
    }

    public void Reiniciar()
    {
        if (_conectado) return;
        transform.localPosition = posicionLocalOriginal;
        transform.localRotation = rotacionLocalOriginal;
        finalCable.size         = tamañoOriginal;
        finalCable.color        = colorOriginal;
    }

    // ─── Conexión con colliders 3D (funciona con cualquier rotación) ───────────

    private void ComprobarConexion()
    {
        BoxCollider miCollider = GetComponent<BoxCollider>();
        if (miCollider == null) return;

        // Centro y mitad del tamaño del collider en espacio mundo
        Vector3 centro = transform.TransformPoint(miCollider.center);
        Vector3 halfExtents = Vector3.Scale(miCollider.size, transform.lossyScale) * 0.5f;

        Collider[] resultados = Physics.OverlapBox(centro, halfExtents, transform.rotation);

        foreach (Collider col in resultados)
        {
            if (col.gameObject == gameObject) continue;

            Cable otroCable = col.GetComponent<Cable>();
            if (otroCable == null || !otroCable.esPuntoAnclaje || otroCable.Conectado) continue;
            if (colorOriginal != otroCable.colorOriginal) continue;

            transform.position = col.transform.position;
            ActualizarRotacion();
            ActualizarTamaño();

            // Sonido de chispazo al conectar
            if (audioSource != null && sonidoChispazo != null)
                audioSource.PlayOneShot(sonidoChispazo);

            Conectar();
            otroCable.Conectar();

            if (tareaCables != null)
            {
                tareaCables.conexionesActuales++;
                tareaCables.ComprobarVictoria();
            }

            if (SelectorCablesMando.Instance != null)
                SelectorCablesMando.Instance.SiguienteCable();
        }
    }

    public void Conectar()
    {
        _conectado           = true;
        _arrastrandoConMando = false;
        luz.SetActive(true);
        Destroy(this);
    }
}