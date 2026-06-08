using UnityEngine;
using System.Collections;

/// <summary>
/// Puerta que requiere la llave para abrirse.
/// Al pulsar E cerca:
/// - Si tienes la llave (según GameManager) → se abre
/// - Si no → mensaje "necesitas una llave"
/// </summary>
public class KeyDoor : MonoBehaviour
{
    [Header("Pickup")]
    public float interactRange = 2f;
    public KeyCode interactKey = KeyCode.E;

    [Header("Animación de apertura")]
    [Tooltip("Rotación local en Y al abrir la puerta")]
    public float openAngleY = 90f;
    public float openDuration = 1.5f;

    [Header("Comportamiento")]
    [Tooltip("Si está marcado, la llave se gasta al abrir esta puerta")]
    public bool consumirLlave = true;

    [Header("Audio opcional")]
    public AudioSource openSound;
    public AudioSource lockedSound;

    [Header("Icono flotante (opcional)")]
    public PickupIcon pickupIcon;

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

        if (inRange && Input.GetKeyDown(interactKey))
            TryOpen();
    }

    private void TryOpen()
    {
        bool hasKey = GameManager.instance != null && GameManager.instance.tieneLlave;

        if (hasKey)
        {
            Open();

            if (consumirLlave && GameManager.instance != null)
                GameManager.instance.UsarLlave();
        }
        else
        {
            Debug.Log("[Door] La puerta está cerrada. Necesitas una llave.");
            if (lockedSound != null) lockedSound.Play();

            // Mostrar mensaje en el HUD
            if (GameManager.instance != null)
                GameManager.instance.ReproducirVoz("Sembla que necesito una clau", 2f);
        }
    }

    private void Open()
    {
        _opened = true;

        if (pickupIcon != null)
            pickupIcon.MostrarIcono(false);

        if (openSound != null) openSound.Play();

        StartCoroutine(AnimateOpen());

        Debug.Log("[Door] Puerta abierta.");
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
    }
}