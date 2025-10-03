using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }

    public enum State { Playing, GameOver }
    public State Current { get; private set; } = State.Playing;

    [Header("Optional UI (drag a CanvasGroup for a Game Over panel)")]
    [SerializeField] private CanvasGroup gameOverUI;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        // Hide UI on start
        if (gameOverUI)
        {
            gameOverUI.alpha = 0f;
            gameOverUI.interactable = false;
            gameOverUI.blocksRaycasts = false;
        }
        Time.timeScale = 1f;
        Current = State.Playing;
    }

    public void Death()
    {
        if (Current == State.GameOver) return;
        Current = State.GameOver;

        // Show panel if assigned, otherwise just reload the scene.
        if (gameOverUI)
        {
            gameOverUI.alpha = 1f;
            gameOverUI.interactable = true;
            gameOverUI.blocksRaycasts = true;
            Time.timeScale = 0f; // pause game
        }
        else
        {
            // Fallback behavior: immediate restart
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    // Hook these to UI buttons if you use the optional panel
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitToDesktop()
    {
        Time.timeScale = 1f;
        Application.Quit();
    }
}
