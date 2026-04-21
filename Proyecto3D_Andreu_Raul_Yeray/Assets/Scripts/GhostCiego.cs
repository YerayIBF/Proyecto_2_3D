using UnityEngine;

public class GhostCiego : MonoBehaviour
{
    public Transform[] patrolPoints;
    private UnityEngine.AI.NavMeshAgent agent;
    private int currentPoint;

    public float waitTime = 2f;
    private float waitTimer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        GetComponent<MeshRenderer>().enabled = false;
        if (patrolPoints.Length > 0)
        {
            currentPoint = 0;
            GoToNextPoint();
        }
    }

    // Update is called once per frame
    void Update()
    {

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTime)
            {
                GoToNextPoint();
                waitTimer = 0;
            }
        }
    }

    void GoToNextPoint()
    {
        if (patrolPoints.Length == 0)
            return;

        int nextPoint;
        do
        {
            nextPoint = Random.Range(0, patrolPoints.Length);
        } while (nextPoint == currentPoint && patrolPoints.Length > 1);

        currentPoint = nextPoint;
        agent.SetDestination(patrolPoints[currentPoint].position);
    }

    public void Detener(){
        agent.ResetPath();
    }
}
