using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;

public class Creditos : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Invoke("WaitForEnd", 7);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void WaitForEnd()
    {
        SceneManager.LoadScene("Menu");
    }
}
