using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// Pantalla de muerte con sistema de respawn.
/// - Botón Reintentar: respawnea al jugador en el punto inicial SIN recargar la escena
/// - Botón Menú: carga la escena del menú principal
/// </summary>
public class DeathScreen : MonoBehaviour
{
    [Header("Referencias UI")]
    public Image fadeImage;
    public TextMeshProUGUI deathText;
    public Button retryButton;
    public Button menuButton;
    public CanvasGroup deathUIGroup;

    [Header("Spawn point del jugador")]
    [Tooltip("Transform donde reaparecerá el jugador al pulsar Reintentar. Si está vacío, usa la posición inicial guardada al empezar.")]
    public Transform spawnPoint;
    [Tooltip("Si está marcado, guarda automáticamente la posición inicial del jugador al empezar la escena")]
    public bool autoUseInitialPosition = true;

    [Header("Escenas")]
    public string menuSceneName = "Menu";

    [Header("Tiempos")]
    public float fadeDuration = 2.5f;
    public float showUIDelay = 0.5f;
    public float uiFadeDuration = 1f;

    // Posición/rotación guardadas al inicio
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;
    private bool _initialPositionSaved = false;

    private void Awake()
    {
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.gameObject.SetActive(true);
            fadeImage.raycastTarget = false;
        }

        if (deathUIGroup != null)
        {
            deathUIGroup.alpha = 0f;
            deathUIGroup.interactable = false;
            deathUIGroup.blocksRaycasts = false;
        }

        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetryClicked);

        if (menuButton != null)
            menuButton.onClick.AddListener(OnMenuClicked);
    }

    private void Start()
    {
        if (PlayerStateMachine.Instance != null)
        {
            PlayerStateMachine.Instance.OnPlayerDied += ShowDeathScreen;

            // Guardar la posición inicial del jugador
            if (autoUseInitialPosition)
            {
                _initialPosition = PlayerStateMachine.Instance.transform.position;
                _initialRotation = PlayerStateMachine.Instance.transform.rotation;
                _initialPositionSaved = true;
            }
        }
    }

    private void OnDestroy()
    {
        if (PlayerStateMachine.Instance != null)
            PlayerStateMachine.Instance.OnPlayerDied -= ShowDeathScreen;

        if (retryButton != null)
            retryButton.onClick.RemoveListener(OnRetryClicked);

        if (menuButton != null)
            menuButton.onClick.RemoveListener(OnMenuClicked);
    }

    // ─── Mostrar pantalla de muerte ──────────────────────────────────────────

    public void ShowDeathScreen()
    {
        StartCoroutine(FadeAndShow());
    }

    private IEnumerator FadeAndShow()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        float t = 0f;
        Color startColor = fadeImage != null ? fadeImage.color : Color.black;
        Color endColor = new Color(0f, 0f, 0f, 1f);

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(t / fadeDuration);
            if (fadeImage != null)
                fadeImage.color = Color.Lerp(startColor, endColor, progress);
            yield return null;
        }

        if (fadeImage != null)
        {
            fadeImage.color = endColor;
            fadeImage.raycastTarget = true;
        }

        yield return new WaitForSecondsRealtime(showUIDelay);

        if (deathUIGroup != null)
        {
            t = 0f;
            while (t < uiFadeDuration)
            {
                t += Time.unscaledDeltaTime;
                deathUIGroup.alpha = Mathf.Clamp01(t / uiFadeDuration);
                yield return null;
            }
            deathUIGroup.alpha = 1f;
            deathUIGroup.interactable = true;
            deathUIGroup.blocksRaycasts = true;
        }
    }

    // ─── Ocultar pantalla de muerte (al respawnear) ──────────────────────────

    private IEnumerator HideDeathScreen()
    {
        // Ocultar UI rápido
        if (deathUIGroup != null)
        {
            deathUIGroup.alpha = 0f;
            deathUIGroup.interactable = false;
            deathUIGroup.blocksRaycasts = false;
        }

        // Fade out del negro
        float t = 0f;
        float duration = 0.5f;
        Color startColor = fadeImage != null ? fadeImage.color : Color.black;
        Color endColor = new Color(0f, 0f, 0f, 0f);

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(t / duration);
            if (fadeImage != null)
                fadeImage.color = Color.Lerp(startColor, endColor, progress);
            yield return null;
        }

        if (fadeImage != null)
        {
            fadeImage.color = endColor;
            fadeImage.raycastTarget = false;
        }

        // Bloquear cursor para volver a jugar
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // ─── Botones ─────────────────────────────────────────────────────────────

    private void OnRetryClicked()
    {
        Debug.Log("[DeathScreen] Reintentar — respawneando jugador.");
        Time.timeScale = 1f;

        if (PlayerStateMachine.Instance != null)
        {
            Vector3 pos;
            Quaternion rot;

            if (spawnPoint != null)
            {
                pos = spawnPoint.position;
                rot = spawnPoint.rotation;
            }
            else if (_initialPositionSaved)
            {
                pos = _initialPosition;
                rot = _initialRotation;
            }
            else
            {
                Debug.LogWarning("[DeathScreen] No hay spawn point ni posición inicial guardada. Recargando escena.");
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                return;
            }

            PlayerStateMachine.Instance.Respawn(pos, rot);
            StartCoroutine(HideDeathScreen());
        }
        else
        {
            // Fallback: si no hay PlayerStateMachine, recarga la escena
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void OnMenuClicked()
    {
        Debug.Log($"[DeathScreen] Volviendo al menú: {menuSceneName}");
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }
}