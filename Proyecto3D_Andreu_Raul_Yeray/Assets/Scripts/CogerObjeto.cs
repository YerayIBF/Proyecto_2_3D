using UnityEngine;

public class CogerObjeto : MonoBehaviour
{
    public Animator animator;
    public GameObject handPoint;
    private GameObject objetoCogido;
    public float throwForce = 10f;

    private Camera cam;
    public LineRenderer lineRenderer;
    public int predictionSteps = 30;
    public float timeStep = 0.1f;
    private Transform objetoInteractuable;
    private GameObject objeto;
    public bool apuntando = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cam = Camera.main;
        lineRenderer.enabled = false;
        objetoInteractuable = GameObject.FindGameObjectWithTag("objetoCogible").transform;
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

                GameManager.instance.tieneMegafono = false;
            }
        }

        if (objetoCogido != null)
        {
            if (GameManager.instance.tieneMegafono)
            {
                if (Input.GetMouseButton(1))
                {
                    Debug.Log("Apuntando con el megafono");
                    apuntando = true;
                    animator.SetBool("isAiming", true);
                    GameManager.instance.MostrarCanvasMegafono(true);
                }
                else
                {
                    //se quita la animacion de apuntar
                    apuntando = false;
                    animator.SetBool("isAiming", false);
                    GameManager.instance.MostrarCanvasMegafono(false);
                }

                lineRenderer.enabled = false;
            }
            else
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

        if (objetoCogido == null && Vector3.Distance(transform.position, objetoInteractuable.position) < 2f)
        {
            objetoInteractuable.GetComponent<ThrowObject>().MostrarIcono(true);
        }
        else
        {
            objetoInteractuable.GetComponent<ThrowObject>().MostrarIcono(false);
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
            if (objetoCogido == null){
                other.GetComponent<ThrowObject>().MostrarIcono(false);
                /*other.GetComponent<Rigidbody>().useGravity = false;
                other.GetComponent<Rigidbody>().isKinematic = true;
                other.transform.position = handPoint.transform.position;
                other.gameObject.transform.SetParent(handPoint.gameObject.transform);*/
                objeto = other.gameObject;
            }
        }
        else if (other.gameObject.CompareTag("Megafono"))
        {
            if (objetoCogido == null)
            {
                objeto = other.gameObject;
                //animator.SetTrigger("Coger");
                /*other.GetComponent<Rigidbody>().useGravity = false;
                other.GetComponent<Rigidbody>().isKinematic = true;
                Transform handlePoint = other.transform.Find("HandlePoint");
                other.gameObject.transform.SetParent(handPoint.gameObject.transform);
                other.transform.localPosition = -handlePoint.localPosition;
                other.transform.localRotation = Quaternion.Inverse(handlePoint.localRotation);
                objetoCogido = other.gameObject;

                GameManager.instance.RecogerMegafono();*/
            }
        }
    }

    public void AnimatorCogerObjeto()
    {
        if (objeto != null)
        {
            objeto.GetComponent<Rigidbody>().useGravity = false;
            objeto.GetComponent<Rigidbody>().isKinematic = true;
            Transform handlePoint = objeto.transform.Find("HandlePoint");
            objeto.transform.SetParent(handPoint.gameObject.transform);

            if (handlePoint != null)
            {
                objeto.transform.localPosition = -handlePoint.localPosition;
                objeto.transform.localRotation = Quaternion.Inverse(handlePoint.localRotation);
            }

            objetoCogido = objeto;
            objeto = null;

            if (objetoCogido.CompareTag("Megafono"))
            {
                GameManager.instance.RecogerMegafono();
            }
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
