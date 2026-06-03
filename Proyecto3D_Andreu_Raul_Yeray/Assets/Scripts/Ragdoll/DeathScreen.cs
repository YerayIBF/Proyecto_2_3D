using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// Pantalla de muerte. Se suscribe al evento OnPlayerDied del PlayerStateMachine
/// y muestra una transición de fade a negro, mensaje y botón reintentar.
///
/// Setup:
/// - Coloca este script en un GameObject (puede ser un Canvas hijo, o en el PlayerArmature)
/// - Crea un Canvas con un Image negro a pantalla completa (alpha = 0)
/// - Añade un TextMeshProUGUI con "HAS MUERTO" (oculto al inicio)
/// - Añade un Button con texto "Reintentar" (oculto al inicio)
/// - Asigna todo en el Inspector
/// </summary>
public class DeathScreen : MonoBehaviour
{
    [Header("Referencias UI")]
    [Tooltip("Image negro a pantalla completa, alpha 0 al inicio")]
    public Image fadeImage;
    [Tooltip("Texto 'HAS MUERTO'")]
    public TextMeshProUGUI deathText;
    [Tooltip("Botón de reintentar")]
    public Button retryButton;
    [Tooltip("CanvasGroup que contiene el texto y el botón (para fade in)")]
    public CanvasGroup deathUIGroup;

    [Header("Tiempos")]
    [Tooltip("Tiempo que tarda en oscurecerse la pantalla")]
    public float fadeDuration = 2.5f;
    [Tooltip("Delay antes de mostrar el mensaje y botón")]
    public float showUIDelay = 0.5f;
    [Tooltip("Tiempo del fade in del texto/botón")]
    public float uiFadeDuration = 1f;

    private void Awake()
    {
        // Ocultar todo al inicio
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.gameObject.SetActive(true);
            fadeImage.raycastTarget = false; // no bloquea clicks al inicio
        }

        if (deathUIGroup != null)
        {
            deathUIGroup.alpha = 0f;
            deathUIGroup.interactable = false;
            deathUIGroup.blocksRaycasts = false;
        }

        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetryClicked);
    }

    private void Start()
    {
        // Suscribirse al evento de muerte del PlayerStateMachine
        if (PlayerStateMachine.Instance != null)
            PlayerStateMachine.Instance.OnPlayerDied += ShowDeathScreen;
    }

    private void OnDestroy()
    {
        if (PlayerStateMachine.Instance != null)
            PlayerStateMachine.Instance.OnPlayerDied -= ShowDeathScreen;

        if (retryButton != null)
            retryButton.onClick.RemoveListener(OnRetryClicked);
    }

    // ─── Mostrar pantalla de muerte ──────────────────────────────────────────

    public void ShowDeathScreen()
    {
        StartCoroutine(FadeAndShow());
    }

    private IEnumerator FadeAndShow()
    {
        // Liberar el cursor para poder pulsar el botón
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Fase 1: oscurecer la pantalla
        float t = 0f;
        Color startColor = fadeImage != null ? fadeImage.color : Color.black;
        Color endColor   = new Color(0f, 0f, 0f, 1f);

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime; // unscaled por si Time.timeScale = 0
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

        // Fase 2: esperar un momento antes de mostrar UI
        yield return new WaitForSecondsRealtime(showUIDelay);

        // Fase 3: fade in del texto y botón
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

    // ─── Botón Reintentar ────────────────────────────────────────────────────

    private void OnRetryClicked()
    {
        Debug.Log("[DeathScreen] Reintentar — recargando escena.");

        // Restaurar timeScale por si lo paramos
        Time.timeScale = 1f;

        // Recargar la escena actual
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
