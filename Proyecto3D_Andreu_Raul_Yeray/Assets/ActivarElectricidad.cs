using UnityEngine;

public class ActivarElectricidad : MonoBehaviour
{
    public BoxCollider electricidadCollider;
    public GameObject canvasElectricidad;
    public Animator puertaAnim;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        canvasElectricidad.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            canvasElectricidad.SetActive(true);
            GameManager.instance.zonaActivacion = true;
            //puertaAnim.SetTrigger("Abrir");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            canvasElectricidad.SetActive(false);
            GameManager.instance.zonaActivacion = false;
        }
    }
}
