using UnityEngine;

/// <summary>
/// Mantiene la Spot Light en la posición del modelo de la linterna,
/// pero le da la rotación de la cámara (apunta siempre hacia el centro de pantalla).
///
/// Coloca este script en el GameObject de la Spot Light (debe estar fuera del modelo
/// de la linterna, por ejemplo como hijo de PlayerCameraRoot o suelto en la escena).
/// </summary>
[DefaultExecutionOrder(10001)] // Ejecutar después del IK
public class FlashlightLightFollower : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Transform del modelo de la linterna (donde se ve físicamente)")]
    public Transform flashlightModel;

    [Tooltip("Cámara principal (de donde toma la rotación)")]
    public Camera mainCamera;

    [Header("Offset")]
    [Tooltip("Offset adicional de la posición desde la linterna (en local de la linterna)")]
    public Vector3 positionOffset = Vector3.zero;

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (flashlightModel == null || mainCamera == null) return;

        // POSICIÓN → desde el modelo de la linterna (sale visualmente desde ahí)
        transform.position = flashlightModel.position
                           + flashlightModel.TransformDirection(positionOffset);

        // ROTACIÓN → siempre hacia donde mira la cámara
        transform.rotation = mainCamera.transform.rotation;
    }
}
