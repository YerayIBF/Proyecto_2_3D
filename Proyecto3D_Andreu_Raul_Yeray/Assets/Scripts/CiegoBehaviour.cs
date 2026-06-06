using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;

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

    public Animator animator;
    [HideInInspector]
    public NavMeshAgent agent;

    public float investigarTimer = 10f;
    public float aturdidoTimer = 5f;

    public Transform[] patrolPoints;
    private int puntoActual;
    public float waitTime = 2f;
    private float waitTimer;
    private float timerSonido;
    private bool ataqueActivado = false;
    [HideInInspector]
    public bool stunActivado = false;

    private float ataqueTimer = 0f;
    private float duracionAtaque = 3f;
    private float dañoTimer = 0f;
    private CiegoPatrol patrullajeScript;

    public ParticleSystem stunEffect;
    public ParticleSystem attackEffect;
    public GameObject canvasAlerta;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        //animator = GetComponent<Animator>();

        animator.SetInteger("state", 0);
        patrullajeScript = GetComponent<CiegoPatrol>();
        stunEffect.Stop();
        attackEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        canvasAlerta.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {

        if (atacando)
        {
            DañoAtaque();
            return;
        }

        if (aturdido)
        {
            aturdidoTimer -= Time.deltaTime;
            if (aturdidoTimer <= 0f)
            {
                aturdido = false;
                stunActivado = false;
                perseguir = false;
                sonidoDetectado = false;

                agent.isStopped = false;
                animator.SetBool("isStun", false);

                stunEffect.Stop();
                Patrullar();
            }
            else
            {
                DetenerPatrullaje();
                Aturdido();
            }
        }
        else
        {
            if (HaySonidos())
            {
                if (SonidoCercano())
                {
                    DetenerPatrullaje();
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
                            DetenerPatrullaje();
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
                        DetenerPatrullaje();
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
        /*if (sonidos.Count >= 3)
        {
            sonidos.RemoveAt(0);
        }*/

        if (!investigando)
        {
            AudioManager.Instance.PlaySFX("CiegoAlerta");
            StartCoroutine(MostrarExclamacion());
        }

        sonidos.Clear();

        sonidos.Add(posicionSonido);
        sonidoDetectado = true;
        investigando = true;
        investigarTimer = 10f;
    }

    IEnumerator MostrarExclamacion()
    {
        canvasAlerta.SetActive(true);

        yield return new WaitForSeconds(1.5f);

        canvasAlerta.SetActive(false);
    }

    public void PerseguirSonido()
    {
        patrullar = false;
        perseguir = true;

        if (sonidos.Count == 0)
        {
            investigando = false;
            perseguir = false;
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

        //agent.SetDestination(sonidoCercano);
        patrullajeScript.ActivarPersecución(sonidoCercano);
        animator.SetInteger("state", 2);
        if (agent.velocity.magnitude > 0.1f)
        {
            Vector3 direccionMovimiento = agent.velocity.normalized;
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccionMovimiento);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, Time.deltaTime * 5f);
        }


        if (minDistancia <= areaAtaque)
        {
            perseguir = false;
            patrullajeScript.DesactivarPatrullaje();
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


    //Funcion que activa el daño de ataque hacia el jugador mientras atacando sea true    
    public void DañoAtaque()
    {
        dañoTimer += Time.deltaTime;

        if (dañoTimer >= 0.5f)
        {
            dañoTimer = 0f;

            Collider[] rango = Physics.OverlapSphere(transform.position, areaAtaque);
            foreach (Collider col in rango){
                if (col.CompareTag("Player"))
                {
                    //Atacar al jugador, funcion script jugador take damage
                    Debug.Log("Estoy recibiendo daño");
                    //instanciar player states machine para quitar daño al jugador
                    PlayerStateMachine.Instance.TakeDamage(10f);
                }
            }
        }
    }

    //Particulas efecto ataque en area 
    public void ActivarParticulasAtaque()
    {
        if (attackEffect != null)
        {
            attackEffect.Play(true);
        }
    }

    //Funcion llamada al finalizar la animacion de ataque para poder tener control sobre tiempo de ataque y la vuelta a patrullar
    public void FinAtaque()
    {
        agent.isStopped = true;
        animator.SetInteger("state", 0);

        StartCoroutine(EsperarAtaque());
    }

    IEnumerator EsperarAtaque()
    {
        yield return new WaitForSeconds(duracionAtaque);

        atacando = false;
        ataqueActivado = false;
        perseguir = false;
        agent.isStopped = false;

        Patrullar();
    }

    public void Atacar()
    {
        atacando = true;
        if (!ataqueActivado)
        {
            animator.SetTrigger("atacar");
            ataqueActivado = true;
            AudioManager.Instance.PlaySFX("AtaqueCiego");
        }

        /*Collider[] rango = Physics.OverlapSphere(transform.position, areaAtaque);
        foreach (Collider col in rango){
            if (col.CompareTag("Player"))
            {
                //Atacar al jugador
                Debug.Log("Estoy recibiendo daño");
            }
        }*/

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

            //timerSonido += Time.deltaTime;
            //if (timerSonido >= 2f)
            //{
                sonidos.Remove(sonidoCercano);
                sonidoDetectado = sonidos.Count > 0;
                ataqueActivado = false;
                //timerSonido = 0;
            //}
        }
    }  

    public void Patrullar()
    {
        if (atacando && ataqueActivado)
        {
            return;
        }

        if (perseguir)
        {
            return;
        }

        patrullar = true;

        patrullajeScript.ActivarPatrullaje();
        float speed = agent.velocity.magnitude;
        bool isMoving = speed > 0.1f && agent.remainingDistance > 0.5f;
        if (isMoving)
        {
            animator.SetInteger("state", 1);
        }
        else
        {
            animator.SetInteger("state", 0);
        }

        if (agent.velocity.magnitude > 0.1f)
        {
            Vector3 direccionMovimiento = agent.velocity.normalized;
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccionMovimiento);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, Time.deltaTime * 5f);
        }
    }

    public void DetenerPatrullaje()
    {
        patrullajeScript.DesactivarPatrullaje();
    }

    //Sonido de pasos que se llamara desde el animation event de la animacion walk del enemigo
    public void SonidoPaso()
    {
        AudioManager.Instance.PlaySFX("CiegoPasos");
        //AudioManager.Instance.PlaySFXAtPoint("PasosCiego", transform.position);
    }

    public void Aturdido()
    {
        if (!stunActivado)
        {
            //animator.SetTrigger("stun");
            animator.SetBool("isStun", true);
            stunActivado = true;
            stunEffect.Play();
        }

        investigando = false;
        sonidoDetectado = false;
        perseguir = false;
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
