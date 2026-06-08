using UnityEngine;

/// <summary>
/// Pila para la linterna. Al pulsar E cerca, suma 1 al inventario.
/// </summary>
public class PilaLinterna : MonoBehaviour
{
    [Header("Pickup")]
    public float pickupRange = 2f;
    public KeyCode pickupKey = KeyCode.E;

    [Header("Icono flotante (opcional)")]
    public PickupIcon pickupIcon;

    [SerializeField] private bool _picked = false;
    private Transform _player;

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;
    }

    private void Update()
    {
        if (_picked || _player == null) return;

        float dist = Vector3.Distance(transform.position, _player.position);
        bool inRange = dist <= pickupRange;

        if (pickupIcon != null)
            pickupIcon.MostrarIcono(inRange);

        if (inRange && Input.GetKeyDown(pickupKey))
            Pickup();
    }

    private void Pickup()
    {
        // Intentar añadir al inventario
        if (PlayerStateMachine.Instance == null)
        {
            Debug.LogWarning("[PilaLinterna] PlayerStateMachine no encontrado.");
            return;
        }

        bool added = PlayerStateMachine.Instance.AddBattery(1);
        if (!added)
        {
            Debug.Log("[PilaLinterna] Inventario lleno.");
            // Opcional: mostrar mensaje al jugador
            if (GameManager.instance != null)
                GameManager.instance.ReproducirVoz("El inventari esta ple de piles", 2f);
            return;
        }

        _picked = true;

        if (pickupIcon != null)
            pickupIcon.MostrarIcono(false);

        Debug.Log("[PilaLinterna] Pila recogida.");

        // Hacer desaparecer la pila
        gameObject.SetActive(false);
    }
}
