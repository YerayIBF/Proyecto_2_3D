using UnityEngine;

public class ParedDestruct : MonoBehaviour
{
    public GameObject paredDestruidaPrefab;
    public GameObject cristalesRotos;
    public Transform puntoSpawnSuelo;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        ThrowObject throwObject = collision.gameObject.GetComponent<ThrowObject>();

        if (throwObject != null)
        {
            if (paredDestruidaPrefab != null)
            {
                GameObject panelRoto = Instantiate(paredDestruidaPrefab, transform.position, transform.rotation);

                panelRoto.transform.localScale = transform.localScale;

                MeshCollider col = panelRoto.GetComponent<MeshCollider>();
                if (col != null)
                {
                    col.enabled = false;
                }
            }

            if (cristalesRotos != null)
            {
                Instantiate(cristalesRotos, puntoSpawnSuelo.position, puntoSpawnSuelo.rotation);
            }

            //AudioManager.Instance.PlaySFX("RomperCristal");
            
            Destroy(gameObject);
        }
    }
}
