using System.Collections;
using UnityEngine;

public class AICarSpawn : MonoBehaviour
{
    [SerializeField]
    GameObject[] carAIPrefabs;

    GameObject[] carAIPool = new GameObject[20];

    WaitForSeconds wait = new WaitForSeconds(0.5f);

    Transform playerCarTransform;

    [SerializeField]
    LayerMask otherCarsLayerMask;

    Collider[] overlappedCheckCollider = new Collider[1];

    [SerializeField]
    float spawnDistance = 80f;

    [SerializeField]
    float despawnBehindDistance = 50f;

    [SerializeField]
    float despawnAheadDistance = 200f;

    [SerializeField]
    float minSpawnInterval = 1f;

    [SerializeField]
    float maxSpawnInterval = 3f;

    [Header("Road Settings")]
    [SerializeField]
    float roadCenterX = -12.87f;

    [SerializeField]
    float roadTotalWidth = 3.75f;

    [SerializeField]
    float curbWidth = 0.33f;

    private float usableRoadWidth;
    private float lane3Center;
    private float lane4Center;

    [Header("Distance Settings")]
    [SerializeField]
    float minDistanceBetweenCars = 15f;

    float nextSpawnTime = 0;

    void Start()
    {
        CalculateRoadAndLanes();
        playerCarTransform = GameObject.FindGameObjectWithTag("Player").transform;

        int prefabIndex = 0;
        for (int i = 0; i < carAIPool.Length; i++)
        {
            carAIPool[i] = Instantiate(carAIPrefabs[prefabIndex]);
            carAIPool[i].SetActive(false);

            if (carAIPool[i].GetComponent<AIHandler>() == null)
                carAIPool[i].AddComponent<AIHandler>();

            prefabIndex++;
            if (prefabIndex > carAIPrefabs.Length - 1)
                prefabIndex = 0;
        }

        StartCoroutine(UpdateLessOftenCO());
    }

    void CalculateRoadAndLanes()
    {
        usableRoadWidth = roadTotalWidth - (curbWidth * 2);

        float halfUsable = usableRoadWidth / 2f;
        float roadLeftUsable = roadCenterX - halfUsable;

        float laneWidth = usableRoadWidth / 4f;

        float lane1Center = roadLeftUsable + (laneWidth / 2f);
        float lane2Center = lane1Center + laneWidth;
        lane3Center = lane2Center + laneWidth;
        lane4Center = lane3Center + laneWidth;
    }

    IEnumerator UpdateLessOftenCO()
    {
        while (true)
        {
            CleanUpCarsBeyondView();
            SpawnNewCars();
            yield return wait;
        }
    }

    bool IsSafeDistance(float spawnZ)
    {
        foreach (GameObject car in carAIPool)
        {
            if (!car.activeInHierarchy) continue;

            float distance = Mathf.Abs(car.transform.position.z - spawnZ);
            if (distance < minDistanceBetweenCars)
                return false;
        }
        return true;
    }

    bool IsSafeDistanceFromPlayer(float spawnZ)
    {
        if (playerCarTransform == null) return true;
        return Mathf.Abs(playerCarTransform.position.z - spawnZ) > minDistanceBetweenCars;
    }

    float GetRandomSameLanePosition()
    {
        int laneIndex = Random.Range(0, 2) == 0 ? 2 : 3;
        return laneIndex == 2 ? lane3Center : lane4Center;
    }

    void SpawnNewCars()
    {
        if (Time.time < nextSpawnTime)
            return;

        GameObject carToSpawn = null;
        foreach (GameObject aiCar in carAIPool)
        {
            if (!aiCar.activeInHierarchy)
            {
                carToSpawn = aiCar;
                break;
            }
        }

        if (carToSpawn == null)
            return;

        float exactX = GetRandomSameLanePosition();

        float baseZ = playerCarTransform.position.z + spawnDistance;
        float bestZ = baseZ;
        bool foundSpot = false;

        for (float offset = 0; offset <= 40; offset += 5)
        {
            float testZ = baseZ + offset;

            if (IsSafeDistance(testZ) && IsSafeDistanceFromPlayer(testZ))
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

        if (Physics.OverlapBoxNonAlloc(spawnPosition, Vector3.one * 2, overlappedCheckCollider, Quaternion.identity, otherCarsLayerMask) > 0)
            return;

        carToSpawn.transform.position = spawnPosition;
        carToSpawn.transform.rotation = Quaternion.identity;
        carToSpawn.SetActive(true);

        nextSpawnTime = Time.time + Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    void CleanUpCarsBeyondView()
    {
        foreach (GameObject aiCar in carAIPool)
        {
            if (!aiCar.activeInHierarchy)
                continue;

            float distanceFromPlayer = aiCar.transform.position.z - playerCarTransform.position.z;

            if (distanceFromPlayer > despawnAheadDistance || distanceFromPlayer < -despawnBehindDistance)
            {
                aiCar.SetActive(false);
            }
        }
    }
}