using UnityEngine;

public class LightEffect : MonoBehaviour
{
    private Light light;
    public float maxWaitTime = 1f;
    public float maxFlickerTime = 0.2f;

    float timer;
    float interval;

    public float minIntensity = 1f;
    public float maxIntensity = 2f;
    public float speed = 5f;
    float targetIntensity;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        light = GetComponent<Light>();
        targetIntensity = maxIntensity;
    }

    // Update is called once per frame
    void Update()
    {
        light.intensity = Mathf.Lerp(light.intensity, targetIntensity, Time.deltaTime * speed);
        if (Mathf.Abs(light.intensity - targetIntensity) < 0.05f)
        {
            if (targetIntensity == maxIntensity)
            {
                targetIntensity = minIntensity;
            }
            else
            {
                targetIntensity = maxIntensity;
            }
        }
    }
}

