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

    [Header("Rango de detección")]
    public float pickupRange = 2f;

    // Estado
    private Camera cam;
    private GameObject objetoCogido;
    private GameObject objeto;
    public bool apuntando = false;

    private Vector3 handPointRotation;
    private Vector3 handPointLeftRotation;

    // ─── Init ────────────────────────────────────────────────────────────────

    void Start()
    {
        cam = Camera.main;
        if (lineRenderer != null) lineRenderer.enabled = false;

        handPointRotation = handPoint.transform.localEulerAngles;
        if (handPointLeft != null)
            handPointLeftRotation = handPointLeft.transform.localEulerAngles;
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
            objeto = null;
            return;
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

        objeto = masCercano;
    }

    // ─── Apuntado megáfono ───────────────────────────────────────────────────

    private void HandleMegaphoneAim()
    {
        if (Input.GetMouseButton(1))
        {
            if (!apuntando)
            {
                apuntando = true;
                if (animator != null) animator.SetBool("isAiming", true);
                if (GameManager.instance != null) GameManager.instance.MostrarCanvasMegafono(true);
                PlayerEquipmentManager.Instance?.SetAimingMegaphone(true);
            }
        }
        else if (apuntando)
        {
            apuntando = false;
            if (animator != null) animator.SetBool("isAiming", false);
            if (GameManager.instance != null) GameManager.instance.MostrarCanvasMegafono(false);
            PlayerEquipmentManager.Instance?.SetAimingMegaphone(false);
        }
    }

    // ─── Apuntado lanzable + lanzar ──────────────────────────────────────────

    private void HandleAimAndThrow()
    {
        if (Input.GetMouseButton(1))
        {
            throwForce += 10f * Time.deltaTime;
            throwForce = Mathf.Clamp(throwForce, 5f, 30f);
            if (lineRenderer != null) lineRenderer.enabled = true;
            DibujarTrayectoria();
        }
        else
        {
            if (lineRenderer != null) lineRenderer.enabled = false;
            throwForce = 10f;
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
        rb.AddForce(direction * throwForce, ForceMode.VelocityChange);

        ThrowObject throwable = objetoCogido.GetComponent<ThrowObject>();
        if (throwable != null) throwable.Lanzado();

        objetoCogido.transform.SetParent(null);
        objetoCogido = null;

        if (handPointLeft != null)
            handPointLeft.transform.localEulerAngles = handPointLeftRotation;

        PlayerEquipmentManager.Instance?.ReleaseThrowable();
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
            GameObject[] items = GameObject.FindGameObjectsWithTag(tag);
            foreach (var item in items)
            {
                ThrowObject thr = item.GetComponent<ThrowObject>();
                if (thr != null)
                    thr.MostrarIcono(item == objeto);
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
        if (lineRenderer == null) return;

        Vector3 startPoint = handPointLeft != null
            ? handPointLeft.transform.position
            : handPoint.transform.position;

        Vector3 direction = (cam.transform.forward + Vector3.up * 0.4f).normalized;
        Vector3 startVel  = direction * throwForce;

        lineRenderer.positionCount = predictionSteps;
        for (int i = 0; i < predictionSteps; i++)
        {
            float t = i * timeStep;
            Vector3 point = startPoint + startVel * t + 0.5f * Physics.gravity * t * t;
            lineRenderer.SetPosition(i, point);
        }
    }
}