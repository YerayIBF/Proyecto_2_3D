using UnityEngine;
using UnityEngine.InputSystem;

public class Megafono : MonoBehaviour
{
    public SphereCollider sonidoGolpe;
    public float energiaActual = 100f;
    public float maxEnergia = 100f;
    public float coste = 25f;
    private GameObject enemigoCiego;
    public CogerObjeto cogerObjetoScript;

    [Header("Input Actions (mando + teclado)")]
    [Tooltip("Action para aturdir/activar megáfono (LB)")]
    public InputActionReference attackAction;
    [Tooltip("Action para recargar (RT)")]
    public InputActionReference reloadAction;

    private void OnEnable()
    {
        if (attackAction != null) attackAction.action.Enable();
        if (reloadAction != null) reloadAction.action.Enable();
    }

    private void OnDisable()
    {
        if (attackAction != null) attackAction.action.Disable();
        if (reloadAction != null) reloadAction.action.Disable();
    }

    void Start()
    {
        sonidoGolpe.enabled = false;
        enemigoCiego = GameObject.FindGameObjectWithTag("EnemyBlind");
    }

    void Update()
    {
        // ── ACTIVAR MEGÁFONO (M / LB mando) ──
        bool attackInput = Input.GetKeyDown(KeyCode.M)
                        || (attackAction != null && attackAction.action.WasPressedThisFrame());

        if (attackInput && energiaActual >= coste && GameManager.instance.tieneMegafono && cogerObjetoScript.apuntando)
        {
            ActivarMegafono();
        }

        // ── RECARGAR MEGÁFONO (R / RT mando) ──
        bool reloadInput = Input.GetKeyDown(KeyCode.R)
                        || (reloadAction != null && reloadAction.action.WasPressedThisFrame());

        if (reloadInput && PlayerEquipmentManager.Instance != null
            && PlayerEquipmentManager.Instance.IsMegaphoneInHand)
        {
            TriggerReloadAnimation();
        }
    }

    public void ActivarMegafono()
    {
        energiaActual -= coste;
        GameManager.instance.ActualizarEnergia(energiaActual, maxEnergia);

        sonidoGolpe.enabled = true;
        Debug.Log("megafono activado");

        Invoke("DesactivarMegafono", 3);
    }

    public void DesactivarMegafono()
    {
        sonidoGolpe.enabled = false;
        Debug.Log("megafono desactivado");
    }

    /// <summary>
    /// Recarga completa - llamado desde el Animation Event vía PlayerStateMachine.
    /// </summary>
    public void RecargarEnergia(float cantidad)
    {
        energiaActual = maxEnergia;
        energiaActual = Mathf.Clamp(energiaActual, 0, maxEnergia);
        GameManager.instance.ActualizarEnergia(energiaActual, maxEnergia);
        Debug.Log("[Megafono] Recargado.");
    }

    /// <summary>
    /// Dispara la animación de recarga.
    /// </summary>
    public void TriggerReloadAnimation()
    {
        if (PlayerStateMachine.Instance == null
            || PlayerStateMachine.Instance.MegafonoBatteryCount <= 0)
        {
            Debug.Log("[Megafono] Sin pilas, no se puede recargar.");
            return;
        }

        if (energiaActual >= maxEnergia)
        {
            Debug.Log("[Megafono] Energía al máximo.");
            return;
        }

        if (PlayerStateMachine.Instance.playerAnimator == null)
        {
            PlayerStateMachine.Instance.TryReloadMegafono();
            return;
        }

        Animator anim = PlayerStateMachine.Instance.playerAnimator;
        bool isCrouched = PlayerStateMachine.Instance.IsCrouched;

        if (isCrouched)
        {
            anim.SetTrigger("ReloadCrouched");
            PlayerStateMachine.Instance.EnterTemporaryState(
                PlayerStateMachine.PlayerState.ReloadingCrouched, 1.5f);
        }
        else
        {
            anim.SetTrigger("Reload");
            PlayerStateMachine.Instance.EnterTemporaryState(
                PlayerStateMachine.PlayerState.Reloading, 1.5f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("EnemyBlind"))
        {
            CiegoBehaviour enemigo = other.GetComponent<CiegoBehaviour>();
            enemigo.aturdido = true;
            enemigo.stunActivado = false;
            enemigo.aturdidoTimer = 5f;
            enemigo.agent.isStopped = true;

            Debug.Log("He tocado al ciego con el megafono");
        }
    }
}