using UnityEngine;

public class AnimatorBridge : MonoBehaviour
{
    public CogerObjeto handScript;

    void Start()
    {

    }

    void Update()
    {

    }

    public void EventCoger()
    {
        handScript.AnimatorCogerObjeto();
    }

    public void LanzarObjeto()
    {
        handScript.LanzarObjeto();
    }
}