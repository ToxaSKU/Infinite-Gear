using UnityEngine;
using TMPro; // Добавьте эту строку для TextMeshPro

public class ScoreManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] TextMeshProUGUI distanceText;  // Изменено с Text на TextMeshProUGUI
    [SerializeField] TextMeshProUGUI scoreText;     // Изменено с Text на TextMeshProUGUI
    [SerializeField] TextMeshProUGUI overtakeText;  // Изменено с Text на TextMeshProUGUI
    [SerializeField] TextMeshProUGUI finalScoreText; // Изменено с Text на TextMeshProUGUI

    [Header("Score Settings")]
    [SerializeField] float pointsPerMeter = 10f;
    [SerializeField] int pointsPerOvertake = 100;
    [SerializeField] float comboTimeWindow = 3f;
    [SerializeField] int comboMultiplier = 2;

    [Header("Distance Settings")]
    [SerializeField] float distanceMultiplier = 1f;

    private float totalScore = 0;
    private float totalDistance = 0;
    private int overtakeCount = 0;
    private int currentCombo = 0;
    private float lastOvertakeTime = 0;
    private Vector3 lastPosition;
    private bool isGameActive = true;

    void Start()
    {
        lastPosition = transform.position;
        UpdateUI();
    }

    void Update()
    {
        if (!isGameActive) return;

        float distanceThisFrame = Vector3.Distance(transform.position, lastPosition);
        totalDistance += distanceThisFrame;
        lastPosition = transform.position;

        float distanceScore = distanceThisFrame * pointsPerMeter * distanceMultiplier;
        totalScore += distanceScore;

        UpdateUI();
    }

    public void AddOvertake()
    {
        if (!isGameActive) return;

        overtakeCount++;

        float timeSinceLast = Time.time - lastOvertakeTime;
        if (timeSinceLast < comboTimeWindow)
        {
            currentCombo++;
            int comboPoints = pointsPerOvertake * Mathf.Min(currentCombo, comboMultiplier);
            totalScore += comboPoints;
        }
        else
        {
            currentCombo = 1;
            totalScore += pointsPerOvertake;
        }

        lastOvertakeTime = Time.time;
        ShowOvertakeEffect();
        UpdateUI();
    }

    void ShowOvertakeEffect()
    {
        if (overtakeText != null)
        {
            if (currentCombo > 1)
                overtakeText.text = $"OVERTAKE! +{pointsPerOvertake} x{currentCombo} COMBO!";
            else
                overtakeText.text = $"OVERTAKE! +{pointsPerOvertake}";

            Invoke(nameof(ClearOvertakeText), 1f);
        }
    }

    void ClearOvertakeText()
    {
        if (overtakeText != null)
            overtakeText.text = "";
    }

    void UpdateUI()
    {
        if (distanceText != null)
            distanceText.text = $"Distance: {totalDistance:F0}m";

        if (scoreText != null)
            scoreText.text = $"Score: {totalScore:F0}";

        if (overtakeText != null && currentCombo > 1 && overtakeText.text == "")
            overtakeText.text = $"Overtakes: {overtakeCount} | x{currentCombo} COMBO!";
        else if (overtakeText != null && overtakeText.text == "")
            overtakeText.text = $"Overtakes: {overtakeCount}";
    }

    public void GameOver()
    {
        isGameActive = false;

        if (finalScoreText != null)
            finalScoreText.text = $"Final Score: {totalScore:F0}\nDistance: {totalDistance:F0}m\nOvertakes: {overtakeCount}";
    }

    public float GetTotalScore()
    {
        return totalScore;
    }

    public float GetTotalDistance()
    {
        return totalDistance;
    }

    public int GetOvertakeCount()
    {
        return overtakeCount;
    }
}