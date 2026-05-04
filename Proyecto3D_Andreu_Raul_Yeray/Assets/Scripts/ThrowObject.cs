using UnityEngine;

public class ThrowObject : MonoBehaviour
{
    private Transform player;
    private bool lanzado = false;
    private float lanzadoTimer = 0f;
    public float delayDeteccion = 0.2f;

    public GameObject icono;

    private Renderer renderer;
    private Material materialOutline;
    [SerializeField] private string propiedadSize = "_OutlineSize";
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        renderer = GetComponent<Renderer>();
        player = GameObject.FindGameObjectWithTag("Player").transform;

        Material[] materials = renderer.materials;
        if (materials.Length > 1)
        {
            materialOutline = materials[1];
        }
        else
        {
            Debug.Log("Este objeto no tiene el material outline");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (lanzado)
        {
            lanzadoTimer += Time.deltaTime;
        }

        float distancia = Vector3.Distance(transform.position, player.position);

        if (distancia < 4f && materialOutline != null)
        {
            materialOutline.SetFloat(propiedadSize, 1.1f);
        }
        else
        {
            materialOutline.SetFloat(propiedadSize, 0f);
        }
    }

    public void Lanzado()
    {
        lanzado = true;
        lanzadoTimer = 0f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!lanzado || lanzadoTimer < delayDeteccion) return;

        EmitirSonido.instance.EmitirRuido(transform.position, 15f);
        Debug.Log("Objeto aterrizó, sonido emitido");

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.linearDamping = 2f;
        rb.angularDamping = 2f;

        if (rb.linearVelocity.magnitude > 10f)
        {
            rb.linearVelocity *= 0.5f;
        }
        lanzado = false; 
    }

    public void MostrarIcono(bool estado)
    {
        if (icono != null)
        {
            icono.SetActive(estado);
        }
    }
}
