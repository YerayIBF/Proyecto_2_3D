using UnityEngine;

/// <summary>
/// Pila recogible del mundo. Al pulsar E cerca, se añade al inventario
/// del PlayerStateMachine.
///
/// Coloca este script en cada pila de la escena junto con un Collider.
/// </summary>
public class BatteryWorldPickup : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Cuántas baterías añade al inventario")]
    public int batteriesAmount = 1;
    [Tooltip("Distancia para poder recogerla")]
    public float pickupRange = 2f;

    [Header("Visual al recoger")]
    public GameObject pickupEffect;
    public AudioClip  pickupSound;

    [Header("Icono UI 'pulsa E' (opcional)")]
    public GameObject pickupIcon;

    [Header("Rotación decorativa")]
    public bool  rotateOnFloor   = true;
    public float rotationSpeed   = 90f;
    public bool  floatUpDown     = true;
    public float floatAmplitude  = 0.15f;
    public float floatSpeed      = 2f;

    private Transform _player;
    private Vector3 _startPos;
    private bool _playerInRange = false;

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;

        _startPos = transform.position;

        if (pickupIcon != null) pickupIcon.SetActive(false);
    }

    private void Update()
    {
        // Rotación y flotación decorativa
        if (rotateOnFloor)
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        if (floatUpDown)
        {
            float newY = _startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        // Detección del jugador
        if (_player == null) return;

        float dist = Vector3.Distance(transform.position, _player.position);
        bool nowInRange = dist <= pickupRange;

        if (nowInRange != _playerInRange)
        {
            _playerInRange = nowInRange;
            if (pickupIcon != null) pickupIcon.SetActive(_playerInRange);
        }

        // Pulsar E para recoger
        if (_playerInRange && Input.GetKeyDown(KeyCode.E))
            Recoger();
    }

    private void Recoger()
    {
        if (PlayerStateMachine.Instance == null)
        {
            Debug.LogError("[Battery] No hay PlayerStateMachine en escena.");
            return;
        }

        bool added = PlayerStateMachine.Instance.AddBattery(batteriesAmount);

        if (!added)
        {
            Debug.Log("[Battery] Inventario lleno.");
            return;
        }

        // Efectos
        if (pickupEffect != null)
            Instantiate(pickupEffect, transform.position, Quaternion.identity);
        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        Debug.Log($"[Battery] Recogida +{batteriesAmount}");
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}