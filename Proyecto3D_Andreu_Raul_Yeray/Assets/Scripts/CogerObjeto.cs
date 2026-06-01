using UnityEngine;

/// <summary>
/// CogerObjeto — versión integrada con PlayerEquipmentManager.
///
/// Cambios respecto a tu versión:
/// - Detección por distancia (más fiable que triggers)
/// - Bloquea animación "Coger" si no hay objeto cerca
/// - Bloquea recoger lanzable si el equipment manager dice que no
/// - Mantiene toda tu lógica: HandlePoint, rotaciones, trayectoria, apuntado megáfono
/// </summary>
public class CogerObjeto : MonoBehaviour
{
    [Header("Animator")]
    public Animator animator;

    [Header("Manos")]
    public GameObject handPoint;        // mano derecha (megáfono/linterna)
    public GameObject handPointLeft;    // mano izquierda (lanzables)

    [Header("Lanzamiento")]
    public float throwForce = 10f;

    [Header("Trayectoria")]
    public LineRenderer lineRenderer;
    public int predictionSteps = 30;
    public float timeStep = 0.1f;
    private Transform objetoInteractuable;
    private Transform megafonoObject;
    [HideInInspector]
    public GameObject objeto;
    public bool apuntando = false;
    private Vector3 handPointRotation;
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

    // ─── Update ──────────────────────────────────────────────────────────────

    void Update()
    {
        // Detectar el objeto más cercano por distancia (más fiable que triggers)
        DetectarObjetoCercano();

        // Mostrar/ocultar iconos según cercanía
        ActualizarIconos();

        // Soltar (R) — solo lanzables
        if (objetoCogido != null && Input.GetKey("r"))
        {
            if (objetoCogido.CompareTag("objetoCogible"))
                SoltarLanzable();
        }

        // Apuntado + lanzar (solo si tenemos un lanzable)
        if (objetoCogido != null && objetoCogido.CompareTag("objetoCogible"))
            HandleAimAndThrow();

        // Apuntado megáfono (si está equipado en mano dcha)
        if (PlayerEquipmentManager.Instance != null
            && PlayerEquipmentManager.Instance.IsMegaphoneInHand)
        {
            HandleMegaphoneAim();
        }
        else if (apuntando)
        {
            apuntando = false;
            if (animator != null) animator.SetBool("isAiming", false);
            if (GameManager.instance != null) GameManager.instance.MostrarCanvasMegafono(false);
        }

        // Recoger con E (SOLO si hay objeto cerca y bloqueos permiten)
        if (Input.GetKeyDown(KeyCode.E) && objetoCogido == null)
        {
            if (objeto == null)
            {
                Debug.Log("[CogerObjeto] No hay nada cerca para recoger.");
                return;
            }

            // Comprobar bloqueos según tipo
            if (objeto.CompareTag("objetoCogible"))
            {
                if (PlayerEquipmentManager.Instance != null
                    && !PlayerEquipmentManager.Instance.CanPickupThrowable)
                {
                    Debug.Log("[CogerObjeto] No puedes recoger lanzables ahora.");
                    return;
                }
            }

            // OK, dispara animación (al final llama a AnimatorCogerObjeto)
            if (animator != null)
                animator.SetTrigger("Coger");
            else
                AnimatorCogerObjeto();
        }
    }

    // ─── Detección por distancia ─────────────────────────────────────────────

    private void DetectarObjetoCercano()
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
                objeto = null;

