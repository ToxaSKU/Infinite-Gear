using System.Collections;
using UnityEngine;

public class OppositeTraffic : MonoBehaviour
{
    [SerializeField]
    GameObject[] oppositeCarPrefabs; // Префабы машин для встречки

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
    float minSpawnInterval = 2f;

    [SerializeField]
    float maxSpawnInterval = 5f;

    [SerializeField]
    float oppositeLaneOffset = -5f; // Смещение влево (отрицательное по X)

    float nextSpawnTime = 0;

    void Start()
    {
        playerCarTransform = GameObject.FindGameObjectWithTag("Player").transform;

        // Создаём пул машин для встречки
        int prefabIndex = 0;
        for (int i = 0; i < oppositeCarPool.Length; i++)
        {
            oppositeCarPool[i] = Instantiate(oppositeCarPrefabs[prefabIndex]);
            oppositeCarPool[i].SetActive(false);

            prefabIndex++;
            if (prefabIndex >= oppositeCarPrefabs.Length)
                prefabIndex = 0;
        }

        StartCoroutine(SpawnOppositeTraffic());
    }

    IEnumerator SpawnOppositeTraffic()
    {
        while (true)
        {
            SpawnOppositeCar();
            CleanupOppositeCars();
            yield return new WaitForSeconds(0.3f);
        }
    }

    void SpawnOppositeCar()
    {
        if (Time.time < nextSpawnTime)
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

        // Позиция спавна на встречной полосе (слева от игрока)
        Vector3 spawnPosition = new Vector3(
            playerCarTransform.position.x + oppositeLaneOffset,
            playerCarTransform.position.y,
            playerCarTransform.position.z + spawnDistance
        );

        // Проверяем, нет ли другой машины на этом месте
        if (Physics.OverlapBoxNonAlloc(spawnPosition, Vector3.one * 2.5f, overlappedCheckCollider, Quaternion.identity, trafficLayerMask) > 0)
            return;

        carToSpawn.transform.position = spawnPosition;

        // Разворачиваем машину в противоположную сторону
        carToSpawn.transform.rotation = Quaternion.Euler(0, 180, 0);

        carToSpawn.SetActive(true);

        nextSpawnTime = Time.time + Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    void CleanupOppositeCars()
    {
        foreach (GameObject car in oppositeCarPool)
        {
            if (!car.activeInHierarchy)
                continue;

            float distanceFromPlayer = car.transform.position.z - playerCarTransform.position.z;

            // Удаляем машины, которые уехали далеко вперёд или назад
            if (Mathf.Abs(distanceFromPlayer) > despawnDistance)
            {
                car.SetActive(false);
            }
        }
    }
}