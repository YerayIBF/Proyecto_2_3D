using UnityEngine;

public class PilaMegafonoNueva : MonoBehaviour
{
    public int cantidad = 1;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerStateMachine>() != null)
            Recoger();
    }

    public void Recoger()
    {
        PlayerStateMachine.Instance?.AddMegafonoBattery(cantidad);
        gameObject.SetActive(false);
    }
}