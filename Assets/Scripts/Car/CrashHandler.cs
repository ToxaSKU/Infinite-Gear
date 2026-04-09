using UnityEngine;

public class CrashHandler : MonoBehaviour
{
    //[SerializeField]
    //private string otherCarTag = "CarAI"; // Тег других машин

    //[SerializeField]
    //private bool showCrashMessage = true; // Показывать сообщение о краше

    //[SerializeField]
    //private float stopDelay = 0.1f; // Небольшая задержка перед остановкой

    //private bool hasCrashed = false; // Флаг, чтобы краш сработал только один раз

    //private void OnCollisionEnter(Collision collision)
    //{
    //    // Проверяем, что столкновение с другой машиной и краш ещё не произошёл
    //    if (!hasCrashed && collision.gameObject.CompareTag(otherCarTag))
    //    {
    //        hasCrashed = true;

    //        // Останавливаем время игры
    //        Time.timeScale = 0f;

    //        // Показываем сообщение в консоли
    //        if (showCrashMessage)
    //            Debug.Log("Авария! Игра остановлена.");
    //    }
    //}

    //// Опционально: метод для возобновления игры (например, по кнопке Restart)
    //public void ResumeGame()
    //{
    //    Time.timeScale = 1f;
    //    hasCrashed = false;
    //}
}