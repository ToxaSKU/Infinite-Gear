using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIHandler : MonoBehaviour
{
    [SerializeField] CarHandler carHandler;

    [Header("Speed Settings")]
    [SerializeField] float fixedSpeed = 10f;           // Фиксированная скорость для всех AI
    [SerializeField] bool useFixedSpeed = true;        // Включить фиксированную скорость

    [Header("Player Detection (для обгонов и очков)")]
    [SerializeField] float checkInterval = 0.2f;

    private float currentSpeed = 10f;
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

        if (useFixedSpeed && carHandler != null)
        {
            carHandler.SetMaxSpeed(fixedSpeed);
        }

        currentSpeed = fixedSpeed;
    }

    private void OnDisable()
    {
        allAICars.Remove(this);
    }

    void Start()
    {
        // Запускаем только проверку для обгонов (если нужно)
        StartCoroutine(CheckForOvertakeRoutine());
    }

    IEnumerator CheckForOvertakeRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(checkInterval);
        while (true)
        {
            yield return wait;
            // Здесь можно проверять обгон игроком (для ScoreManager)
            CheckIfPlayerIsOvertaking();
        }
    }

    void CheckIfPlayerIsOvertaking()
    {
        // Эта проверка нужна только для начисления очков за обгон
        // Не влияет на скорость AI
    }

    void Update()
    {
        // Постоянно едем с фиксированной скоростью
        if (carHandler != null)
            carHandler.SetInput(new Vector2(0f, 1f)); // Всегда полный газ
    }

    // Метод для изменения скорости из DifficultyManager
    public void SetSpeed(float newSpeed)
    {
        fixedSpeed = newSpeed;
        if (useFixedSpeed && carHandler != null)
        {
            carHandler.SetMaxSpeed(fixedSpeed);
        }
    }

    public float GetCurrentSpeed()
    {
        return carHandler != null ? carHandler.GetCurrentSpeed() : fixedSpeed;
    }
}