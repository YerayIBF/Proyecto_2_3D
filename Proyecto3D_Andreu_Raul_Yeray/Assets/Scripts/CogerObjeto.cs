using UnityEngine;

public class CogerObjeto : MonoBehaviour
{
    public GameObject handPoint;
    private GameObject objetoCogido;
    public float throwForce = 10f;

    private Camera cam;
    public LineRenderer lineRenderer;
    public int predictionSteps = 30;
    public float timeStep = 0.1f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cam = Camera.main;
        lineRenderer.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
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

        if (objetoCogido != null)
        {
            //Lanzar al pulsar el botón izquierdo del ratón
            if (Input.GetMouseButton(1))
            {
                throwForce += 10f * Time.deltaTime;
                throwForce = Mathf.Clamp(throwForce, 5f, 30f);

                lineRenderer.enabled = true;
                //activar el LineRenderer de trayectoria
                DibujarTrayectoria();
            }
            else
            {
                lineRenderer.enabled = false;
                throwForce = 10f;
            }
            
            if (Input.GetMouseButtonDown(0))
            {
                LanzarObjeto();
            }
        }
        
    }

    public void LanzarObjeto()
    {
        lineRenderer.enabled = false;
        Rigidbody rb = objetoCogido.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;

        Vector3 direction = (handPoint.transform.forward + Vector3.up * 0.4f).normalized;
        Vector3 force = direction * throwForce;
        rb.AddForce(force, ForceMode.VelocityChange);

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
                other.GetComponent<ThrowObject>().MostrarIcono(false);
                other.GetComponent<Rigidbody>().useGravity = false;
                other.GetComponent<Rigidbody>().isKinematic = true;
                other.transform.position = handPoint.transform.position;
                other.gameObject.transform.SetParent(handPoint.gameObject.transform);
                objetoCogido = other.gameObject;
            }
        }else if (other.gameObject.CompareTag("PilaMegafono"))
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                other.GetComponent<PilaMegafono>().Recargar();
                Destroy(other.gameObject);
            }
        }
    }

    private void OnTriggerEnter(Collider other) 
    {
        if (objetoCogido == null && other.CompareTag("objetoCogible"))
        {
            other.GetComponent<ThrowObject>().MostrarIcono(true);
        }
    }

    private void OnTriggerExit(Collider other) 
    {
        if (objetoCogido == null && other.CompareTag("objetoCogible"))
        {
            other.GetComponent<ThrowObject>().MostrarIcono(false);
        }
    }

    void DibujarTrayectoria()
    {
        Vector3 startPoint = handPoint.transform.position;
        Vector3 direction = (handPoint.transform.forward + Vector3.up * 0.4f).normalized;
        Vector3 startVelocity = direction * throwForce;

        lineRenderer.positionCount = predictionSteps;
        for (int i = 0; i < predictionSteps; i++)
        {
            float t = i * timeStep;
            Vector3 point = startPoint + startVelocity * t + 0.5f * Physics.gravity * t * t;
            lineRenderer.SetPosition(i, point);
        }
    }
}
