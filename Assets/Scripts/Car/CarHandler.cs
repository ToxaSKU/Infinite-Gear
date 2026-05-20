using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarHandler : MonoBehaviour
{
    // ========== КОМПОНЕНТЫ ==========

    [SerializeField]
    Rigidbody rb; // Ссылка на компонент Rigidbody (физика машины)

    [SerializeField]
    Transform gameModel; // Ссылка на трансформ 3D модели машины (для поворота)

    // ========== НАСТРОЙКИ ЗВУКА ==========

    [Header("SFX")]
    [SerializeField]
    AudioSource carEngineAS; // Аудио источник для звука двигателя

    [SerializeField]
    AnimationCurve carPitchAnimationCurve; // Кривая изменения тона звука от скорости

    // ========== НАСТРОЙКИ АВАРИИ ==========

    [Header("Crash Settings")]
    [SerializeField]
    float crashSpeedThreshold = 3f; // Порог скорости столкновения для аварии (м/с)
    [SerializeField]
    float crashAudioFadeTime = 0.5f; // Время затухания звука при аварии

    // ========== ПРОГРЕССИЯ СКОРОСТИ ==========

    [Header("Speed Progression")]
    [SerializeField] bool useSpeedProgression = true; // Включить постепенное увеличение макс.скорости
    [SerializeField] float startMaxSpeed = 15f; // Начальная максимальная скорость
    [SerializeField] float finalMaxSpeed = 35f; // Конечная максимальная скорость (через timeToReachMaxSpeed)
    [SerializeField] float timeToReachMaxSpeed = 120f; // Время (сек) для достижения finalMaxSpeed
    [SerializeField] AnimationCurve speedProgressionCurve = AnimationCurve.Linear(0, 0, 1, 1); // Кривая прогрессии

    // ========== МИНИМАЛЬНАЯ СКОРОСТЬ (защита от застревания) ==========

    [Header("Minimum Speed (Anti-Stuck)")]
    [SerializeField] bool enableMinSpeed = true; // Включить минимальную скорость
    [SerializeField] float minSpeed = 12f; // Минимальная скорость (чтобы машина не останавливалась)
    [SerializeField] bool increaseMinSpeedOverTime = true; // Увеличивать ли минимальную скорость со временем
    [SerializeField] float finalMinSpeed = 25f; // Конечная минимальная скорость

    // ========== БАЗОВЫЕ НАСТРОЙКИ УПРАВЛЕНИЯ ==========

    // Максимальные значения
    float maxSteerVelocity = 12; // Максимальная боковая скорость (поворот)
    float maxForwardVelocity = 20; // Максимальная скорость вперёд (переопределяется прогрессией)

    // Множители (чувствительность)
    float accelerationMultiplier = 5; // Сила ускорения
    float breakstionMultiplier = 15; // Сила торможения
    float steeringMultiplier = 10; // Сила поворота

    // ========== ПРИВАТНЫЕ ПЕРЕМЕННЫЕ ==========

    Vector2 input = Vector2.zero; // Ввод игрока (x = руль, y = газ/тормоз)

    bool isPlayer = true; // Является ли машина игроком (для AI своя логика)
    private bool audioInitialized = false; // Инициализирован ли звук
    private bool isCrashed = false; // В аварии ли машина
    private float originalVolume; // Оригинальная громкость звука

    private float gameStartTime; // Время старта игры (для прогрессии)
    private float currentMaxSpeed; // Текущая максимальная скорость
    private float currentMinSpeed; // Текущая минимальная скорость

    // ========== МЕТОДЫ ==========

    void Start()
    {
        // Сбрасываем поворот модели
        gameModel.transform.localRotation = Quaternion.identity;

        // Проверяем, игрок ли это (по тегу)
        isPlayer = CompareTag("Player");

        if (isPlayer)
        {
            InitCarAudio(); // Инициализация звука двигателя
            gameStartTime = Time.time; // Запоминаем время старта
            UpdateMaxSpeed(); // Вычисляем начальную максимальную скорость
            currentMinSpeed = minSpeed; // Устанавливаем минимальную скорость
        }
    }

    void Update()
    {
        // Поворот модели машины в зависимости от боковой скорости (эффект наклона)
        gameModel.transform.rotation = Quaternion.Euler(0, rb.velocity.x * 5, 0);

        // Если машина в аварии т - останавливаем
        if (isCrashed && carEngineAS != null && carEngineAS.isPlaying)
        {
            carEngineAS.Stop();
        }

        // Прогрессия скорости (только для игрока, не в аварии)
        if (isPlayer && useSpeedProgression && !isCrashed)
        {
            UpdateMaxSpeed(); // Обновляем максимальную скорость

            // Увеличиваем минимальную скорость со временем
            if (increaseMinSpeedOverTime)
            {
                float elapsedTime = Time.time - gameStartTime;
                float t = Mathf.Clamp01(elapsedTime / timeToReachMaxSpeed);
                currentMinSpeed = Mathf.Lerp(minSpeed, finalMinSpeed, t);
            }
        }

        UpdateCarAudio(); // Обновляем звук двигателя (тон от скорости)
    }

    // Обновление максимальной скорости в зависимости от времени
    void UpdateMaxSpeed()
    {
        float elapsedTime = Time.time - gameStartTime;
        float t = Mathf.Clamp01(elapsedTime / timeToReachMaxSpeed); // 0..1
        float curveValue = speedProgressionCurve.Evaluate(t); // Значение с кривой
        currentMaxSpeed = Mathf.Lerp(startMaxSpeed, finalMaxSpeed, curveValue);
        maxForwardVelocity = currentMaxSpeed;
    }

    // Инициализация звука двигателя
    void InitCarAudio()
    {
        if (carEngineAS == null)
        {
            carEngineAS = GetComponent<AudioSource>();
            if (carEngineAS == null)
                return;
        }

        if (carEngineAS.clip == null)
            return;

        carEngineAS.loop = true; // Зацикливаем звук
        carEngineAS.playOnAwake = false; // Не играть автоматически
        originalVolume = 0.6f;
        carEngineAS.volume = originalVolume;

        carEngineAS.Play();
        audioInitialized = true;
    }

    // Физика (вызывается фиксированное количество раз в секунду)
    private void FixedUpdate()
    {
        if (isCrashed)
            return; // Если в аварии - не двигаемся

        // Поддержание минимальной скорости (чтобы не застревать)
        if (isPlayer && enableMinSpeed && rb.velocity.z < currentMinSpeed)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0, currentMinSpeed);
        }

        // Ускорение (W или стрелка вверх)
        if (input.y > 0)
        {
            Accelerate();
        }
        else
            rb.drag = 0.2f; // Сопротивление при отпускании газа

        // Торможение (S или стрелка вниз)
        if (input.y < 0)
        {
            Brake();
        }

        Steer(); // Поворот

        // Запрещаем движение назад (отрицательная скорость)
        if (rb.velocity.z < 0)
            rb.velocity = new Vector3(rb.velocity.x, 0, 0);
    }

    // Ускорение
    void Accelerate()
    {
        rb.drag = 0; // Убираем сопротивление

        // Ограничение максимальной скорости
        if (rb.velocity.z >= maxForwardVelocity)
            return;

        // Применяем силу вперёд
        rb.AddForce(rb.transform.forward * accelerationMultiplier * input.y);
    }

    // Торможение
    void Brake()
    {
        // Не тормозим, если скорость уже ниже минимальной
        if (isPlayer && enableMinSpeed && rb.velocity.z <= currentMinSpeed)
            return;

        // Не тормозим, если уже стоим
        if (rb.velocity.z <= 0)
            return;

        // Применяем силу назад
        rb.AddForce(rb.transform.forward * breakstionMultiplier * input.y);
    }

    // Поворот (руление)
    void Steer()
    {
        if (Mathf.Abs(input.x) > 0) // Есть ввод поворота
        {
            // Ограничение поворота в зависимости от скорости
            float speedBaseSteerLimit = rb.velocity.z / 5.0f;
            speedBaseSteerLimit = Mathf.Clamp01(speedBaseSteerLimit);

            // Применяем боковую силу (занос)
            rb.AddForce(rb.transform.right * steeringMultiplier * input.x * speedBaseSteerLimit);

            // Нормализуем боковую скорость и ограничиваем
            float normalizedX = rb.velocity.x / maxSteerVelocity;
            normalizedX = Mathf.Clamp(normalizedX, -1.0f, 1.0f);
            rb.velocity = new Vector3(normalizedX * maxSteerVelocity, 0, rb.velocity.z);
        }
        else
        {
            // Автоматическое выравнивание (когда отпустили руль)
            rb.velocity = Vector3.Lerp(rb.velocity, new Vector3(0, 0, rb.velocity.z), Time.fixedDeltaTime * 3);
        }
    }

    // Обновление звука двигателя (меняем тон в зависимости от скорости)
    void UpdateCarAudio()
    {
        if (!isPlayer)
            return;

        if (isCrashed)
        {
            if (carEngineAS != null && carEngineAS.isPlaying)
                carEngineAS.Stop();
            return;
        }

        if (!audioInitialized || carEngineAS == null)
            return;

        // Запускаем звук, если он не играет
        if (!carEngineAS.isPlaying && carEngineAS.clip != null)
            carEngineAS.Play();

        // Процент от максимальной скорости (0..1)
        float carMaxSpeedPercentage = Mathf.Clamp01(rb.velocity.z / maxForwardVelocity);

        // Вычисляем тон звука по кривой или по умолчанию
        if (carPitchAnimationCurve != null && carPitchAnimationCurve.keys.Length > 0)
            carEngineAS.pitch = carPitchAnimationCurve.Evaluate(carMaxSpeedPercentage);
        else
            carEngineAS.pitch = 0.7f + carMaxSpeedPercentage * 0.8f; // От 0.7 до 1.5
    }

    // Обработка столкновения
    private void OnCollisionEnter(Collision collision)
    {
        // Пропускаем триггеры
        if (collision.collider.isTrigger)
            return;

        // Пропускаем столкновение с самим собой
        if (collision.gameObject == gameObject)
            return;

        // Если уже в аварии - пропускаем
        if (isCrashed)
            return;

        // Игнорируем стены
        if (collision.gameObject.CompareTag("Wall"))
            return;

        // Игнорируем объекты с определёнными именами
        if (collision.gameObject.name.Contains("Cube") ||
            collision.gameObject.name.Contains("Wall") ||
            collision.gameObject.name.Contains("Barrier"))
            return;

        // Игнорируем дорогу
        if (collision.gameObject.CompareTag("Road"))
            return;

        // Проверяем скорость столкновения
        float crashSpeed = collision.relativeVelocity.magnitude;

        if (crashSpeed > crashSpeedThreshold)
            Crash(); // Авария!
    }

    // Авария
    void Crash()
    {
        if (isCrashed) return;

        isCrashed = true;

        // Останавливаем движение
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Останавливаем звук
        if (carEngineAS != null)
        {
            carEngineAS.Stop();
            carEngineAS.enabled = false;
            carEngineAS.volume = 0f;
        }

        // Отключаем скрипт (машина не двигается)
        this.enabled = false;
    }

    // ========== ПУБЛИЧНЫЕ МЕТОДЫ ==========

    // Восстановление машины после аварии
    public void RepairCar()
    {
        isCrashed = false;
        this.enabled = true;

        if (isPlayer && carEngineAS != null)
        {
            carEngineAS.enabled = true;
            carEngineAS.volume = originalVolume;
            carEngineAS.Play();
            audioInitialized = true;
        }
    }

    // Установка ввода (вызывается из InputHandler)
    public void SetInput(Vector2 inputVector)
    {
        inputVector.Normalize(); // Нормализуем (максимум 1)
        input = inputVector;
    }

    // Установка максимальной скорости (для AI)
    public void SetMaxSpeed(float speed)
    {
        maxForwardVelocity = speed;
    }

    // Получение максимальной скорости
    public float GetMaxSpeed()
    {
        return maxForwardVelocity;
    }

    // Получение текущей скорости
    public float GetCurrentSpeed()
    {
        return rb.velocity.z;
    }

    // Проверка, в аварии ли машина
    public bool IsCrashed()
    {
        return isCrashed;
    }

    // Остановка звука двигателя
    public void StopCarAudio()
    {
        if (carEngineAS != null)
        {
            carEngineAS.Stop();
            carEngineAS.enabled = false;
            carEngineAS.volume = 0f;
        }
    }
}