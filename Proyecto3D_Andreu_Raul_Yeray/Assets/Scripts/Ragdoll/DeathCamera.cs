using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

/// <summary>
/// Cámara de muerte estilo GTA — al morir el jugador, la cámara se eleva
/// y apunta hacia abajo enfocando el cuerpo. Funciona con Cinemachine 3.x.
///
/// Setup:
/// 1. Crea una nueva CinemachineCamera en la escena (GameObject → Cinemachine → Cinemachine Camera)
/// 2. Nómbrala "DeathCamera"
/// 3. Asígnala en este script
/// 4. Asigna también la cámara principal (ThirdPerson) para bajarle prioridad al morir
/// </summary>
public class DeathCamera : MonoBehaviour
{
    [Header("Cámaras Cinemachine")]
    [Tooltip("Cámara que se activa al morir (vista cenital del cuerpo)")]
    public CinemachineCamera deathCamera;
    [Tooltip("Cámara normal del jugador (se baja prioridad al morir)")]
    public CinemachineCamera normalCamera;

    [Header("Referencia del jugador")]
    [Tooltip("El cuerpo del jugador (a quién enfoca la cámara de muerte)")]
    public Transform playerBody;

    [Header("Configuración cámara cenital")]
    [Tooltip("Altura sobre el jugador")]
    public float heightAboveBody = 4f;
    [Tooltip("Desplazamiento horizontal (para no quedar 100% vertical)")]
    public float horizontalOffset = 2f;
    [Tooltip("Movimiento suave de zoom al activarse")]
    public float zoomDuration = 1.5f;

    [Header("Prioridades")]
    public int priorityHigh = 30;
    public int priorityLow  = 5;

    private void Awake()
    {
        // Empezar con la cámara de muerte con prioridad baja
        if (deathCamera != null)
            deathCamera.Priority = priorityLow;
    }

    private void Start()
    {
        // Suscribirse al evento de muerte
        if (PlayerStateMachine.Instance != null)
            PlayerStateMachine.Instance.OnPlayerDied += ActivateDeathCamera;
    }

    private void OnDestroy()
    {
        if (PlayerStateMachine.Instance != null)
            PlayerStateMachine.Instance.OnPlayerDied -= ActivateDeathCamera;
    }

    // ─── Activar cámara de muerte ────────────────────────────────────────────

    public void ActivateDeathCamera()
    {
        Debug.Log("[DeathCamera] Activando vista cenital.");
        StartCoroutine(MoveCameraAbove());
    }

    private IEnumerator MoveCameraAbove()
    {
        if (deathCamera == null || playerBody == null)
        {
            Debug.LogError("[DeathCamera] Falta deathCamera o playerBody.");
            yield break;
        }

        // Esperar 1 frame para que el ragdoll empiece a caer
        yield return null;

        // Posicionar la cámara arriba del jugador con un poco de offset
        Vector3 targetPos = playerBody.position
                          + Vector3.up * heightAboveBody
                          + playerBody.forward * -horizontalOffset; // un poco detrás

        deathCamera.transform.position = targetPos;

        // Apuntar hacia abajo al cuerpo del jugador
        deathCamera.transform.LookAt(playerBody.position);

        // Cambiar prioridades — Cinemachine hace blend automático
        if (normalCamera != null) normalCamera.Priority = priorityLow;
        deathCamera.Priority = priorityHigh;

        // Mantener el LookAt durante unos segundos para seguir el ragdoll que cae
        float t = 0f;
        while (t < zoomDuration)
        {
            t += Time.unscaledDeltaTime;
            if (playerBody != null)
                deathCamera.transform.LookAt(playerBody.position);
            yield return null;
        }
    }
}
