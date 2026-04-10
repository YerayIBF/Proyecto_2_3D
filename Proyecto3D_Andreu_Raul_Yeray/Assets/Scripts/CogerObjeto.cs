using UnityEngine;

public class CogerObjeto : MonoBehaviour
{
    public GameObject handPoint;
    private GameObject objetoCogido;

    private Camera cam;
    public LineRenderer lineRenderer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
        }

        if (objetoCogido != null)
        {
            if (Input.GetKey("r"))
            {
                objetoCogido.GetComponent<Rigidbody>().useGravity = true;
                objetoCogido.GetComponent<Rigidbody>().isKinematic = false;
                EmitirSonido.instance.EmitirRuido(objetoCogido.transform.position, 15f);

                objetoCogido.gameObject.transform.SetParent(null);
                objetoCogido = null;
            }
        }

        //Lanzar al pulsar el botón izquierdo del ratón
        if (Input.GetMouseButton(1))
        {
            //activar el LineRenderer de trayectoria
            DibujarTrayectoria();

            if (Input.GetMouseButtonDown(0))
            {
                LanzarObjeto();
            }
        }
    }

    Vector3 CalcularFuerzaParabolica(Vector3 origen, Vector3 destino, float fuerza)
    {
        Vector3 direccion = destino - origen;

        // Componente horizontal
        Vector3 direccionHorizontal = new Vector3(direccion.x, 0f, direccion.z);
        float distanciaHorizontal = direccionHorizontal.magnitude;
        float alturaVertical = direccion.y;

        if (distanciaHorizontal < 0.5f)
        {
            return cam.transform.forward * fuerza;
        }

        // Ángulo de lanzamiento (45 grados da el mayor alcance)
        float angulo = 45f * Mathf.Deg2Rad;

        float divisor = 2f * Mathf.Cos(angulo) * Mathf.Cos(angulo) *
                    (distanciaHorizontal * Mathf.Tan(angulo) - alturaVertical);

        if (divisor <= 0f)
        {
            return cam.transform.forward * fuerza;
        }

        // Calcula la velocidad necesaria para llegar al destino
        float velocidad = Mathf.Sqrt(
            (Physics.gravity.magnitude * distanciaHorizontal * distanciaHorizontal) /
            (2f * Mathf.Cos(angulo) * Mathf.Cos(angulo) *
            (distanciaHorizontal * Mathf.Tan(angulo) - alturaVertical))
        );

        // Limita la velocidad máxima para que no sea infinita
        velocidad = Mathf.Clamp(velocidad, 0f, fuerza * 2f);

        // Construye el vector de fuerza final
        Vector3 fuerzaFinal = direccionHorizontal.normalized * velocidad * Mathf.Cos(angulo)
                            + Vector3.up * velocidad * Mathf.Sin(angulo);

        if (float.IsNaN(fuerzaFinal.x) || float.IsNaN(fuerzaFinal.y) || float.IsNaN(fuerzaFinal.z))
        {
            return cam.transform.forward * fuerza;
        }

        return fuerzaFinal;
    }

    public void LanzarObjeto()
    {
        Rigidbody rb = objetoCogido.GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.isKinematic = false;

        Vector3 targetPoint;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 50f))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = cam.transform.position + cam.transform.forward * 20f;
        }

        Vector3 fuerza = CalcularFuerzaParabolica(objetoCogido.transform.position, targetPoint, 15f);
        rb.AddForce(fuerza, ForceMode.Impulse);

        ThrowObject throwable = objetoCogido.GetComponent<ThrowObject>();
        if (throwable != null)
        {
            throwable.Lanzado();
        }

        objetoCogido.transform.SetParent(null);
        objetoCogido = null;
    }

    private void OnTriggerStay(Collider other) {
        if (other.gameObject.CompareTag("objetoCogible"))
        {
            if (Input.GetKeyDown(KeyCode.E) && objetoCogido == null){
                other.GetComponent<Rigidbody>().useGravity = false;
                other.GetComponent<Rigidbody>().isKinematic = true;
                other.transform.position = handPoint.transform.position;
                other.gameObject.transform.SetParent(handPoint.gameObject.transform);
                objetoCogido = other.gameObject;
            }
        }
    }

    void DibujarTrayectoria()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 50f)) return;

        int puntos = 30;
        lineRenderer.positionCount = puntos;

        if (objetoCogido != null)
        {
            Vector3 fuerzaPreview = CalcularFuerzaParabolica(
            objetoCogido.transform.position, hit.point, 15f);

            Vector3 pos = objetoCogido.transform.position;
            Vector3 velocidad = fuerzaPreview / objetoCogido.GetComponent<Rigidbody>().mass;

            for (int i = 0; i < puntos; i++)
            {
                lineRenderer.SetPosition(i, pos);
                velocidad += Physics.gravity * 0.05f;
                pos += velocidad * 0.05f;
            }
        }
    }
}
