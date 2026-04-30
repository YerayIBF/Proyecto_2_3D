using UnityEngine;

public class Megafono : MonoBehaviour
{
    public SphereCollider sonidoGolpe;
    public float energiaActual = 100f;
    public float maxEnergia = 100f;
    public float coste = 25f;
    private GameObject enemigoCiego;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sonidoGolpe.enabled = false;
        enemigoCiego = GameObject.FindGameObjectWithTag("EnemyBlind");
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M) && energiaActual >= coste)
        {
            ActivarMegafono();
        }
    }

    public void ActivarMegafono()
    {
        energiaActual -= coste;

        sonidoGolpe.enabled = true;
        Debug.Log("megafono activado");

        Invoke("DesactivarMegafono", 3);
    }

    public void DesactivarMegafono()
    {
        sonidoGolpe.enabled = false;
        Debug.Log("megafono desactivado");
    }


    //Funcion que se llamara desde el script de la pila
    public void RecargarEnergia(float cantidad)
    {
        energiaActual += cantidad;

        energiaActual = Mathf.Clamp(energiaActual, 0, maxEnergia);
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
