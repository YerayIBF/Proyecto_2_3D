using UnityEngine;

public class Papel : MonoBehaviour
{
    public string texto;
    public GameObject canvasLeer;
    private bool jugadorCerca = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (jugadorCerca && Input.GetKeyDown(KeyCode.E))
        {
            LeerPapel();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = true;
            if (canvasLeer != null)
            {
                canvasLeer.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = false;
            if (canvasLeer != null)
            {
                canvasLeer.SetActive(false);
            }
        }
    }

    void LeerPapel()
    {
        //GameManager.instance.MostrarPapel(texto);
        if (canvasLeer != null)
        {
            canvasLeer.SetActive(false);
        }
    }
}
