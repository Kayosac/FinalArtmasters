using UnityEngine;

[ExecuteAlways]
public class CameraShakePerlinAnimatable : MonoBehaviour
{
    [Header("Shake Control")]
    [Range(0f, 1f)] public float shakeAmount = 1.0f; // можно анимировать через Animator
    public bool shake = true;

    [Header("Noise Settings")]
    public float baseFrequency = 1.0f;
    public Vector3 baseAmplitude = new Vector3(0.1f, 0.1f, 0f);
    public Vector3 baseRotationAmplitude = new Vector3(1f, 1f, 1f);

    [Header("Smoothing")]
    [Tooltip("How quickly the effect interpolates (0 = instant)")]
    [Range(0f, 10f)] public float smoothSpeed = 5f;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private float timeOffset;

    private float currentShakeAmount = 0f;

    void OnEnable()
    {
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
        timeOffset = Random.Range(0f, 1000f);
    }

    void Update()
    {
        if (!Application.isPlaying && !shake)
        {
            ResetTransform();
            return;
        }

        currentShakeAmount = Mathf.Lerp(currentShakeAmount, shakeAmount, Time.deltaTime * smoothSpeed);
        ApplyShake(currentShakeAmount);
    }

    void ApplyShake(float weight)
    {
        float time = (Application.isPlaying ? Time.time : Time.realtimeSinceStartup) * baseFrequency + timeOffset;

        float x = (Mathf.PerlinNoise(time, 0f) - 0.5f) * 2f;
        float y = (Mathf.PerlinNoise(0f, time) - 0.5f) * 2f;
        float z = (Mathf.PerlinNoise(time, time) - 0.5f) * 2f;

        Vector3 posOffset = new Vector3(x * baseAmplitude.x, y * baseAmplitude.y, z * baseAmplitude.z) * weight;
        Vector3 rotOffset = new Vector3(x * baseRotationAmplitude.x, y * baseRotationAmplitude.y, z * baseRotationAmplitude.z) * weight;

        transform.localPosition = initialPosition + posOffset;
        transform.localRotation = initialRotation * Quaternion.Euler(rotOffset);
    }

    void ResetTransform()
    {
        transform.localPosition = initialPosition;
        transform.localRotation = initialRotation;
    }

    void OnDisable()
    {
        ResetTransform();
    }
}
