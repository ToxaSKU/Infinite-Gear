using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIHandler : MonoBehaviour
{
    [SerializeField] CarHandler carHandler;
    [SerializeField] MeshCollider meshCollider; // опционально, для отключения при проверках

    [Header("Distance Settings")]
    [SerializeField] float targetFollowDistance = 8f;   // желаемая дистанция до впереди идущей машины
    [SerializeField] float minBrakeDistance = 4f;       // при такой дистанции начинаем тормозить
    [SerializeField] float emergencyDistance = 2f;      // аварийное торможение (почти останавливаемся)
    [SerializeField] float maxSpeed = 12f;              // максимальная скорость (км/ч или условно)
    [SerializeField] float checkInterval = 0.1f;

    private float currentGas = 1f;
    private float distanceToCarAhead = Mathf.Infinity;

    // Статический список всех активных AI машин (для перекрёстных проверок)
    private static List<AIHandler> allAICars = new List<AIHandler>();

    private void Awake()
    {
        if (CompareTag("Player"))
        {
            Destroy(this);
            return;
        }
    }

    private void OnEnable()
    {
        allAICars.Add(this);
        if (carHandler != null)
            carHandler.SetMaxSpeed(maxSpeed);
        currentGas = 1f;
    }

    private void OnDisable()
    {
        allAICars.Remove(this);
    }

    void Start()
    {
        StartCoroutine(CheckDistanceRoutine());
    }

    IEnumerator CheckDistanceRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(checkInterval);
        while (true)
        {
            yield return wait;
            CheckDistanceToCarAhead();
        }
    }

    void CheckDistanceToCarAhead()
    {
        float closestDistance = Mathf.Infinity;
        float myZ = transform.position.z;
        float myX = transform.position.x;

        foreach (AIHandler other in allAICars)
        {
            if (other == this) continue;
            if (!other.gameObject.activeInHierarchy) continue;

            float otherZ = other.transform.position.z;
            float otherX = other.transform.position.x;

            // Машина должна быть ВПЕРЕДИ (по Z) и не дальше 30 метров
            if (otherZ > myZ && otherZ - myZ < 30f)
            {
                // Проверяем, что машина примерно на той же полосе (разница по X не более 2 метров)
                if (Mathf.Abs(myX - otherX) < 2.5f)
                {
                    float dist = otherZ - myZ;
                    if (dist < closestDistance)
                        closestDistance = dist;
                }
            }
        }

        distanceToCarAhead = closestDistance;
        UpdateGasBasedOnDistance();
    }

    void UpdateGasBasedOnDistance()
    {
        if (distanceToCarAhead < emergencyDistance)
        {
            // Аварийная ситуация – почти стоим
            currentGas = 0.1f;
        }
        else if (distanceToCarAhead < minBrakeDistance)
        {
            // Плавное торможение: чем ближе, тем меньше газа
            float t = (distanceToCarAhead - emergencyDistance) / (minBrakeDistance - emergencyDistance);
            currentGas = Mathf.Lerp(0.2f, 1f, t);
        }
        else if (distanceToCarAhead < targetFollowDistance)
        {
            // Немного притормаживаем, чтобы держать дистанцию
            currentGas = 0.7f;
        }
        else
        {
            // Свободная дорога
            currentGas = 1f;
        }

        currentGas = Mathf.Clamp(currentGas, 0.1f, 1f);
    }

    void Update()
    {
        if (carHandler != null)
            carHandler.SetInput(new Vector2(0f, currentGas));
    }

    // Для отладки в редакторе
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        if (distanceToCarAhead < 15f)
        {
            Gizmos.color = Color.Lerp(Color.red, Color.green, distanceToCarAhead / 15f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * distanceToCarAhead);
            Gizmos.DrawWireSphere(transform.position + transform.forward * distanceToCarAhead, 1f);
        }
    }
}