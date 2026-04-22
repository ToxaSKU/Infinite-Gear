using UnityEngine;
using System.Collections.Generic;

public class OvertakeSound : MonoBehaviour
{
    [Header("References")]
    [SerializeField] CarHandler carHandler;
    [SerializeField] AudioSource overtakeAudioSource;
    [SerializeField] ScoreManager scoreManager;

    [Header("Sound Settings")]
    [SerializeField] AudioClip overtakeClip;
    [SerializeField] float overtakeVolume = 0.7f;
    [SerializeField] float minSpeedForOvertake = 10f;
    [SerializeField] float maxSideDistance = 2.5f;

    [Header("Cooldown")]
    [SerializeField] float minTimeBetweenOvertakes = 0.5f;

    private float lastOvertakeTime = 0;
    private Transform playerTransform;
    private List<int> recentlyOvertaken = new List<int>();

    void Start()
    {
        if (carHandler == null)
            carHandler = GetComponent<CarHandler>();

        if (scoreManager == null)
            scoreManager = FindObjectOfType<ScoreManager>();

        if (overtakeAudioSource == null)
        {
            overtakeAudioSource = gameObject.AddComponent<AudioSource>();
            overtakeAudioSource.playOnAwake = false;
            overtakeAudioSource.spatialBlend = 0f;
            overtakeAudioSource.volume = overtakeVolume;
        }

        playerTransform = transform;

        Debug.Log("OvertakeSound initialized");
    }

    void Update()
    {
        CheckForOvertake();
    }

    void CheckForOvertake()
    {
        if (carHandler == null) return;

        float currentSpeed = Mathf.Abs(carHandler.GetCurrentSpeed());
        if (currentSpeed < minSpeedForOvertake) return;

        // Находим все AI машины
        AIHandler[] allCars = FindObjectsOfType<AIHandler>();

        foreach (AIHandler car in allCars)
        {
            if (car == null) continue;

            int carId = car.gameObject.GetInstanceID();

            // Пропускаем недавно обогнанные машины
            if (recentlyOvertaken.Contains(carId)) continue;

            Transform carTransform = car.transform;
            float distanceZ = carTransform.position.z - playerTransform.position.z;
            float distanceX = Mathf.Abs(carTransform.position.x - playerTransform.position.x);

            // Получаем скорость другой машины
            CarHandler otherCarHandler = car.GetComponent<CarHandler>();
            float otherSpeed = otherCarHandler != null ? Mathf.Abs(otherCarHandler.GetCurrentSpeed()) : 10f;
            float speedDiff = currentSpeed - otherSpeed;

            // Логируем для отладки
            if (distanceZ < 5f && distanceZ > -5f)
            {
                Debug.Log($"Car: {car.name}, Z diff: {distanceZ:F1}, X diff: {distanceX:F1}, Speed diff: {speedDiff:F1}");
            }

            // Условие обгона:
            // 1. Машина рядом по Z (сзади или сбоку)
            // 2. Машина на той же или соседней полосе
            // 3. Скорость игрока выше
            if (distanceZ < 2f && distanceZ > -8f && distanceX < maxSideDistance && speedDiff > 2f)
            {
                Debug.Log($"OVERTAKE! {car.name} | Speed diff: {speedDiff:F1}");
                PlayOvertake(carId);
                break;
            }
        }
    }

    void PlayOvertake(int carId)
    {
        if (Time.time - lastOvertakeTime < minTimeBetweenOvertakes) return;

        lastOvertakeTime = Time.time;
        recentlyOvertaken.Add(carId);

        // Звук
        if (overtakeClip != null && overtakeAudioSource != null)
            overtakeAudioSource.PlayOneShot(overtakeClip, overtakeVolume);

        // Начисление очков
        if (scoreManager != null)
        {
            scoreManager.AddOvertake();
            Debug.Log("Overtake score added!");
        }
        else
        {
            Debug.LogError("ScoreManager is NULL!");
        }

        // Очищаем список через 5 секунд
        Invoke(nameof(ClearRecentOvertakes), 5f);
    }

    void ClearRecentOvertakes()
    {
        recentlyOvertaken.Clear();
    }
}