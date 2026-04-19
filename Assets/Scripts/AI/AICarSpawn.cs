using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AICarSpawn : MonoBehaviour
{
    [Header("Car Prefabs")]
    [SerializeField] GameObject[] carAIPrefabs;

    [Header("Pool Settings")]
    [SerializeField] int poolSize = 40;
    private GameObject[] carAIPool;

    [Header("Spawn Timing")]
    [SerializeField] float spawnInterval = 0.8f;  // Фиксированный интервал вместо случайного
    [SerializeField] int carsPerSpawn = 2;  // Количество машин за спавн

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

    [Header("Traffic Density")]
    [SerializeField] float trafficDensity = 0.8f;
    [SerializeField] float densityAdjustSpeed = 0.05f;  // Скорость подстройки плотности

    [Header("Distance Between Cars")]
    [SerializeField] float minDistanceBetweenCars = 8f;
    [SerializeField] float laneWidthThreshold = 2.0f;

    [Header("Collision Check")]
    [SerializeField] LayerMask otherCarsLayerMask;
    private Collider[] overlappedCheckCollider = new Collider[3];

    private Transform playerCarTransform;
    private float nextSpawnTime = 0;
    private WaitForSeconds wait = new WaitForSeconds(0.1f);

    private float[] laneCenters = new float[4];
    private int[] laneSpawnCounts = new int[4];  // Счётчик спавнов на каждой полосе
    private int totalSpawns = 0;

    private float currentSpawnInterval;
    private int currentCarsPerSpawn;

    void Start()
    {
        laneCenters[0] = lane1X;
        laneCenters[1] = lane2X;
        laneCenters[2] = lane3X;
        laneCenters[3] = lane4X;

        currentSpawnInterval = spawnInterval;
        currentCarsPerSpawn = carsPerSpawn;

        Debug.Log($"AICarSpawn: Равномерный спавн, интервал {spawnInterval} сек, {carsPerSpawn} машин за раз");

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
        StartCoroutine(DensityAdjuster());
    }

    IEnumerator DensityAdjuster()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);

            int activeCars = GetActiveCarsCount();
            float currentDensity = (float)activeCars / poolSize;

            // Плавно подстраиваем интервал спавна под целевую плотность
            if (currentDensity < trafficDensity)
            {
                // Мало машин - спавним чаще
                currentSpawnInterval = Mathf.Max(0.3f, currentSpawnInterval - densityAdjustSpeed);
                currentCarsPerSpawn = Mathf.Min(carsPerSpawn + 1, 3);
            }
            else if (currentDensity > trafficDensity)
            {
                // Много машин - спавним реже
                currentSpawnInterval = Mathf.Min(1.5f, currentSpawnInterval + densityAdjustSpeed);
                currentCarsPerSpawn = Mathf.Max(1, carsPerSpawn - 1);
            }
        }
    }

    int GetActiveCarsCount()
    {
        int count = 0;
        foreach (GameObject car in carAIPool)
        {
            if (car.activeInHierarchy) count++;
        }
        return count;
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

    // Выбирает следующую полосу для спавна (Round-Robin)
    int GetNextLaneIndex()
    {
        if (totalSpawns == 0)
            return Random.Range(0, laneCenters.Length);

        // Находим полосу с наименьшим количеством спавнов
        int minIndex = 0;
        int minCount = laneSpawnCounts[0];

        for (int i = 1; i < laneSpawnCounts.Length; i++)
        {
            if (laneSpawnCounts[i] < minCount)
            {
                minCount = laneSpawnCounts[i];
                minIndex = i;
            }
        }

        return minIndex;
    }

    void SpawnMultipleCars()
    {
        if (Time.time < nextSpawnTime)
            return;

        // Определяем, сколько машин спавнить сейчас
        int carsToSpawn = currentCarsPerSpawn;

        // Дополнительная проверка: если свободных машин мало - спавним меньше
        int freeCars = GetFreeCarsCount();
        carsToSpawn = Mathf.Min(carsToSpawn, freeCars);

        if (carsToSpawn <= 0)
        {
            nextSpawnTime = Time.time + 0.5f;
            return;
        }

        // Создаём список полос для спавна (без повторений в одном цикле)
        List<int> usedLanesThisCycle = new List<int>();
        int spawnedThisCycle = 0;

        // Сначала выбираем лучшие полосы (с наименьшим количеством спавнов)
        List<int> sortedLanes = new List<int>();
        for (int i = 0; i < laneCenters.Length; i++)
            sortedLanes.Add(i);

        sortedLanes.Sort((a, b) => laneSpawnCounts[a].CompareTo(laneSpawnCounts[b]));

        foreach (int laneIndex in sortedLanes)
        {
            if (spawnedThisCycle >= carsToSpawn)
                break;

            if (usedLanesThisCycle.Contains(laneIndex))
                continue;

            float laneX = laneCenters[laneIndex];

            if (CanSpawnOnLane(laneX))
            {
                GameObject carToSpawn = GetFreeCarFromPool();
                if (carToSpawn == null)
                    break;

                // Небольшой случайный сдвиг по Z для разнообразия
                float zOffset = Random.Range(-3f, 3f);

                Vector3 spawnPos = new Vector3(
                    laneX,
                    playerCarTransform.position.y,
                    playerCarTransform.position.z + spawnDistance + zOffset
                );

                carToSpawn.transform.position = spawnPos;
                carToSpawn.transform.rotation = Quaternion.identity;
                carToSpawn.SetActive(true);

                spawnedThisCycle++;
                usedLanesThisCycle.Add(laneIndex);
                laneSpawnCounts[laneIndex]++;
                totalSpawns++;
            }
        }

        if (spawnedThisCycle > 0)
        {
            // Фиксированный интервал для равномерного спавна
            nextSpawnTime = Time.time + currentSpawnInterval;

            // Периодически выводим статистику спавна
            if (totalSpawns % 20 == 0)
            {
                Debug.Log($"Статистика спавна по полосам: L1={laneSpawnCounts[0]}, L2={laneSpawnCounts[1]}, L3={laneSpawnCounts[2]}, L4={laneSpawnCounts[3]}");
            }
        }
        else
        {
            // Если не удалось заспавнить, пробуем через короткое время
            nextSpawnTime = Time.time + 0.3f;
        }
    }

    int GetFreeCarsCount()
    {
        int count = 0;
        foreach (GameObject car in carAIPool)
        {
            if (!car.activeInHierarchy)
                count++;
        }
        return count;
    }

    GameObject GetFreeCarFromPool()
    {
        int startIndex = Random.Range(0, poolSize);
        for (int i = 0; i < poolSize; i++)
        {
            int index = (startIndex + i) % poolSize;
            if (!carAIPool[index].activeInHierarchy)
                return carAIPool[index];
        }
        return null;
    }

    bool CanSpawnOnLane(float laneX)
    {
        float spawnZ = playerCarTransform.position.z + spawnDistance;

        // Проверяем дистанцию до других машин на этой полосе
        foreach (GameObject car in carAIPool)
        {
            if (!car.activeInHierarchy) continue;
            float xDiff = Mathf.Abs(car.transform.position.x - laneX);
            if (xDiff > laneWidthThreshold) continue;
            float zDiff = Mathf.Abs(car.transform.position.z - spawnZ);
            if (zDiff < minDistanceBetweenCars)
                return false;
        }

        // Проверка дистанции до игрока
        if (Mathf.Abs(playerCarTransform.position.z - spawnZ) < minDistanceBetweenCars)
            return false;

        // Проверка физическим боксом
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

            // Получаем реальную позицию модели (первый дочерний объект)
            Transform modelTransform = car.transform.GetChild(0);
            float carZ = modelTransform != null ? modelTransform.position.z : car.transform.position.z;

            float distanceFromPlayer = carZ - playerCarTransform.position.z;

            if (distanceFromPlayer > despawnAheadDistance || distanceFromPlayer < -despawnBehindDistance)
            {
                car.SetActive(false);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
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

        Gizmos.color = Color.green;
        Vector3 spawnZoneCenter = new Vector3(0, 0, playerCarTransform?.position.z + spawnDistance ?? 100);
        Vector3 spawnZoneSize = new Vector3(8f, 2f, 10f);
        Gizmos.DrawWireCube(spawnZoneCenter, spawnZoneSize);
    }
}