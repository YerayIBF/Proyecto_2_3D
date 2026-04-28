using UnityEngine;

public class CiegoPatrol : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject ghost; 
    private UnityEngine.AI.NavMeshAgent agent;
    private UnityEngine.AI.NavMeshAgent ghostAgent;
    private GhostCiego ghostScript; 

    void Start()
    {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        enabled = false;
        if (ghost != null)
        {
            ghostAgent = ghost.GetComponent<UnityEngine.AI.NavMeshAgent>();
            ghostScript = ghost.GetComponent<GhostCiego>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (ghost != null)
        {
            float distanciaGhost = Vector3.Distance(transform.position, ghost.transform.position);
            if (ghostScript.estaEsperando)
            {
                agent.ResetPath();
                //agent.SetDestination(ghost.transform.position);
            }
            else{
                if (distanciaGhost > 0.5f)
                {
                    agent.SetDestination(ghost.transform.position);
                }
            }
        }
    }

    public void ActivarPatrullaje()
    {
        enabled = true;
    }

    public void DesactivarPatrullaje(){
        enabled = false;
        agent.speed = 1.5f;
        ghostAgent.speed = 1f;

        agent.GetComponent<Animator>().speed = 1f;
        agent.ResetPath();
        ghostScript.ReanudarPatrullaje();
    }

    public void ActivarPersecución(Vector3 destino)
    {
        enabled = true;
        agent.speed = 2.5f;
        ghostAgent.speed = 2f;

        agent.GetComponent<Animator>().speed = 1.5f;
        ghostScript.IrAlSonido(destino);
        agent.SetDestination(ghost.transform.position);
    }
}