                GameManager.instance.tieneMegafono = false;
                handPoint.transform.localEulerAngles = handPointRotation;
            }
        }

        GameObject masCercano = null;
        float minDist = pickupRange;

        string[] tags = { "objetoCogible", "Megafono", "Linterna" };
        foreach (string tag in tags)
        {
            GameObject[] candidatos = GameObject.FindGameObjectsWithTag(tag);
            foreach (var go in candidatos)
            {
                if (go == null) continue;
                float d = Vector3.Distance(transform.position, go.transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    masCercano = go;
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

        if (Input.GetMouseButtonDown(0))
            LanzarObjeto();
    }

    public void LanzarObjeto()
    {
        if (objetoCogido == null) return;

        if (lineRenderer != null) lineRenderer.enabled = false;

        Rigidbody rb = objetoCogido.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;

        Vector3 direction = (cam.transform.forward + Vector3.up * 0.4f).normalized;
        Vector3 force = direction * throwForce;
        rb.AddForce(force, ForceMode.VelocityChange);

        ThrowObject throwable = objetoCogido.GetComponent<ThrowObject>();
        if (throwable != null) throwable.Lanzado();

        objetoCogido.transform.SetParent(null);
        objetoCogido = null;

        handPoint.transform.localEulerAngles = handPointRotation;
    }

    private void SoltarLanzable()
    {
        if (objetoCogido == null) return;

        Rigidbody rb = objetoCogido.GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.isKinematic = false;

        if (EmitirSonido.instance != null)
            EmitirSonido.instance.EmitirRuido(objetoCogido.transform.position, 15f);

        objetoCogido.transform.SetParent(null);
        objetoCogido = null;

        if (handPointLeft != null)
            handPointLeft.transform.localEulerAngles = handPointLeftRotation;

        PlayerEquipmentManager.Instance?.ReleaseThrowable();
    }

    // ─── Iconos ──────────────────────────────────────────────────────────────

    private void ActualizarIconos()
    {
        // Oculta el icono de todos los items que NO son el más cercano
        string[] tags = { "objetoCogible", "Megafono", "Linterna" };
        foreach (string tag in tags)
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

    // ─── AnimatorCogerObjeto — Animation Event ───────────────────────────────

    public void AnimatorCogerObjeto()
    {
        if (objeto == null) return;

        // LANZABLES → mano izquierda
        if (objeto.CompareTag("objetoCogible"))
        {
            GameObject handTarget = handPointLeft != null ? handPointLeft : handPoint;

            Rigidbody rb = objeto.GetComponent<Rigidbody>();
            if (rb != null) { rb.useGravity = false; rb.isKinematic = true; }

            Transform handlePoint = objeto.transform.Find("HandlePoint");
            objeto.transform.SetParent(handTarget.transform);

            if (handlePoint != null)
            {
                objeto.transform.localPosition = -handlePoint.localPosition;
                objeto.transform.localRotation = Quaternion.Inverse(handlePoint.localRotation);
            }
            else
            {
                objeto.transform.localPosition = Vector3.zero;
                objeto.transform.localRotation = Quaternion.identity;
            }

            Vector3 rotacion = handTarget.transform.localEulerAngles;
            handTarget.transform.localRotation = Quaternion.Euler(rotacion.x, -150.3f, rotacion.z);

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
            PlayerEquipmentManager.Instance?.PickupThrowable(objetoCogido);
        }
        // MEGÁFONO → mano derecha
        else if (objeto.CompareTag("Megafono"))
        {
            PlayerEquipmentManager.Instance?.PickupMegaphone(objeto);
            if (GameManager.instance != null) GameManager.instance.RecogerMegafono();
        }
        // LINTERNA → mano derecha
        else if (objeto.CompareTag("Linterna"))
        {
            PlayerEquipmentManager.Instance?.PickupFlashlight(objeto);
            if (GameManager.instance != null) GameManager.instance.tieneLinterna = true;
        }

        objeto = null;
    }

    // ─── Trayectoria ─────────────────────────────────────────────────────────

    void DibujarTrayectoria()
    {
        Vector3 startPoint = handPoint.transform.position;
        Vector3 direction = (cam.transform.forward + Vector3.up * 0.4f).normalized;
        Vector3 startVelocity = direction * throwForce;

        lineRenderer.positionCount = predictionSteps;
        for (int i = 0; i < predictionSteps; i++)
        {
            float t = i * timeStep;
            Vector3 point = startPoint + startVel * t + 0.5f * Physics.gravity * t * t;
            lineRenderer.SetPosition(i, point);
        }
    }
}