using UnityEngine;

public class PilaLinterna : MonoBehaviour
{
    public int cantidad = 1;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerStateMachine>() != null)
            Recoger();
    }

    public void Recoger()
    {
        PlayerStateMachine.Instance?.AddBattery(cantidad);
        gameObject.SetActive(false);
    }
}