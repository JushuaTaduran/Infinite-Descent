using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BreathingLight : MonoBehaviour
{
    private Light2D light2D;

    [Header("Breathing Settings")]
    [SerializeField] private float minIntensity = 0.5f;
    [SerializeField] private float maxIntensity = 1.5f;
    [SerializeField] private float pulseSpeed = 2f;

    void Start()
    {
        light2D = GetComponent<Light2D>();
        StartBreathingEffect();
    }

    void StartBreathingEffect()
    {
        LeanTween.value(gameObject, minIntensity, maxIntensity, pulseSpeed)
            .setEase(LeanTweenType.easeInOutSine)
            .setLoopPingPong()
            .setOnUpdate((float val) =>
            {
                if (light2D != null)
                    light2D.intensity = val;
            });
    }
}
