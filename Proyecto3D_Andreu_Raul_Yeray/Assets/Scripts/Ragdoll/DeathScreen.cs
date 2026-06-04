using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// Pantalla de muerte. Se suscribe al evento OnPlayerDied del PlayerStateMachine
/// y muestra una transición de fade a negro, mensaje y botones de reintentar / menú.
/// </summary>
public class DeathScreen : MonoBehaviour
{
    [Header("Referencias UI")]
    [Tooltip("Image negro a pantalla completa, alpha 0 al inicio")]
    public Image fadeImage;
    [Tooltip("Texto 'HAS MUERTO'")]
    public TextMeshProUGUI deathText;
    [Tooltip("Botón de reintentar (recarga escena actual)")]
    public Button retryButton;
    [Tooltip("Botón para ir al menú principal")]
    public Button menuButton;
    [Tooltip("CanvasGroup que contiene el texto y los botones (para fade in)")]
    public CanvasGroup deathUIGroup;

    [Header("Escenas")]
    [Tooltip("Nombre de la escena del menú principal")]
    public string menuSceneName = "Menu";

    [Header("Tiempos")]
    public float fadeDuration = 2.5f;
    public float showUIDelay = 0.5f;
    public float uiFadeDuration = 1f;

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
            PlayerStateMachine.Instance.OnPlayerDied += ShowDeathScreen;
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
        Color endColor   = new Color(0f, 0f, 0f, 1f);

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

    // ─── Botones ─────────────────────────────────────────────────────────────

    private void OnRetryClicked()
    {
        Debug.Log("[DeathScreen] Reintentar — recargando escena.");
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnMenuClicked()
    {
        Debug.Log($"[DeathScreen] Volviendo al menú: {menuSceneName}");
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }
}