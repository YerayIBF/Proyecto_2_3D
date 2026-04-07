using UnityEngine;
using UnityEngine.AI;


public class AITester : MonoBehaviour
{
    public float soundRadius = 10f;

    void Update()
    {
        // Al pulsar Space, emite un sonido donde hagas clic con el ratón
        if (Input.GetKeyDown(KeyCode.Space))
        {
            EmitSoundAtMousePosition();
        }
    }

    void EmitSoundAtMousePosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Emite el sonido en el punto donde clicaste
            EmitirSonido.instance.EmitirRuido(hit.point, soundRadius);

            // Dibuja una esfera visual en esa posición durante 3 segundos
            Debug.DrawLine(hit.point, hit.point + Vector3.up * 2f, Color.red, 3f);
            Debug.Log($"Sonido emitido en: {hit.point}");
        }
    }
}