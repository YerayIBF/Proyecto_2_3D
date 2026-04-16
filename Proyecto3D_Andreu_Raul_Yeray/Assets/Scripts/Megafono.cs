using UnityEngine;

public class Megafono : MonoBehaviour
{
    public SphereCollider sonidoGolpe;
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
        if (Input.GetKeyDown(KeyCode.M))
        {
            ActivarMegafono();
        }
    }

    public void ActivarMegafono()
    {
        sonidoGolpe.enabled = true;
        Debug.Log("megafono activado");

        Invoke("DesactivarMegafono", 3);
    }

    public void DesactivarMegafono()
    {
        sonidoGolpe.enabled = false;
        Debug.Log("megafono desactivado");
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
