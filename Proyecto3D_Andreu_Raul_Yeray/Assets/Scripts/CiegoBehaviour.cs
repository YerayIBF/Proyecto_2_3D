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

    private List<Vector3> sonidos = new List<Vector3>();

    public float areaVision = 10f;

    private NavMeshAgent agent;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        if (aturdido)
        {
            //aturdido 5 segundos
        }
        else
        {
            return;
        }

        if (sonidos.Count > 0)
        {
            if (SonidoCercano())
            {
                Atacar();
            }
            else
            {
                if (investigando)
                {
                    //comprobar cooldown investigar, si ha terminado patrulla
                    Investigar();
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
                //comprobar cooldown investigar, si ha terminado patrulla
                Investigar();
            }
            else
            {
                Patrullar();
            }
        }
    }

    private  bool SonidoCercano()
    {
        return false;
    }

    private bool Investigar()
    {
        return false;
    }

    private bool Atacar()
    {
        return false;
    }

    private bool DetectarSonido()
    {
        return false;
    }

    public void Patrullar()
    {
        
    }

    public void Perseguir()
    {
        
    }
}
