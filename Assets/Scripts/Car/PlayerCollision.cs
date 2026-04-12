using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerCollision : MonoBehaviour
{
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] float restartDelay = 2f;
    private bool isGameOver = false;

    void Start()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false); // Скрываем панель в начале
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isGameOver) return;
        if (collision.gameObject.GetComponent<AIHandler>() != null)
        {
            GameOver();
        }
    }

    void GameOver()
    {
        isGameOver = true;
        Time.timeScale = 0f; // Останавливаем игру
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true); // Показываем панель
        Invoke(nameof(RestartGame), restartDelay); // Через 2 секунды рестарт
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}