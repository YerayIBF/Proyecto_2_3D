using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// Puerta que requiere una llave con ID específico.
/// Al pulsar E/X cerca:
/// - Si tienes la llave con el ID correcto → se abre
/// - Si no → mensaje "necesitas una llave"
/// </summary>
public class KeyDoor : MonoBehaviour
{
    [Header("Identificador")]
    [Tooltip("ID de la llave necesaria para abrir esta puerta. Debe coincidir con el de KeyPickup.")]
    public string requiredKeyID = "default";

    [Header("Pickup")]
    public float interactRange = 2f;
    public KeyCode interactKey = KeyCode.E;

    [Header("Animación de apertura")]
    public float openAngleY = 90f;
    public float openDuration = 1.5f;

    [Header("Comportamiento")]
    [Tooltip("Si está marcado, la llave se gasta al abrir esta puerta")]
    public bool consumirLlave = true;

    [Header("Final del juego")]
    [Tooltip("Si está marcado, carga la escena de créditos al abrirse")]
    public bool esPuertaFinal = false;
    public string sceneToLoadOnFinal = "Creditos";

    [Header("Audio opcional")]
    public AudioSource openSound;
    public AudioSource lockedSound;

    [Header("Icono flotante (opcional)")]
    public PickupIcon pickupIcon;

    [Header("Mensajes")]
    public string mensajeSinLlave = "Sembla que necesito una clau";

    [SerializeField] private bool _opened = false;
    private Transform _player;
    private Quaternion _closedRotation;
    private Quaternion _openRotation;

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;

        _closedRotation = transform.localRotation;
        _openRotation = _closedRotation * Quaternion.Euler(0f, openAngleY, 0f);
    }

    private void Update()
    {
        if (_opened || _player == null) return;

        float dist = Vector3.Distance(transform.position, _player.position);
        bool inRange = dist <= interactRange;

        if (pickupIcon != null)
            pickupIcon.MostrarIcono(inRange);

        if (inRange && BotonAbrirPulsado())
            TryOpen();
    }

    private bool BotonAbrirPulsado()
    {
        if (Input.GetKeyDown(interactKey))
            return true;

        if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame)
            return true;

        return false;
    }

    private void TryOpen()
    {
        bool puedeAbrir = GameManager.instance != null && GameManager.instance.HasKey(requiredKeyID);

        if (puedeAbrir)
        {
            Open();

            if (consumirLlave && GameManager.instance != null)
                GameManager.instance.UseKey(requiredKeyID);
        }
        else
        {
            Debug.Log($"[Door] La puerta '{requiredKeyID}' está cerrada. Necesitas la llave.");
            if (lockedSound != null) lockedSound.Play();

            if (GameManager.instance != null && !string.IsNullOrEmpty(mensajeSinLlave))
                GameManager.instance.ReproducirVoz(mensajeSinLlave, 2f);
        }
    }

    private void Open()
    {
        _opened = true;

        if (pickupIcon != null)
            pickupIcon.MostrarIcono(false);

        if (openSound != null) openSound.Play();

        StartCoroutine(AnimateOpen());

        Debug.Log($"[Door] Puerta '{requiredKeyID}' abierta.");
    }

    private IEnumerator AnimateOpen()
    {
        float t = 0f;
        while (t < openDuration)
        {
            t += Time.deltaTime;
            transform.localRotation = Quaternion.Slerp(_closedRotation, _openRotation, t / openDuration);
            yield return null;
        }
        transform.localRotation = _openRotation;

        if (esPuertaFinal)
        {
            SceneManager.LoadScene(sceneToLoadOnFinal);
        }
    }
}