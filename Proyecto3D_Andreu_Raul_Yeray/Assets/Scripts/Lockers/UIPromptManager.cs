using UnityEngine;
using TMPro;

/// <summary>
/// Singleton para mostrar prompts contextuales en pantalla.
/// Coloca este script en un GameObject con un TextMeshProUGUI hijo.
/// El Canvas debe estar en Screen Space - Overlay.
/// </summary>
public class UIPromptManager : MonoBehaviour
{
    public static UIPromptManager Instance { get; private set; }

    [Tooltip("El TextMeshProUGUI donde se mostrará el mensaje")]
    public TextMeshProUGUI promptText;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Hide();
    }

    public void Show(string message)
    {
        if (promptText == null) return;
        promptText.text = message;
        promptText.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (promptText == null) return;
        promptText.gameObject.SetActive(false);
    }
}
