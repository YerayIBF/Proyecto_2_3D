using UnityEngine;

public class RandomizarCables : MonoBehaviour
{
    private void Awake()
    {
        int count = transform.childCount;

        // Intercambia solo las posiciones LOCALES en Y (el color/cable sube o baja)
        for (int i = 0; i < count; i++)
        {
            int j = Random.Range(0, count);

            Transform a = transform.GetChild(i);
            Transform b = transform.GetChild(j);

            Vector3 posA = a.localPosition;
            Vector3 posB = b.localPosition;

            // Solo intercambia la Y para mantener X y Z intactas
            float tempY = posA.y;
            posA.y = posB.y;
            posB.y = tempY;

            a.localPosition = posA;
            b.localPosition = posB;
        }
    }
}