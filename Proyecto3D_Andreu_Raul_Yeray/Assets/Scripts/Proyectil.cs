using UnityEngine;

public class Proyectil : MonoBehaviour
{
    public int damage;
    private bool hit;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        
        if (!hit) {
            if(other.CompareTag("Player"))
            {
                PlayerStateMachine.Instance.TakeDamage(damage);
                Destroy(gameObject);
                hit = true;
            }
        }
        
    }
}
