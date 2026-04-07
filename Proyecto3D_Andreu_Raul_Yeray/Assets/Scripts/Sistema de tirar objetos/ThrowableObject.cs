using UnityEngine;

/// <summary>
/// Coloca en cada objeto lanzable. Requiere Rigidbody + Collider.
/// Al impactar emite ruido proporcional a la velocidad de impacto.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ThrowableObject : MonoBehaviour
{
    [Header("Ruido al impactar")]
    public float noiseRadius      = 8f;
    public float minImpactForce   = 1.5f;

    [HideInInspector] public bool isHeld = false;

    private Rigidbody _rb;
    private Collider  _col;

    private void Awake()
    {
        _rb  = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();
    }

    public void OnPickup()
    {
        isHeld          = true;
        _rb.isKinematic = true;
        _col.enabled    = false;
    }

    public void OnThrow(Vector3 force)
    {
        isHeld          = false;
        _col.enabled    = true;
        _rb.isKinematic = false;
        _rb.AddForce(force, ForceMode.Impulse);
    }

    public void OnDrop()
    {
        isHeld          = false;
        _col.enabled    = true;
        _rb.isKinematic = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isHeld) return;

        float impact = collision.relativeVelocity.magnitude;
        if (impact < minImpactForce) return;

        // Intensidad proporcional a la velocidad (máximo a 15 m/s)
        float intensity = Mathf.Clamp01(impact / 15f);
        NoiseEmitter.Emit(transform.position, noiseRadius, intensity);
    }
}
