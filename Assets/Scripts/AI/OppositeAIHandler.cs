using System.Collections;
using UnityEngine;

public class OppositeTrafficSpawner : MonoBehaviour
{
    [SerializeField]
    GameObject[] oppositeCarPrefabs;

    GameObject[] oppositeCarPool = new GameObject[15];

    Transform playerCarTransform;

    [SerializeField]
    LayerMask trafficLayerMask;

    Collider[] overlappedCheckCollider = new Collider[1];

    [SerializeField]
    float spawnDistance = 80f;

    [SerializeField]
    float despawnDistance = 150f;

    [SerializeField]
    float minSpawnInterval = 3f;

    [SerializeField]
    float maxSpawnInterval = 5f;

    [Header("Lane Settings (Left Side - Opposite)")]
    [SerializeField]
    float lane1Center = -14.1f;  // Левая встречная (крайняя левая)

    [SerializeField]
    float lane2Center = -12.87f; // Правая встречная (ближе к центру)

    [Header("Distance Settings")]
    [SerializeField]
    float minDistanceBetweenCars = 15f;

    float nextSpawnTime = 0;

    void Start()
    {
        playerCarTransform = GameObject.FindGameObjectWithTag("Player").transform;

        if (oppositeCarPrefabs.Length > 0)
        {
            int prefabIndex = 0;
            for (int i = 0; i < oppositeCarPool.Length; i++)
            {
                oppositeCarPool[i] = Instantiate(oppositeCarPrefabs[prefabIndex]);
                oppositeCarPool[i].SetActive(false);

                AIHandler oldAi = oppositeCarPool[i].GetComponent<AIHandler>();
                if (oldAi != null)
                    Destroy(oldAi);

                if (oppositeCarPool[i].GetComponent<OppositeAIHandler>() == null)
                    oppositeCarPool[i].AddComponent<OppositeAIHandler>();

                prefabIndex++;
                if (prefabIndex >= oppositeCarPrefabs.Length)
                    prefabIndex = 0;
            }
        }

        Debug.Log($"=== ВСТРЕЧНЫЕ ПОЛОСЫ (ЛЕВАЯ СТОРОНА) ===");
        Debug.Log($"Lane 1 (левая встречная): {lane1Center}");
        Debug.Log($"Lane 2 (правая встречная): {lane2Center}");
        Debug.Log($"=========================================");

        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            SpawnOppositeCar();
            CleanupCars();
            yield return new WaitForSeconds(0.2f);
        }
    }

    bool IsSafeDistance(float spawnZ)
    {
        foreach (GameObject car in oppositeCarPool)
        {
            if (!car.activeInHierarchy) continue;

            float distance = Mathf.Abs(car.transform.position.z - spawnZ);
            if (distance < minDistanceBetweenCars)
                return false;
        }
        return true;
    }

    bool IsSafeFromPlayer(float spawnZ)
    {
        if (playerCarTransform == null) return true;
        return Mathf.Abs(playerCarTransform.position.z - spawnZ) > minDistanceBetweenCars;
    }

    float GetRandomLaneCenter()
    {
        // Только встречные полосы (1 и 2) - левая сторона
        return Random.Range(0, 2) == 0 ? lane1Center : lane2Center;
    }

    void SpawnOppositeCar()
    {
        if (Time.time < nextSpawnTime)
            return;

        if (oppositeCarPool.Length == 0)
            return;

        GameObject carToSpawn = null;
        foreach (GameObject car in oppositeCarPool)
        {
            if (!car.activeInHierarchy)
            {
                carToSpawn = car;
                break;
            }
        }

        if (carToSpawn == null)
            return;

        float exactX = GetRandomLaneCenter();
        float baseZ = playerCarTransform.position.z + spawnDistance;
        float bestZ = baseZ;
        bool foundSpot = false;

        for (float offset = 0; offset <= 40; offset += 5)
        {
            float testZ = baseZ + offset;

            if (IsSafeDistance(testZ) && IsSafeFromPlayer(testZ))
            {
                bestZ = testZ;
                foundSpot = true;
                break;
            }
        }

        if (!foundSpot)
        {
            nextSpawnTime = Time.time + 1f;
            return;
        }

        Vector3 spawnPosition = new Vector3(exactX, playerCarTransform.position.y, bestZ);

        if (Physics.OverlapBoxNonAlloc(spawnPosition, Vector3.one * 2.5f, overlappedCheckCollider, Quaternion.identity, trafficLayerMask) > 0)
            return;

        carToSpawn.transform.position = spawnPosition;
        carToSpawn.SetActive(true);

        nextSpawnTime = Time.time + Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    void CleanupCars()
    {
        foreach (GameObject car in oppositeCarPool)
        {
            if (!car.activeInHierarchy)
                continue;

            float distanceFromPlayer = car.transform.position.z - playerCarTransform.position.z;

            if (Mathf.Abs(distanceFromPlayer) > despawnDistance)
            {
                car.SetActive(false);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 lane1Start = new Vector3(lane1Center, 0.5f, 0);
        Vector3 lane1End = new Vector3(lane1Center, 0.5f, 100);
        Gizmos.DrawLine(lane1Start, lane1End);

        Gizmos.color = new Color(1, 0.5f, 0);
        Vector3 lane2Start = new Vector3(lane2Center, 0.5f, 0);
        Vector3 lane2End = new Vector3(lane2Center, 0.5f, 100);
        Gizmos.DrawLine(lane2Start, lane2End);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(new Vector3(lane1Center, 2f, 50), "Opposite Lane 1");
        UnityEditor.Handles.Label(new Vector3(lane2Center, 2f, 50), "Opposite Lane 2");
#endif
    }
}