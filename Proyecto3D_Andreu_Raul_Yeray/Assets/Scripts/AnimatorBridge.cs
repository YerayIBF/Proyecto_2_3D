using UnityEngine;

public class AnimatorBridge : MonoBehaviour
{
    public CogerObjeto handScript;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void EventCoger()
    {
        handScript.AnimatorCogerObjeto();
    }
}
