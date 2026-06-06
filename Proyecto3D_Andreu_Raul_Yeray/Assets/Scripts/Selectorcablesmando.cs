using UnityEngine;

public class SelectorCablesMando : MonoBehaviour
{
    public static SelectorCablesMando Instance { get; private set; }

    [Header("Referencias")]
    public GameObject[] cablesObj;
    private Cable[] cables;

    [Header("Resaltado")]
    public Color colorSeleccion = Color.white;

    private int   _indiceActual       = 0;
    private Cable _cableArrastrando   = null;
    private float _cooldownNavegacion = 0f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        cables = new Cable[cablesObj.Length];
        for (int i = 0; i < cablesObj.Length; i++)
            cables[i] = cablesObj[i].GetComponentInChildren<Cable>();

        SeleccionarCableActual();
    }

    void Update()
    {
        if (_cooldownNavegacion > 0f) _cooldownNavegacion -= Time.deltaTime;

        if (_cableArrastrando == null)
            NavegerCables();
        else
            GestionarArrastre();
    }

    void NavegerCables()
    {
        if (cables == null || cables.Length == 0) return;

        float v = Input.GetAxisRaw("Vertical");

        if (_cooldownNavegacion <= 0f)
        {
            if (v < -0.5f)
            {
                AvanzarIndice(1);
                _cooldownNavegacion = 0.25f;
            }
            else if (v > 0.5f)
            {
                AvanzarIndice(-1);
                _cooldownNavegacion = 0.25f;
            }
        }

        if (Input.GetButtonDown("Submit"))
        {
            Cable cable = cables[_indiceActual];
            if (cable != null && !cable.Conectado)
            {
                _cableArrastrando = cable;
                cable.IniciarArrastreMando();
                DesresaltarTodos();
            }
        }
    }

    void GestionarArrastre()
    {
        if (Input.GetButtonDown("Submit"))
        {
            _cableArrastrando.SoltarMando();
            _cableArrastrando = null;
            SeleccionarCableActual();
        }
    }

    public void SiguienteCable()
    {
        _cableArrastrando = null;
        StartCoroutine(LimpiarYSeleccionar());
    }

    private System.Collections.IEnumerator LimpiarYSeleccionar()
    {
        yield return null;

        for (int i = 0; i < cables.Length; i++)
        {
            if (cables[i] != null && cables[i].Conectado)
                cables[i] = null;
        }

        SeleccionarCableActual();
    }

    void AvanzarIndice(int direccion)
    {
        int inicio = _indiceActual;
        do
        {
            _indiceActual = (_indiceActual + direccion + cables.Length) % cables.Length;
        }
        while (cables[_indiceActual] == null && _indiceActual != inicio);

        SeleccionarCableActual();
    }

    void SeleccionarCableActual()
    {
        for (int i = 0; i < cables.Length; i++)
        {
            if (cables[i] == null) continue;

            cables[i].finalCable.color = (i == _indiceActual)
                ? colorSeleccion
                : cables[i].colorOriginal;
        }
    }

    void DesresaltarTodos()
    {
        for (int i = 0; i < cables.Length; i++)
        {
            if (cables[i] == null) continue;
            cables[i].finalCable.color = cables[i].colorOriginal;
        }
    }
}