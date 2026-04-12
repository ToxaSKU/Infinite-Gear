using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AICarSpawn : MonoBehaviour
{
    [Header("Car Prefabs")]
    [SerializeField] GameObject[] carAIPrefabs;

    [Header("Pool Settings")]
    [SerializeField] int poolSize = 30;
    private GameObject[] carAIPool;

    [Header("Spawn Timing")]
    [SerializeField] float minSpawnInterval = 0.5f;
    [SerializeField] float maxSpawnInterval = 1.5f;
    [SerializeField] int maxCarsPerSpawn = 2;  // Максимум машин за один спавн

    [Header("Spawn Distances")]
    [SerializeField] float spawnDistance = 80f;
    [SerializeField] float despawnBehindDistance = 50f;
    [SerializeField] float despawnAheadDistance = 300f;

    [Header("Lane Settings")]
    [SerializeField] bool useFixedLane = false;
    [SerializeField] float fixedLaneX = -12.87f;
    [SerializeField] float lane1X = -14.1f;
    [SerializeField] float lane2X = -12.87f;
    [SerializeField] float lane3X = -11.64f;
    [SerializeField] float lane4X = -10.41f;

    [Header("Distance Between Cars")]
    [SerializeField] float minDistanceBetweenCars = 10f;
    [SerializeField] float laneWidthThreshold = 2.0f;

    [Header("Collision Check")]
    [SerializeField] LayerMask otherCarsLayerMask;
    private Collider[] overlappedCheckCollider = new Collider[1];

    private Transform playerCarTransform;
    private float nextSpawnTime = 0;
    private WaitForSeconds wait = new WaitForSeconds(0.2f); // Чаще проверяем для множественного спавна

    private float[] laneCenters = new float[4];
    private List<int> availableLanes = new List<int>();

    void Start()
    {
        laneCenters[0] = lane1X;
        laneCenters[1] = lane2X;
        laneCenters[2] = lane3X;
        laneCenters[3] = lane4X;

        if (useFixedLane)
            Debug.Log($"AICarSpawn: Фиксированная полоса X = {fixedLaneX}");
        else
            Debug.Log($"AICarSpawn: Случайные полосы (4 шт.), макс. машин за спавн = {maxCarsPerSpawn}");

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("AICarSpawn: Нет объекта с тегом 'Player'!");
            enabled = false;
            return;
        }
        playerCarTransform = player.transform;

        carAIPool = new GameObject[poolSize];
        if (carAIPrefabs.Length == 0)
        {
            Debug.LogError("AICarSpawn: Нет префабов в carAIPrefabs!");
            enabled = false;
            return;
        }

        int prefabIndex = 0;
        for (int i = 0; i < poolSize; i++)
        {
            carAIPool[i] = Instantiate(carAIPrefabs[prefabIndex]);
            carAIPool[i].SetActive(false);

            if (carAIPool[i].GetComponent<AIHandler>() == null)
                carAIPool[i].AddComponent<AIHandler>();

            prefabIndex++;
            if (prefabIndex >= carAIPrefabs.Length)
                prefabIndex = 0;
        }

        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            CleanUpCarsBeyondView();
            SpawnMultipleCars();
            yield return wait;
        }
    }

    void SpawnMultipleCars()
    {
        if (Time.time < nextSpawnTime)
            return;

        // Собираем список полос, которые можно использовать
        List<float> lanesToUse = new List<float>();
        if (useFixedLane)
        {
            lanesToUse.Add(fixedLaneX);
        }
        else
        {
            // Перемешиваем порядок полос, чтобы не было приоритета
            availableLanes.Clear();
            for (int i = 0; i < laneCenters.Length; i++)
                availableLanes.Add(i);
            Shuffle(availableLanes);
            foreach (int idx in availableLanes)
                lanesToUse.Add(laneCenters[idx]);
        }

        int spawnedThisCycle = 0;
        // Пытаемся заспавнить до maxCarsPerSpawn машин на разных полосах
        foreach (float laneX in lanesToUse)
        {
            if (spawnedThisCycle >= maxCarsPerSpawn)
                break;

            // Проверяем, есть ли свободная машина в пуле
            GameObject carToSpawn = GetFreeCarFromPool();
            if (carToSpawn == null)
                break; // Нет свободных машин

            // Проверяем, можно ли спавнить на этой полосе
            if (CanSpawnOnLane(laneX))
            {
                // Спавним машину
                Vector3 spawnPos = new Vector3(laneX, playerCarTransform.position.y, playerCarTransform.position.z + spawnDistance);
                carToSpawn.transform.position = spawnPos;
                carToSpawn.transform.rotation = Quaternion.identity;
                carToSpawn.SetActive(true);
                spawnedThisCycle++;
            }
            else
            {
                // Возвращаем машину обратно в пул (не использовали)
                // Ничего не делаем, просто не активируем
            }
        }

        if (spawnedThisCycle > 0)
        {
            // Устанавливаем следующий интервал спавна (от последнего спавна)
            nextSpawnTime = Time.time + Random.Range(minSpawnInterval, maxSpawnInterval);
        }
        else
        {
            // Если ни одной не заспавнили, пробуем снова через короткое время
            nextSpawnTime = Time.time + 0.5f;
        }
    }

    GameObject GetFreeCarFromPool()
    {
        foreach (GameObject car in carAIPool)
        {
            if (!car.activeInHierarchy)
                return car;
        }
        return null;
    }

    bool CanSpawnOnLane(float laneX)
    {
        float spawnZ = playerCarTransform.position.z + spawnDistance;

        // Проверяем, нет ли на этой же полосе машин слишком близко
        foreach (GameObject car in carAIPool)
        {
            if (!car.activeInHierarchy) continue;
            float xDiff = Mathf.Abs(car.transform.position.x - laneX);
            if (xDiff > laneWidthThreshold) continue; // Другая полоса
            float zDiff = Mathf.Abs(car.transform.position.z - spawnZ);
            if (zDiff < minDistanceBetweenCars)
                return false;
        }

        // Проверка дистанции до игрока
        if (Mathf.Abs(playerCarTransform.position.z - spawnZ) < minDistanceBetweenCars)
            return false;

        // Проверка физическим боксом (опционально)
        Vector3 checkPos = new Vector3(laneX, playerCarTransform.position.y, spawnZ);
        if (Physics.OverlapBoxNonAlloc(checkPos, Vector3.one * 2f, overlappedCheckCollider, Quaternion.identity, otherCarsLayerMask) > 0)
            return false;

        return true;
    }

    void CleanUpCarsBeyondView()
    {
        if (playerCarTransform == null) return;
        foreach (GameObject car in carAIPool)
        {
            if (!car.activeInHierarchy) continue;
            float distanceFromPlayer = car.transform.position.z - playerCarTransform.position.z;
            if (distanceFromPlayer > despawnAheadDistance || distanceFromPlayer < -despawnBehindDistance)
                car.SetActive(false);
        }
    }

    void Shuffle(List<int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    void OnDrawGizmosSelected()
    {
        // Визуализация (как раньше)
        if (!Application.isPlaying)
        {
            laneCenters[0] = lane1X;
            laneCenters[1] = lane2X;
            laneCenters[2] = lane3X;
            laneCenters[3] = lane4X;
        }
        Gizmos.color = Color.cyan;
        float startZ = (playerCarTransform != null) ? playerCarTransform.position.z - 50f : 0f;
        float endZ = (playerCarTransform != null) ? playerCarTransform.position.z + spawnDistance + 50f : 100f;
        if (useFixedLane && Application.isPlaying)
        {
            Gizmos.DrawLine(new Vector3(fixedLaneX, 0.5f, startZ), new Vector3(fixedLaneX, 0.5f, endZ));
        }
        else
        {
            foreach (float x in laneCenters)
            {
                Gizmos.DrawLine(new Vector3(x, 0.5f, startZ), new Vector3(x, 0.5f, endZ));
            }
        }
    }
}