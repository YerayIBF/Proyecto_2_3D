using UnityEngine;

public class Proyectil : MonoBehaviour
{
    public float damage = 10f;
    public float velocidad = 20f;
    public LayerMask capasColision; // selecciona Default, Player, etc en el Inspector

    private bool _yaGolpeo = false;
    private Vector3 _direccion;

    public void Init(Vector3 direccion)
    {
        _direccion = direccion.normalized;
    }

    void Update()
    {
        if (_yaGolpeo) return;

        float distanciaFrame = velocidad * Time.deltaTime;

        if (Physics.Raycast(transform.position, _direccion, out RaycastHit hit, distanciaFrame, capasColision))
        {
            _yaGolpeo = true;

            if (hit.collider.CompareTag("Player"))
                PlayerStateMachine.Instance.TakeDamage(damage);

            Destroy(gameObject);
            return;
        }

        transform.position += _direccion * distanciaFrame;
    }
}