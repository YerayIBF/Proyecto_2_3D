using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

/// <summary>
/// Cámara de muerte estilo GTA — al morir el jugador, la cámara se eleva
/// y apunta hacia abajo enfocando el cuerpo. Funciona con Cinemachine 3.x.
///
/// Al respawnear, llamar a ResetCamera() para devolver la cámara normal.
/// </summary>
public class DeathCamera : MonoBehaviour
{
    [Header("Cámaras Cinemachine")]
    public CinemachineCamera deathCamera;
    public CinemachineCamera normalCamera;

    [Header("Referencia del jugador")]
    public Transform playerBody;

    [Header("Configuración cámara cenital")]
    public float heightAboveBody = 4f;
    public float horizontalOffset = 2f;
    public float zoomDuration = 1.5f;

    [Header("Prioridades")]
    public int priorityHigh = 30;
    public int priorityLow  = 5;

    private void Awake()
    {
        if (deathCamera != null)
            deathCamera.Priority = priorityLow;
    }

    private void Start()
    {
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

        yield return null;

        Vector3 targetPos = playerBody.position
                          + Vector3.up * heightAboveBody
                          + playerBody.forward * -horizontalOffset;

        deathCamera.transform.position = targetPos;
        deathCamera.transform.LookAt(playerBody.position);

        if (normalCamera != null) normalCamera.Priority = priorityLow;
        deathCamera.Priority = priorityHigh;

        float t = 0f;
        while (t < zoomDuration)
        {
            t += Time.unscaledDeltaTime;
            if (playerBody != null)
                deathCamera.transform.LookAt(playerBody.position);
            yield return null;
        }
    }

    // ─── Reset al respawnear ─────────────────────────────────────────────────

    /// <summary>
    /// Devuelve las prioridades al estado normal. Llamar desde el respawn.
    /// </summary>
    public void ResetCamera()
    {
        Debug.Log("[DeathCamera] Reset al respawnear.");

        // Para cualquier coroutine en curso
        StopAllCoroutines();

        if (deathCamera != null) deathCamera.Priority = priorityLow;
        if (normalCamera != null) normalCamera.Priority = priorityHigh;
    }
}