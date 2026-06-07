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

    void Awake()
    {
        colorOriginal = finalCable.color;
    }

    void Start()
    {
        posicionLocalOriginal = transform.localPosition;
        rotacionLocalOriginal = transform.localRotation;
        tamañoOriginal        = finalCable.size;
        tareaCables           = transform.root.gameObject.GetComponent<TareaCables>();
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

    // Lee el stick izquierdo del mando + teclado como fallback
    public static Vector2 LeerStick()
    {
        Vector2 mov = Vector2.zero;

        if (Gamepad.current != null)
            mov = Gamepad.current.leftStick.ReadValue();

        // Fallback teclado para testear
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

    private void ComprobarConexion()
    {
        // Usa el collider real del cable (sin radio de ayuda)
        Collider2D miCollider = GetComponent<Collider2D>();
        if (miCollider == null) return;

        ContactFilter2D filtro = new ContactFilter2D();
        filtro.useTriggers = true;
        Collider2D[] resultados = new Collider2D[10];
        int count = miCollider.Overlap(filtro, resultados);

        for (int i = 0; i < count; i++)
        {
            Collider2D col = resultados[i];
            if (col == null || col.gameObject == gameObject) continue;

            Cable otroCable = col.GetComponent<Cable>();
            if (otroCable == null || !otroCable.esPuntoAnclaje || otroCable.Conectado) continue;
            if (colorOriginal != otroCable.colorOriginal) continue;

            transform.position = col.transform.position;
            ActualizarRotacion();
            ActualizarTamaño();

            Conectar();
            otroCable.Conectar();
            tareaCables.conexionesActuales++;
            tareaCables.ComprobarVictoria();

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