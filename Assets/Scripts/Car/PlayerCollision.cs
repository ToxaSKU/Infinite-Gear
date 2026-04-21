using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerCollision : MonoBehaviour
{
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] float restartDelay = 2f;
    private bool isGameOver = false;
    private ScoreManager scoreManager;

    void Start()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        scoreManager = FindObjectOfType<ScoreManager>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isGameOver) return;
        if (collision.gameObject.GetComponent<AIHandler>() != null)
        {
            GameOver();
        }
        Debug.Log($"Collision with: {collision.gameObject.name}, Tag: {collision.gameObject.tag}");

        if (isGameOver) return;

        AIHandler ai = collision.gameObject.GetComponent<AIHandler>();
        if (ai != null)
        {
            Debug.Log("AI detected! Game Over!");
            GameOver();
        }
    }

    void GameOver()
    {
        isGameOver = true;

        // Останавливаем звук
        AudioSource[] audioSources = GetComponents<AudioSource>();
        foreach (AudioSource audio in audioSources)
            audio.Stop();

        // Вызываем GameOver у ScoreManager
        if (scoreManager != null)
            scoreManager.GameOver();

        Time.timeScale = 0f;
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        Invoke(nameof(RestartGame), restartDelay);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

}