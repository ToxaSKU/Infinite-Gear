using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Endless : MonoBehaviour
{
    // ========== НАСТРОЙКИ В ИНСПЕКТОРЕ ==========

    [SerializeField]
    GameObject[] sectionsPrefabs; // Массив префабов секций дороги (разные варианты)

    // ========== ПРИВАТНЫЕ ПЕРЕМЕННЫЕ ==========

    GameObject[] sectionsPool = new GameObject[20]; // Пул секций (всего 20)
    GameObject[] sections = new GameObject[10]; // Активные секции (10 штук перед игроком)

    Transform playerCarTransform; // Ссылка на трансформ машины игрока

    WaitForSeconds waitFor100ms = new WaitForSeconds(0.1f); // Задержка 0.1 сек для корутины

    const float sectionLength = 27; // Длина одной секции дороги (по оси Z)



    void Start()
    {
        // Находим машину игрока по тегу
        playerCarTransform = GameObject.FindGameObjectWithTag("Player").transform;

        int prefabIndex = 0;


        // Создаём 20 секций разных типов и прячем их (SetActive false)
        for (int i = 0; i < sectionsPool.Length; i++)
        {
            sectionsPool[i] = Instantiate(sectionsPrefabs[prefabIndex]);
            sectionsPool[i].SetActive(false); // Изначально выключена

            prefabIndex++;

            // Если закончились префабы - начинаем сначала
            if (prefabIndex > sectionsPrefabs.Length - 1)
                prefabIndex = 0;
        }

        // Создаём 10 активных секций, расположенных друг за другом
        for (int i = 0; i < sections.Length; i++)
        {
            // Берём случайную свободную секцию из пула
            GameObject randomSection = GetRandomSectionFromPool();

            // Размещаем её: X берём из пула, Y = -10 (ниже камеры), Z = i * длина секции
            randomSection.transform.position = new Vector3(sectionsPool[i].transform.position.x, -10, i * sectionLength);
            randomSection.SetActive(true); // Включаем секцию

            // Сохраняем в массив активных секций
            sections[i] = randomSection;
        }

        // Запускаем корутину обновления позиций секций
        StartCoroutine(UpdateLessonOfTenCO());
    }

    // Корутина: каждые 0.1 секунды проверяем позиции секций
    IEnumerator UpdateLessonOfTenCO()
    {
        while (true)
        {
            UpdateSectionPositions();
            yield return waitFor100ms;
        }
    }

    // Обновление позиций секций (бесконечная дорога)
    void UpdateSectionPositions()
    {
        for (int i = 0; i < sections.Length; i++)
        {
            if (sections[i].transform.position.z - playerCarTransform.position.z < -sectionLength)
            {
                // Запоминаем позицию старой секции
                Vector3 lastSectionPosition = sections[i].transform.position;

                // Выключаем старую секцию
                sections[i].SetActive(false);

                // Берём новую случайную секцию из пула
                sections[i] = GetRandomSectionFromPool();

                // Перемещаем новую секцию в конец очереди (за последнюю активную секцию)
                sections[i].transform.position = new Vector3(
                    lastSectionPosition.x,
                    -10,
                    lastSectionPosition.z + sectionLength * sections.Length
                );

                // Включаем новую секцию
                sections[i].SetActive(true);
            }
        }
    }

    // Получение свободной (неактивной) секции из пула
    GameObject GetRandomSectionFromPool()
    {
        // Выбираем случайный индекс в пуле
        int randomIndex = Random.Range(0, sectionsPool.Length);

        bool isNewSectionFound = false;

        // Ищем свободную секцию (которая не активна на сцене)
        while (!isNewSectionFound)
        {
            // Если секция не активна - нашли
            if (!sectionsPool[randomIndex].activeInHierarchy)
                isNewSectionFound = true;
            else
            {
                // Если секция активна - переходим к следующей
                randomIndex++;

                // Зацикливаем индекс (если дошли до конца - идём сначала)
                if (randomIndex > sectionsPool.Length - 1)
                    randomIndex = 0;
            }
        }

        return sectionsPool[randomIndex];
    }
}