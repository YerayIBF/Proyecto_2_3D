using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class CiegoBehaviour : MonoBehaviour
{

    public bool investigando = false;
    public bool patrullar = false;
    public bool sonidoDetectado = false;
    public bool perseguir = false;
    public bool aturdido = false;
    public bool atacando = false;

    public List<Vector3> sonidos = new List<Vector3>();

    public float areaEscucha = 15f;
    public float areaAtaque = 2f;

    private Animator animator;
    [HideInInspector]
    public NavMeshAgent agent;

    public float investigarTimer = 10f;
    public float aturdidoTimer = 5f;

    public Transform[] patrolPoints;
    private int puntoActual;
    public float waitTime = 2f;
    private float waitTimer;
    private float timerSonido;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (aturdido)
        {
            aturdidoTimer -= Time.deltaTime;
            if (aturdidoTimer <= 0f)
            {
                aturdido = false;
                agent.isStopped = false;
                Patrullar();
            }
            else
            {
                Aturdido();
            }
        }
        else
        {
            if (HaySonidos())
            {
                if (SonidoCercano())
                {
                    Atacar();
                }
                else
                {
                    if (!SonidoCercano() && investigando)
                    {
                        investigarTimer -= Time.deltaTime;
                        if (investigarTimer <= 0f)
                        {
                            investigando = false;
                            Patrullar();
                        }
                        else
                        {
                            PerseguirSonido();
                        }
                    }
                    else
                    {
                        Patrullar();
                    }
                }
            }
            else
            {
                if (investigando)
                {
                    investigarTimer -= Time.deltaTime;
                    if (investigarTimer <= 0f)
                    {
                        investigando = false;
                        Patrullar();
                    }
                    else
                    {
                        PerseguirSonido();
                    }
                }
                else
                {
                    Patrullar();
                }
            }
        }

    }

    private bool HaySonidos()
    {
        return sonidos.Count > 0;
    }

    public void DetectarSonido(Vector3 posicionSonido)
    {
        if (sonidos.Count >= 3)
        {
            sonidos.RemoveAt(0);
        }

        sonidos.Add(posicionSonido);
        sonidoDetectado = true;
        investigando = true;
        investigarTimer = 10f;
    }

    public void PerseguirSonido()
    {
        patrullar = false;

        if (sonidos.Count == 0)
        {
            investigando = false;
            return;
        }

        Vector3 sonidoCercano = sonidos[0];
        float minDistancia = float.MaxValue;

        foreach (Vector3 sonido in sonidos)
        {
            float distancia = Vector3.Distance(transform.position, sonido);
            if (distancia <= areaEscucha)
            {
                minDistancia = distancia;
                sonidoCercano = sonido;
            }
        }

        agent.SetDestination(sonidoCercano);


        if (minDistancia <= areaAtaque)
        {
            Atacar();
        }
    }

    private  bool SonidoCercano()
    {
        //Si esta en area de ataque, atacar en area hacia el sonido
        foreach (Vector3 sonido in sonidos)
        {
            if (Vector3.Distance(transform.position, sonido) <= areaAtaque)
            {
                return true;
            }
        }
        return false;
    }

    private bool Investigar()
    {
        return false;
    }

    public void Atacar()
    {
        atacando = true;
        //animator.SetTrigger("atacar");
        Collider[] rango = Physics.OverlapSphere(transform.position, areaAtaque);
        foreach (Collider col in rango){
            if (col.CompareTag("Player"))
            {
                //Atacar al jugador
                Debug.Log("Atacando al jugador");
            }
        }

        //Eliminar sonido mas cercano para evitar entrar en bucle
        if (sonidos.Count > 0)
        {
            Vector3 sonidoCercano = sonidos[0];
            float minDistancia = float.MaxValue;


            foreach (Vector3 sonido in sonidos)
            {
                float distancia = Vector3.Distance(transform.position, sonido);
                if (distancia < minDistancia)
                {
                    minDistancia = distancia;
                    sonidoCercano = sonido;
                }
            }

            timerSonido += Time.deltaTime;
            if (timerSonido >= 2f)
            {
                sonidos.Remove(sonidoCercano);
                sonidoDetectado = sonidos.Count > 0;

                timerSonido = 0;
            }
        }
    }  

    public void Patrullar()
    {
        atacando = false;
        patrullar = true;
        if (agent.isStopped)
        {
            return;
        }
        
        //animator.SetInteger("state", 1);
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTime)
            {
                if (patrolPoints.Length == 0)
                return;

                int nextPoint;
                do
                {
                    nextPoint = Random.Range(0, patrolPoints.Length);
                } while (nextPoint == puntoActual && patrolPoints.Length > 1);

                puntoActual = nextPoint;
                agent.SetDestination(patrolPoints[puntoActual].position);

                waitTimer = 0;
            }
        }
    }

    public void Aturdido()
    {
        //animator.SetInteger("state", 3);
        patrullar = false;
        sonidos.Clear();
        Debug.Log("Aturdido");
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, areaAtaque);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, areaEscucha);
    }
}
