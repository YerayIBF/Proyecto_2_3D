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

    public List<Vector3> sonidos = new List<Vector3>();

    public float areaVision = 10f;
    public float areaAtaque = 2f;

    private Animator animator;
    private NavMeshAgent agent;

    public float investigarTimer = 10f;
    public float aturdidoTimer = 5f;
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
                Patrullar();
            }
            else
            {
                aturdido = true;
                //animator.SetInteger("state", 3);
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
        sonidos.Add(posicionSonido);
        sonidoDetectado = true;
    }

    public void PerseguirSonido()
    {
        investigando = true;
        /*Vector3 sonidoCercano = sonidos.Find(sonido => Vector3.Distance(transform.position, sonido) <= areaVision);
        if (sonidoCercano != Vector3.zero)        {
            agent.SetDestination(sonidoCercano);
            investigando = true;
            //animator.SetInteger("state", 2);
        }*/
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
        agent.SetDestination(sonidoCercano);

        //Si ha llegado al destino, eliminar el sonido de la lista
            if (Vector3.Distance(transform.position, sonidoCercano) < 1f)
            {
                sonidos.Remove(sonidoCercano);
                sonidoDetectado = false;
                perseguir = false;
                investigando = false;
                investigarTimer = 10f;
                //animator.SetInteger("state", 0);
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
        //animator.SetTrigger("atacar");
        Debug.Log("Atacando al jugador");
        Collider[] rango = Physics.OverlapSphere(transform.position, areaAtaque);
        foreach (Collider col in rango){
            if (col.CompareTag("Player"))
            {
                //Atacar al jugador
                Debug.Log("Atacando al jugador");
            }
        }
    }  

    public void Patrullar()
    {
        //animator.SetInteger("state", 1);

    }

    public void Perseguir()
    {
        
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, areaAtaque);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, areaVision);
    }
}
