using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// CogerObjeto — versión limpia.
/// - Mantiene la lógica de dos manos, PlayerEquipmentManager, PickupIcon, bloqueos.
/// - Soporta InputActions para mando: Apuntar, Lanzar, Soltar, Interact.
/// - También funcionan las teclas del teclado como fallback.
///
/// IMPORTANTE: La animación de "Coger" la dispara SÓLO el ThirdPersonController.
/// Este script solo detecta objetos cercanos, muestra iconos y ejecuta
/// AnimatorCogerObjeto() en el Animation Event.
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

    [Header("Input Actions (opcional, para mando)")]
    public InputActionReference apuntarAction;
    public InputActionReference lanzarAction;
    public InputActionReference soltarAction;
    public InputActionReference interactAction;  // recoger

    private Camera cam;
    private GameObject objetoCogido;
    [HideInInspector] public GameObject objeto;
    public bool apuntando = false;

    private Vector3 handPointRotation;
    private Vector3 handPointLeftRotation;

    private float _savedThrowForce = 10f;

    // Tags de objetos detectables (centralizado para facilidad)
    private static readonly string[] _detectableTags = {
        "objetoCogible", "Megafono", "Linterna", "Llave", "LlaveFinal", "PilaMegafonoNueva", "PilaLinterna"
    };

    private void OnEnable()
    {
        if (apuntarAction != null)  apuntarAction.action.Enable();
        if (lanzarAction != null)   lanzarAction.action.Enable();
        if (soltarAction != null)   soltarAction.action.Enable();
        if (interactAction != null) interactAction.action.Enable();
    }

    private void OnDisable()
    {
        if (apuntarAction != null)  apuntarAction.action.Disable();
        if (lanzarAction != null)   lanzarAction.action.Disable();
        if (soltarAction != null)   soltarAction.action.Disable();
        if (interactAction != null) interactAction.action.Disable();
    }

    void Start()
    {
        cam = Camera.main;
        if (lineRenderer != null) lineRenderer.enabled = false;

        handPointRotation = handPoint.transform.localEulerAngles;
        if (handPointLeft != null)
            handPointLeftRotation = handPointLeft.transform.localEulerAngles;
    }

    void Update()
    {
        if (PlayerStateMachine.Instance != null && !PlayerStateMachine.Instance.IsAlive)
            return;

        bool inputApuntar    = (Input.GetMouseButton(1))
                            || (apuntarAction != null && apuntarAction.action.IsPressed());

        bool inputLanzarDown = Input.GetMouseButtonDown(0)
                            || (lanzarAction != null && lanzarAction.action.WasPressedThisFrame());

        bool inputSoltarDown = Input.GetKeyDown(KeyCode.R)
                            || (soltarAction != null && soltarAction.action.WasPressedThisFrame());

        DetectarObjetoCercano();
        ActualizarIconos();

        if (objetoCogido != null && inputSoltarDown)
        {
            if (objetoCogido.CompareTag("objetoCogible"))
                SoltarLanzable();
        }

        if (objetoCogido != null && objetoCogido.CompareTag("objetoCogible"))
        {
            HandleAimAndThrow(inputApuntar, inputLanzarDown);

            if (apuntando)
            {
                apuntando = false;
                if (animator != null) animator.SetBool("isAiming", false);
                if (GameManager.instance != null) GameManager.instance.MostrarCanvasMegafono(false);
                PlayerEquipmentManager.Instance?.SetAimingMegaphone(false);
            }
        }
        else if (PlayerEquipmentManager.Instance != null
                && PlayerEquipmentManager.Instance.IsMegaphoneInHand)
        {
            HandleMegaphoneAim(inputApuntar);
        }
        else if (apuntando)
        {
            apuntando = false;
            if (animator != null) animator.SetBool("isAiming", false);
            if (GameManager.instance != null) GameManager.instance.MostrarCanvasMegafono(false);
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

        foreach (string tag in _detectableTags)
        {
            GameObject[] candidatos = FindGameObjectsWithTagSafe(tag);
            if (candidatos == null) continue;

            foreach (var go in candidatos)
            {
                if (go == null) continue;
                if (EstaRecogido(go)) continue;

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

    /// <summary>
    /// Wrapper de FindGameObjectsWithTag que no tira excepción si el tag no existe.
    /// </summary>
    private GameObject[] FindGameObjectsWithTagSafe(string tag)
    {
        try
        {
            return GameObject.FindGameObjectsWithTag(tag);
        }
        catch (UnityException)
        {
            return null;
        }
    }

    private bool EstaRecogido(GameObject item)
    {
        if (PlayerEquipmentManager.Instance == null) return false;

        if (item.CompareTag("Linterna")
            && PlayerEquipmentManager.Instance.HasFlashlight
            && PlayerEquipmentManager.Instance.linternaObject == item)
            return true;

        if (item.CompareTag("Megafono")
            && PlayerEquipmentManager.Instance.HasMegaphone
            && PlayerEquipmentManager.Instance.megafonoObject == item)
            return true;

        if (item.CompareTag("objetoCogible"))
        {
            if (item.transform.IsChildOf(this.transform))
                return true;
        }

        // Las llaves NO se bloquean por la booleana genérica tieneLlave.
        // Cada llave se desactiva al cogerla (SetActive false), así que deja
        // de aparecer en la detección por sí sola. Cada llave es independiente.

        return false;
    }

    // ─── Apuntar megáfono ────────────────────────────────────────────────────

    private void HandleMegaphoneAim(bool inputApuntar)
    {
        if (inputApuntar)
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

    // ─── Apuntar y lanzar lanzable ───────────────────────────────────────────

    private void HandleAimAndThrow(bool inputApuntar, bool inputLanzarDown)
    {
        if (inputApuntar)
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

        if (inputLanzarDown)
        {
            _savedThrowForce = throwForce;
            TriggerThrowAnimation();
        }
    }

    private void TriggerThrowAnimation()
    {
        if (animator == null)
        {
            LanzarObjeto();
            return;
        }

        bool isCrouched = PlayerStateMachine.Instance != null
                       && PlayerStateMachine.Instance.IsCrouched;

        if (isCrouched)
        {
            animator.SetTrigger("ThrowCrouched");
            PlayerStateMachine.Instance?.EnterTemporaryState(
                PlayerStateMachine.PlayerState.ThrowingCrouched, 1f);
        }
        else
        {
            animator.SetTrigger("Throw");
            PlayerStateMachine.Instance?.EnterTemporaryState(
                PlayerStateMachine.PlayerState.Throwing, 1f);
        }
    }

    public void LanzarObjeto()
    {
        if (objetoCogido == null) return;

        if (lineRenderer != null) lineRenderer.enabled = false;

        Rigidbody rb = objetoCogido.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;

        Vector3 direction = (cam.transform.forward + Vector3.up * 0.4f).normalized;
        rb.AddForce(direction * _savedThrowForce, ForceMode.VelocityChange);

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
        foreach (string tag in _detectableTags)
        {
            GameObject[] items = FindGameObjectsWithTagSafe(tag);
            if (items == null) continue;

            foreach (var item in items)
            {
                bool mostrar = (item == objeto);

                ThrowObject thr = item.GetComponent<ThrowObject>();
                if (thr != null)
                {
                    thr.MostrarIcono(mostrar);
                    continue;
                }

                PickupIcon pickup = item.GetComponent<PickupIcon>();
                if (pickup != null)
                {
                    pickup.MostrarIcono(mostrar);
                }
            }
        }
    }

    // ─── Animation Event ─────────────────────────────────────────────────────

    public void AnimatorCogerObjeto()
    {
        if (objeto == null) return;

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
        else if (objeto.CompareTag("Megafono"))
        {
            PlayerEquipmentManager.Instance?.PickupMegaphone(objeto);
            if (GameManager.instance != null) GameManager.instance.RecogerMegafono();
        }
        else if (objeto.CompareTag("Linterna"))
        {
            PlayerEquipmentManager.Instance?.PickupFlashlight(objeto);
            if (GameManager.instance != null) GameManager.instance.tieneLinterna = true;
        }
        else if (objeto.CompareTag("Llave"))
        {
            // Usa el keyID de la propia llave (sistema con ID), no un ID fijo
            KeyPickup keyPickup = objeto.GetComponent<KeyPickup>();
            if (keyPickup != null)
                keyPickup.OnPickedUp();
            else if (GameManager.instance != null)
                GameManager.instance.RecogerLlave(); // fallback si no tiene KeyPickup

            objeto.SetActive(false);
        }
        else if (objeto.CompareTag("LlaveFinal"))
        {
            GameManager.instance.RecogerLlaveFinal();

            KeyEnd keyEnd = objeto.GetComponent<KeyEnd>();
            if (keyEnd != null)
            {
                keyEnd.MostrarPuerta(5f);
            }

            objeto.SetActive(false);
        }
        else if (objeto.CompareTag("PilaMegafonoNueva"))
        {
            PilaMegafonoNueva pila = objeto.GetComponent<PilaMegafonoNueva>();
            if (pila != null)
            {
                pila.Recoger();
            }
        }
        else if (objeto.CompareTag("PilaLinterna"))
        {
            PilaLinterna pila = objeto.GetComponent<PilaLinterna>();
            if (pila != null)
            {
                pila.Recoger();
            }
        }

        objeto = null;
    }

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