using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// GameManager — singleton global.
/// Gestiona: Game Over, checkpoints, carga de escena.
///
/// Coloca en un GameObject vacío llamado "GameManager" en la escena.
/// Marca "Don't Destroy On Load" para que persista entre escenas.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Checkpoint")]
    [Tooltip("Posición del último checkpoint activado")]
    public Vector3 lastCheckpointPosition = Vector3.zero;
    [Tooltip("Rotación del último checkpoint")]
    public Quaternion lastCheckpointRotation = Quaternion.identity;
    private bool _hasCheckpoint = false;

    [Header("UI — Game Over")]
    [Tooltip("Panel de Game Over en el Canvas (asignar en Inspector)")]
    public GameObject gameOverPanel;

    [Header("Configuración")]
    [Tooltip("Segundos de espera antes de mostrar el panel de Game Over")]
    public float gameOverDelay = 1.5f;

    // Eventos
    public System.Action OnGameOver;
    public System.Action OnRestart;

    // ─── Init ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void Start()
    {
        // Suscribirse a la muerte del jugador
        if (PlayerStateMachine.Instance != null)
            PlayerStateMachine.Instance.OnPlayerDied += HandlePlayerDied;
    }

    private void OnDestroy()
    {
        if (PlayerStateMachine.Instance != null)
            PlayerStateMachine.Instance.OnPlayerDied -= HandlePlayerDied;
    }

    // ─── Game Over ────────────────────────────────────────────────────────────

    private void HandlePlayerDied()
    {
        Debug.Log("[GameManager] Jugador muerto — iniciando Game Over.");
        Invoke(nameof(ShowGameOver), gameOverDelay);
    }

    private void ShowGameOver()
    {
        OnGameOver?.Invoke();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        // Pausar el juego
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        Debug.Log("[GameManager] GAME OVER.");
    }

    // ─── Botones UI ───────────────────────────────────────────────────────────

    /// <summary>Llamado por el botón "Reintentar" del panel de Game Over.</summary>
    public void OnRetryButton()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        if (_hasCheckpoint)
        {
            // Recargar escena y restaurar al checkpoint
            StartCoroutine(LoadFromCheckpoint());
        }
        else
        {
            // Sin checkpoint — recargar escena desde el inicio
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        OnRestart?.Invoke();
    }

    /// <summary>Llamado por el botón "Menú Principal".</summary>
    public void OnMainMenuButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    private System.Collections.IEnumerator LoadFromCheckpoint()
    {
        // Recargar la escena
        AsyncOperation load = SceneManager.LoadSceneAsync(
            SceneManager.GetActiveScene().buildIndex);

        yield return load;

        // Mover al jugador al checkpoint tras cargar
        // El CheckpointSystem se encargará de esto cuando esté implementado
        Debug.Log($"[GameManager] Checkpoint restaurado en {lastCheckpointPosition}");
    }

    // ─── Checkpoints ─────────────────────────────────────────────────────────

    /// <summary>Registra un nuevo checkpoint.</summary>
    public void RegisterCheckpoint(Vector3 position, Quaternion rotation)
    {
        lastCheckpointPosition = position;
        lastCheckpointRotation = rotation;
        _hasCheckpoint         = true;
        Debug.Log($"[GameManager] Checkpoint guardado en {position}");
    }

    // ─── Utilidades ──────────────────────────────────────────────────────────

    public void PauseGame()
    {
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }
}
