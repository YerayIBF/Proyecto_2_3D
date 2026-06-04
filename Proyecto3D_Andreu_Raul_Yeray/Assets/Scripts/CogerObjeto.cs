using UnityEngine;
using UnityEngine.InputSystem; 

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
    private Transform megafonoObject;
    [HideInInspector]
    public GameObject objeto;
    public bool apuntando = false;
    private Vector3 handPointRotation;

    public InputActionReference apuntarAction;
    public InputActionReference lanzarAction;
    public InputActionReference soltarAction;

    private void OnEnable()
    {
        if (apuntarAction != null) apuntarAction.action.Enable();
        if (lanzarAction != null) lanzarAction.action.Enable();
        if (soltarAction != null) soltarAction.action.Enable();
    }

    private void OnDisable()
    {
        if (apuntarAction != null) apuntarAction.action.Disable();
        if (lanzarAction != null) lanzarAction.action.Disable();
        if (soltarAction != null) soltarAction.action.Disable();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cam = Camera.main;
        lineRenderer.enabled = false;
        GameObject objetoCerca = GameObject.FindGameObjectWithTag("objetoCogible");
        GameObject megafonoCerca = GameObject.FindGameObjectWithTag("Megafono");
        if (objetoCerca != null)
        {
            objetoInteractuable = objetoCerca.transform;
        }
        if (megafonoCerca != null)
        {
            megafonoObject = megafonoCerca.transform;
        }

        handPointRotation = handPoint.transform.localEulerAngles;
    }

    // Update is called once per frame
    void Update()
    {
        bool inputApuntar = apuntarAction != null && apuntarAction.action.IsPressed();
        bool inputLanzarDown = lanzarAction != null && lanzarAction.action.WasPressedThisFrame();
        bool inputSoltarDown = soltarAction != null && soltarAction.action.WasPressedThisFrame();

        if (objetoCogido != null)
        {
            if (Input.GetKey("r") || inputSoltarDown)
            {
                objetoCogido.GetComponent<Rigidbody>().useGravity = true;
                objetoCogido.GetComponent<Rigidbody>().isKinematic = false;
                EmitirSonido.instance.EmitirRuido(objetoCogido.transform.position, 15f);

                objetoCogido.gameObject.transform.SetParent(null);
                objetoCogido = null;
                objeto = null;

                GameManager.instance.tieneMegafono = false;
                handPoint.transform.localEulerAngles = handPointRotation;
            }
        }

        if (objetoCogido != null)
        {
            if (GameManager.instance.tieneMegafono)
            {
                if (Input.GetMouseButton(1) || inputApuntar)
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
                if (Input.GetMouseButton(1) || inputApuntar)
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
                
                if (Input.GetMouseButtonDown(0) || inputLanzarDown)
                {
                    LanzarObjeto();
                }
            }
        }

        if (objetoInteractuable != null && megafonoObject != null)
        {
            if (objetoCogido == null && Vector3.Distance(transform.position, objetoInteractuable.position) < 2f)
            {
                objetoInteractuable.GetComponent<ThrowObject>().MostrarIcono(true);
            }else if (objetoCogido == null && Vector3.Distance(transform.position, megafonoObject.position) < 2f)
            {
                megafonoObject.GetComponent<ThrowObject>().MostrarIcono(true);
            }
            else
            {
                objetoInteractuable.GetComponent<ThrowObject>().MostrarIcono(false);
                megafonoObject.GetComponent<ThrowObject>().MostrarIcono(false);
            }   
        }
        
    }

    public void LanzarObjeto()
    {
        lineRenderer.enabled = false;
        Rigidbody rb = objetoCogido.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;

        Vector3 direction = (cam.transform.forward + Vector3.up * 0.4f).normalized;
        Vector3 force = direction * throwForce;
        rb.AddForce(force, ForceMode.VelocityChange);

        ThrowObject throwable = objetoCogido.GetComponent<ThrowObject>();
        if (throwable != null)
        {
            throwable.Lanzado();
        }

        objetoCogido.transform.SetParent(null);
        objetoCogido = null;

        handPoint.transform.localEulerAngles = handPointRotation;
    }

    private void OnTriggerStay(Collider other) {
        if (other.gameObject.CompareTag("objetoCogible"))
        {
            if (objetoCogido == null){
                other.GetComponent<ThrowObject>().MostrarIcono(false);
                objeto = other.gameObject;
            }
        }
        else if (other.gameObject.CompareTag("Megafono"))
        {
            if (objetoCogido == null)
            {
                other.GetComponent<ThrowObject>().MostrarIcono(false);
                objeto = other.gameObject;
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

            if (objeto.CompareTag("objetoCogible"))
            {
                Vector3 rotacion = handPoint.transform.localEulerAngles;
                handPoint.transform.localRotation = Quaternion.Euler(rotacion.x, -150.3f, rotacion.z);
            }
            else if (objeto.CompareTag("Megafono"))
            {
                Vector3 rotacion = handPoint.transform.localEulerAngles;
                handPoint.transform.localEulerAngles = new Vector3(rotacion.x, 150.3f, rotacion.z);
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
        Vector3 direction = (cam.transform.forward + Vector3.up * 0.4f).normalized;
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