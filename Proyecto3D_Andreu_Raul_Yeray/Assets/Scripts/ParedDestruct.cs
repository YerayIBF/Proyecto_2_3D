using UnityEngine;

public class ParedDestruct : MonoBehaviour
{
    public GameObject paredDestruidaPrefab;
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
                Instantiate(paredDestruidaPrefab, transform.position, transform.rotation);
            }
            
            Destroy(gameObject);
        }
    }
}
