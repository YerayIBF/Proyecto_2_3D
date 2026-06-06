using UnityEngine;

public class Cable : MonoBehaviour
{
    public SpriteRenderer finalCable;
    public GameObject luz;

    [Header("Configuración")]
    public bool esPuntoAnclaje = false; // márcalo en los cables de la DERECHA

    [HideInInspector] public Color colorOriginal;
    public bool Conectado => _conectado;

    private Vector2 posicionOriginal;
    private Vector2 tamañoOriginal;
    private TareaCables tareaCables;

    private bool _conectado = false;
    private bool _arrastrandoConMando = false;

    [Header("Mando")]
    public float velocidadMando = 5f;

    void Awake()
    {
        colorOriginal = finalCable.color;
    }

    void Start()
    {
        posicionOriginal = transform.position;
        tamañoOriginal   = finalCable.size;
        tareaCables      = transform.root.gameObject.GetComponent<TareaCables>();
    }

    void Update()
    {
        if (Input.GetMouseButtonUp(0))
            Reiniciar();

        if (_arrastrandoConMando && !_conectado)
        {
            ActualizarPosicionMando();
            ComprobarConexion();
            ActualizarRotacion();
            ActualizarTamaño();
        }
    }

    private void OnMouseDown()
    {
        _arrastrandoConMando = false;
    }

    private void OnMouseDrag()
    {
        if (_conectado) return;
        ActualizarPosicionRaton();
        ComprobarConexion();
        ActualizarRotacion();
        ActualizarTamaño();
    }

    private void ActualizarPosicionRaton()
    {
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        transform.position = mousePosition;
    }

    public void IniciarArrastreMando()
    {
        if (_conectado) return;
        _arrastrandoConMando = true;
    }

    public void SoltarMando()
    {
        _arrastrandoConMando = false;
        Reiniciar();
    }

    private void ActualizarPosicionMando()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        transform.position += new Vector3(h, v, 0) * Time.deltaTime * velocidadMando;
    }

    private void ActualizarRotacion()
    {
        Vector2 posicionActual = transform.position;
        Vector2 puntoOrigen    = transform.parent.position;
        Vector2 direccion      = posicionActual - puntoOrigen;

        float angulo = Vector2.SignedAngle(Vector2.right * transform.lossyScale, direccion);
        transform.rotation = Quaternion.Euler(0, 0, angulo);
    }

    private void ActualizarTamaño()
    {
        Vector2 posicionActual = transform.position;
        Vector2 puntoOrigen    = transform.parent.position;

        float distancia = Vector2.Distance(posicionActual, puntoOrigen);
        finalCable.size = new Vector2(distancia, finalCable.size.y);
    }

    public void Reiniciar()
    {
        if (_conectado) return;
        transform.position = posicionOriginal;
        transform.rotation = Quaternion.identity;
        finalCable.size    = tamañoOriginal;
        finalCable.color   = colorOriginal;
    }

    private void ComprobarConexion()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 0.2f);

        foreach (Collider2D col in colliders)
        {
            if (col.gameObject == gameObject) continue;

            Cable otroCable = col.gameObject.GetComponent<Cable>();
            if (otroCable == null) continue;

            // Solo conectar con puntos de anclaje (derecha) que no estén ya conectados
            if (!otroCable.esPuntoAnclaje) continue;
            if (otroCable.Conectado) continue;

            transform.position = col.transform.position;
            ActualizarRotacion();
            ActualizarTamaño();

            if (colorOriginal == otroCable.colorOriginal)
            {
                Conectar();
                otroCable.Conectar();
                tareaCables.conexionesActuales++;
                tareaCables.ComprobarVictoria();

                // Avisar al selector para que avance al siguiente
                if (SelectorCablesMando.Instance != null)
                    SelectorCablesMando.Instance.SiguienteCable();
            }
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