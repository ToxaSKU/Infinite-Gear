using UnityEngine;
using UnityEngine.UI; // если используешь UI для очков

public class CarController : MonoBehaviour
{
    [Header("Компоненты")]
    [SerializeField] Rigidbody rb;
    [SerializeField] Transform gameModel;

    [Header("Настройки полос")]
    [SerializeField] float[] lanePositions = { -2.5f, 0f, 2.5f };
    [SerializeField] float laneSwitchSpeed = 12f;

    [Header("Скорость")]
    [SerializeField] float maxSpeed = 35f;
    [SerializeField] float acceleration = 8f;
    [SerializeField] float startSpeed = 15f;

    [Header("Поворот модели")]
    [SerializeField] float tiltAmount = 15f;
    [SerializeField] float tiltSmoothness = 5f;

    [Header("Система очков")]
    [SerializeField] Text scoreText; // перетащи Text из UI
    [SerializeField] Text comboText;

    private int currentLane = 1;
    private Vector3 targetPosition;
    private float currentSpeed;
    private int score = 0;
    private int combo = 0;
    private float lastZPosition;
    private float targetTilt = 0f;
    private float currentTilt = 0f;

    void Start()
    {
        targetPosition = transform.position;
        lastZPosition = transform.position.z;
        currentSpeed = startSpeed;

        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.velocity = Vector3.forward * currentSpeed;

        // Замораживаем вращение по X и Z
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        // Управление полосами
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            SwitchLane(-1);
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            SwitchLane(1);

        // Плавное перемещение между полосами
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * laneSwitchSpeed);

        // Автоматическое ускорение (как в Traffic Racer)
        if (currentSpeed < maxSpeed)
        {
            currentSpeed += acceleration * Time.deltaTime;
            rb.velocity = Vector3.forward * currentSpeed;
        }

        // Наклон модели при смене полосы
        float horizontalDelta = (targetPosition.x - transform.position.x) * 5f;
        targetTilt = Mathf.Clamp(-horizontalDelta, -tiltAmount, tiltAmount);
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSmoothness);
        gameModel.localRotation = Quaternion.Euler(0, 0, currentTilt);

        // Подсчёт очков за дистанцию
        float distanceDriven = transform.position.z - lastZPosition;
        if (distanceDriven > 0)
        {
            score += Mathf.FloorToInt(distanceDriven * 10);
            lastZPosition = transform.position.z;
            UpdateUI();
        }
    }

    void SwitchLane(int direction)
    {
        int newLane = currentLane + direction;
        if (newLane >= 0 && newLane < lanePositions.Length)
        {
            currentLane = newLane;
            targetPosition.x = lanePositions[currentLane];
        }
    }

    public void AddScore(int points)
    {
        combo++;
        int bonus = points * combo;
        score += bonus;
        UpdateUI();

        // Визуальный эффект комбо
        if (comboText != null)
        {
            comboText.text = $"COMBO x{combo}! +{bonus}";
            comboText.gameObject.SetActive(true);
            Invoke(nameof(HideCombo), 1f);
        }
    }

    void HideCombo()
    {
        if (comboText != null)
            comboText.gameObject.SetActive(false);
    }

    public void ResetCombo()
    {
        combo = 0;
    }

    void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"SCORE: {score}";
    }

    public float GetSpeed()
    {
        return currentSpeed;
    }

    public int GetScore()
    {
        return score;
    }
}