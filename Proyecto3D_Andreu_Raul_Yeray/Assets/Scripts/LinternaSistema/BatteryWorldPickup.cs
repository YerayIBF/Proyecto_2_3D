using UnityEngine;

/// <summary>
/// Batería recogible en el mundo.
/// Al tocarla se añade al inventario del PlayerStateMachine.
///
/// Requiere: Collider con Is Trigger = true
/// </summary>
public class BatteryWorldPickup : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Cuántas baterías añade al inventario")]
    public int batteriesAmount = 1;

    [Tooltip("Efecto visual al recoger (opcional)")]
    public GameObject pickupEffect;

    [Header("Rotación decorativa")]
    public float rotationSpeed = 90f;
    public bool  floatUpDown   = true;
    public float floatAmplitude = 0.15f;
    public float floatSpeed    = 2f;

    private Vector3 _startPos;

    private void Start()
    {
        _startPos = transform.position;
    }

    private void Update()
    {
        // Rotación y flotación decorativa
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        if (floatUpDown)
        {
            float newY = _startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerStateMachine psm = other.GetComponentInParent<PlayerStateMachine>()
                               ?? other.GetComponent<PlayerStateMachine>();

        if (psm == null) return;

        bool picked = psm.AddBattery(batteriesAmount);

        if (picked)
        {
            if (pickupEffect != null)
                Instantiate(pickupEffect, transform.position, Quaternion.identity);

            Debug.Log($"[Battery] Recogida +{batteriesAmount}");
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("[Battery] Inventario lleno.");
        }
    }
}
