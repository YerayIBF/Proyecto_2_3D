using UnityEngine;

public class PilaMegafono : MonoBehaviour
{
    public float cantidadRecarga = 50f;
    public Megafono megafono;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Recargar()
    {
        if (megafono != null)
        {
            megafono.RecargarEnergia(cantidadRecarga);
        }
    }

    /*private void OnTriggerEnter(Collider other)
    {
        Megafono megafono = other.GetComponentInChildren<Megafono>();

        if (megafono != null)
        {
            megafono.RecargarBateria(cantidadRecarga);
            Destroy(gameObject); 
        }
    }*/
}
