using UnityEngine;

public class WheelRotation : MonoBehaviour
{
    [Header("Wheels")]
    [SerializeField] Transform[] wheels;

    [Header("Settings")]
    [SerializeField] float maxRotationSpeed = 720f; // градусов/сек
    [SerializeField] float maxSpeedForRotation = 30f; // м/с

    private Rigidbody carRigidbody;

    void Start()
    {
        carRigidbody = GetComponent<Rigidbody>();

        if (wheels == null || wheels.Length == 0)
            FindWheelsAutomatically();
    }

    void FindWheelsAutomatically()
    {
        Transform[] allChildren = GetComponentsInChildren<Transform>();
        System.Collections.Generic.List<Transform> found = new System.Collections.Generic.List<Transform>();
        foreach (Transform child in allChildren)
            if (child.name.ToLower().Contains("wheel"))
                found.Add(child);
        wheels = found.ToArray();
    }

    void Update()
    {
        if (carRigidbody == null) return;

        float speed = carRigidbody.velocity.magnitude;
        float t = Mathf.Clamp01(speed / maxSpeedForRotation);
        float rotationAngle = maxRotationSpeed * t * Time.deltaTime;

        foreach (Transform wheel in wheels)
        {
            // Вращаем каждое колесо вокруг ЕГО локальной оси X
            wheel.Rotate(rotationAngle, 0, 0, Space.Self);
        }
    }
}