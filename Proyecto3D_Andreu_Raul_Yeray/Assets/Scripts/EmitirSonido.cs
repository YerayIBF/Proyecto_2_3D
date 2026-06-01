using UnityEngine;

public class EmitirSonido : MonoBehaviour
{
    public static EmitirSonido instance;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void EmitirRuido(Vector3 posicion, float intensidad)
    {
        Collider[] colliders = Physics.OverlapSphere(posicion, intensidad);

        foreach (Collider collider in colliders)
        {
            CiegoBehaviour ciego = collider.GetComponent<CiegoBehaviour>();
            if (ciego != null)
            {
                ciego.DetectarSonido(posicion);
            }
            EnemyBehaviourTree enemigo = collider.GetComponent<EnemyBehaviourTree>();
    if (enemigo != null)
        enemigo.OnHeardNoise(posicion);
        
        }
    }
}
