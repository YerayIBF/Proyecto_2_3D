using UnityEngine;

public class ThrowObject : MonoBehaviour
{
    private bool lanzado = false;
    private float lanzadoTimer = 0f;
    public float delayDeteccion = 0.2f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (lanzado)
        {
            lanzadoTimer += Time.deltaTime;
        }
    }

    public void Lanzado()
    {
        lanzado = true;
        lanzadoTimer = 0f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!lanzado || lanzadoTimer < delayDeteccion) return;

        EmitirSonido.instance.EmitirRuido(transform.position, 15f);
        Debug.Log("Objeto aterrizó, sonido emitido");

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.linearDamping = 5f;
        rb.angularDamping = 5f;

        if (rb.linearVelocity.magnitude > 10f)
        {
            rb.linearVelocity *= 0.5f;
        }
        lanzado = false; 
    }
}
