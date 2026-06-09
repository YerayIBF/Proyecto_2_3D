using UnityEngine;

/// <summary>
/// Activa/desactiva el mando 3D y su cámara cuando el panelControles se abre/cierra.
/// </summary>
public class MenuControles : MonoBehaviour
{
    [Header("Panel a vigilar")]
    public GameObject panelControles;

    [Header("Mando 3D")]
    public Camera mandoCamera;
    public Transform mando3D;
    public float rotationSpeed = 0f;

    private bool _panelActivoPrevio = false;

    private void Awake()
    {
        // Estado inicial: mando y cámara desactivados
        if (mando3D != null) mando3D.gameObject.SetActive(false);
        if (mandoCamera != null) mandoCamera.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (panelControles == null) return;

        bool panelActivoAhora = panelControles.activeInHierarchy;

        if (panelActivoAhora && !_panelActivoPrevio)
        {
            if (mando3D != null) mando3D.gameObject.SetActive(true);
            if (mandoCamera != null) mandoCamera.gameObject.SetActive(true);
        }
        else if (!panelActivoAhora && _panelActivoPrevio)
        {
            if (mando3D != null) mando3D.gameObject.SetActive(false);
            if (mandoCamera != null) mandoCamera.gameObject.SetActive(false);
        }

        _panelActivoPrevio = panelActivoAhora;

        // Rotación opcional del mando
        if (panelActivoAhora && mando3D != null && rotationSpeed != 0f)
        {
            mando3D.Rotate(0f, rotationSpeed * Time.unscaledDeltaTime, 0f, Space.World);
        }
    }
}