using UnityEngine;

public class CiegoPatrol : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject ghost; 
    private UnityEngine.AI.NavMeshAgent agent;
    void Start()
    {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (ghost != null)
        {
            agent.SetDestination(ghost.transform.position);
        }
    }

    public void ActivarPatrullaje()
    {
        enabled = true;
    }

    public void DesactivarPatrullaje(){
        enabled = false;
        agent.ResetPath();
    }
}
