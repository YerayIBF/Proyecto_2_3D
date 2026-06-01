using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class ProgressManager : MonoBehaviour
{
    public static ProgressManager instance;

    //puzzles
    public List<string> puzzlesCompletados = new List<string>();
    private const int puzzlesTotales = 4;
    public string archivoGuardado;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        archivoGuardado = Application.dataPath + "/datosJuego.json";
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public float PorcentajeProgreso()
    {
        if (puzzlesCompletados.Count == 0){
            return 0f; 
        }
        
        float porcentaje = (float)puzzlesCompletados.Count / puzzlesTotales * 100f;
        return Mathf.Clamp(porcentaje, 0f, 100f);
    }

    public bool PuzzleCompletado(string nombrePuzzle)
    {
        return puzzlesCompletados.Contains(nombrePuzzle);
    }

    public void RegistrarPuzzleCompletado(string nombrePuzzle)
    {
        if (!puzzlesCompletados.Contains(nombrePuzzle))
        {
            puzzlesCompletados.Add(nombrePuzzle);
            Debug.Log("Puzzle marcado como completado: " + nombrePuzzle);
        }
    }

    public void GuardarPartida()
    {
        DatosPartida datos = new DatosPartida()
        {
            puzzlesGuardados = new List<string>(puzzlesCompletados)
        };

        string textoJson = JsonUtility.ToJson(datos, true);
        File.WriteAllText(archivoGuardado, textoJson);
    }

    public void CargarPartida()
    {
        if (File.Exists(archivoGuardado))
        {
            string contenido = File.ReadAllText(archivoGuardado);
            DatosPartida datos = JsonUtility.FromJson<DatosPartida>(contenido);

            //Recuperar puzzles completados guardados
            puzzlesCompletados = new List<string>(datos.puzzlesGuardados);
        }
        else
        {
            Debug.Log("El archivo no existe");
        }
    }
}

[System.Serializable]
public class DatosPartida
{
    /*public int nivelActualGuardado;
    public int monedasGuardadas;
    public float saludGuardada;*/
    public List<string> puzzlesGuardados;
}