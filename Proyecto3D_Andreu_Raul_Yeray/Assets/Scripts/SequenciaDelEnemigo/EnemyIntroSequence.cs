using UnityEngine;
using UnityEngine.AI;
using StarterAssets;
using System.Collections;

/// <summary>
/// Secuencia scripted de aparición del enemigo (tutorial / jumpscare).
///
/// FLUJO:
/// 1. Algo externo llama a StartSequence() — típicamente KeyPickup al recoger la llave
/// 2. El enemigo recorre un path predefinido (doblando una esquina)
/// 3. Se queda mirando al jugador. El jugador no puede moverse (sí girar cámara y usar linterna)
/// 4. El jugador stunea al enemigo apuntando con la linterna
/// 5. Se activa el BehaviourTree con un stun extra largo para que el jugador aprenda a esconderse
/// 6. A partir de ahí el enemigo funciona en su comportamiento normal
/// </summary>
public class EnemyIntroSequence : MonoBehaviour
{
    [Header("Referencias")]
    public EnemyBehaviourTree behaviourTree;
    public Transform player;
    public Transform ghostModel;

    [Header("Path de aparición")]
    public Transform[] pathPoints;
    public float introSpeed = 1.5f;

    [Header("Comportamiento")]
    [Tooltip("Velocidad del giro hacia el jugador (más bajo = más lento, dramático)")]
    public float lookAtRotationSpeed = 2f;
    [Tooltip("Espera al final del recorrido antes de poder recibir stun")]
    public float stareDelay = 1f;
    [Tooltip("Duración del stun en esta primera vez (tutorial)")]
    public float tutorialStunDuration = 8f;
    [Tooltip("Ángulo (en grados) dentro del cual la linterna cuenta como apuntando al enemigo")]
    public float stunDetectAngle = 20f;

    [Header("Mensajes del tutorial (catalán)")]
    [Tooltip("Mensaje al apuntar al enemigo por primera vez (antes del stun)")]
    [TextArea]
    public string mensajeApuntar = "Sembla que és sensible a la llum, hauria d'apuntar-li amb la llanterna (RB)";
    public float duracionMensajeApuntar = 15f;

    [Tooltip("Mensaje tras stunearlo, sugiriendo esconderse")]
    [TextArea]
    public string mensajeEsconderse = "Hauria d'amagar-me... Potser en una taquilla. Prem [X] per amagar-te.";
    public float duracionMensajeEsconderse = 6f;
    [Tooltip("Retardo antes de mostrar el mensaje de esconderse tras stunear")]
    public float retardoMensajeEsconderse = 0.5f;

    // ─── Estado interno ──────────────────────────────────────────────────────

    private NavMeshAgent _agent;
    private Animator _anim;

    private bool _sequenceActive = false;
    private bool _waitingForStun = false;
    private bool _isStunned = false;

    private static readonly int HashSpeed   = Animator.StringToHash("Speed");
    private static readonly int HashStunned = Animator.StringToHash("Stunned");

    // ─── Init ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (behaviourTree == null) behaviourTree = GetComponent<EnemyBehaviourTree>();
        if (ghostModel != null) _anim = ghostModel.GetComponent<Animator>();

        if (behaviourTree != null) behaviourTree.enabled = false;
        if (ghostModel != null) ghostModel.gameObject.SetActive(false);

        EnemyAudio audio = GetComponent<EnemyAudio>();
        if (audio == null) audio = GetComponentInChildren<EnemyAudio>();
        if (audio != null) audio.enabled = false;
    }

    // ─── API pública ─────────────────────────────────────────────────────────

    public void StartSequence()
    {
        if (_sequenceActive) return;
        _sequenceActive = true;

        if (ghostModel != null) ghostModel.gameObject.SetActive(true);

        BlockPlayerMovement(true);
        StartCoroutine(IntroRoutine());
    }

    // ─── Cinemática ──────────────────────────────────────────────────────────

    private IEnumerator IntroRoutine()
    {
        if (_agent == null) yield break;

        _agent.isStopped = false;

        for (int i = 0; i < pathPoints.Length; i++)
        {
            if (pathPoints[i] == null) continue;

            _agent.speed = introSpeed;
            _agent.SetDestination(pathPoints[i].position);
            yield return null;

            float timer = 0f;
            while (_agent.pathPending || _agent.remainingDistance > 0.5f)
            {
                timer += Time.deltaTime;
                if (timer > 10f) break;
                yield return null;
            }
        }

        _agent.ResetPath();
        _agent.isStopped = true;

        yield return StartCoroutine(SmoothLookAtPlayer());

        yield return new WaitForSeconds(stareDelay);

        _waitingForStun = true;

        if (GameManager.instance != null)
            GameManager.instance.ReproducirVoz(mensajeApuntar, duracionMensajeApuntar);
    }

    private IEnumerator SmoothLookAtPlayer()
    {
        if (player == null) yield break;

        float t = 0f;
        Quaternion startRot = transform.rotation;
        Quaternion ghostStartRot = ghostModel != null ? ghostModel.rotation : startRot;

        while (t < 1f)
        {
            t += Time.deltaTime * lookAtRotationSpeed;

            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            dirToPlayer.y = 0f;
            if (dirToPlayer == Vector3.zero) yield break;

            Quaternion targetRot = Quaternion.LookRotation(dirToPlayer);

            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            if (ghostModel != null)
                ghostModel.rotation = Quaternion.Slerp(ghostStartRot, targetRot, t);

            yield return null;
        }
    }

    // ─── Update ──────────────────────────────────────────────────────────────

    private void Update()
    {
        if (_sequenceActive)
            UpdateGhostAndAnimations();

        if (_waitingForStun && player != null && !_isStunned)
        {
            LookAtPlayerSmooth();
            CheckForStun();
        }
    }

    private void UpdateGhostAndAnimations()
    {
        if (ghostModel == null) return;

        ghostModel.position = Vector3.Lerp(ghostModel.position, transform.position, Time.deltaTime * 10f);

        if (!_waitingForStun && _agent.velocity.sqrMagnitude > 0.05f)
        {
            Vector3 moveDir = _agent.velocity;
            moveDir.y = 0f;
            if (moveDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                ghostModel.rotation = Quaternion.Slerp(ghostModel.rotation, targetRot, Time.deltaTime * 8f);
            }
        }

        if (_anim != null)
        {
            _anim.SetBool(HashStunned, _isStunned);

            float normalizedSpeed = 0f;
            if (!_isStunned && _agent.velocity.magnitude > 0.1f)
                normalizedSpeed = Mathf.Lerp(0.3f, 0.5f, _agent.velocity.magnitude / introSpeed);

            _anim.SetFloat(HashSpeed, normalizedSpeed, 0.1f, Time.deltaTime);
        }
    }

    private void LookAtPlayerSmooth()
    {
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        dirToPlayer.y = 0f;
        if (dirToPlayer == Vector3.zero) return;

        Quaternion lookRot = Quaternion.LookRotation(dirToPlayer);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * lookAtRotationSpeed);
        if (ghostModel != null)
            ghostModel.rotation = Quaternion.Slerp(ghostModel.rotation, lookRot, Time.deltaTime * lookAtRotationSpeed);
    }

    // ─── Stun ────────────────────────────────────────────────────────────────

    private void CheckForStun()
    {
        FlashlightSystem fs = FindFirstObjectByType<FlashlightSystem>();
        if (fs == null || !fs.IsOn || !fs.IsAiming) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 dirToEnemy = (transform.position - cam.transform.position).normalized;
        float angle = Vector3.Angle(cam.transform.forward, dirToEnemy);

        if (angle < stunDetectAngle)
            OnStunned();
    }

    private void OnStunned()
    {
        if (!_waitingForStun) return;
        _waitingForStun = false;
        _isStunned = true;

        Debug.Log($"[EnemyIntro] ¡Stun aplicado! Activando enemigo con stun de {tutorialStunDuration}s.");

        BlockPlayerMovement(false);

        if (ghostModel != null)
        {
            ghostModel.position = transform.position;
            ghostModel.rotation = transform.rotation;
        }

        EnemyAudio audio = GetComponent<EnemyAudio>();
        if (audio == null) audio = GetComponentInChildren<EnemyAudio>();
        if (audio != null) audio.enabled = true;

        if (behaviourTree != null)
        {
            behaviourTree.enabled = true;

            float originalStun = behaviourTree.stunDuration;
            behaviourTree.stunDuration = tutorialStunDuration;
            behaviourTree.Stun();

            StartCoroutine(RestoreStunDuration(originalStun));
        }

        // ─── Mensaje de tutorial: sugerir esconderse ───
        StartCoroutine(MostrarMensajeEsconderse());

        _sequenceActive = false;
        _agent.isStopped = false;
    }

    private IEnumerator MostrarMensajeEsconderse()
    {
        yield return new WaitForSeconds(retardoMensajeEsconderse);

        if (GameManager.instance != null)
            GameManager.instance.ReproducirVoz(mensajeEsconderse, duracionMensajeEsconderse);
    }

    private IEnumerator RestoreStunDuration(float originalDuration)
    {
        yield return new WaitForSeconds(tutorialStunDuration + 0.5f);
        if (behaviourTree != null)
            behaviourTree.stunDuration = originalDuration;
    }

    // ─── Bloqueo de movimiento del jugador ───────────────────────────────────

    private void BlockPlayerMovement(bool block)
    {
        if (block)
            StartCoroutine(KeepMovementBlocked());
    }

    private IEnumerator KeepMovementBlocked()
    {
        while (_sequenceActive && !_isStunned)
        {
            if (PlayerStateMachine.Instance != null && PlayerStateMachine.Instance.inputs != null)
            {
                PlayerStateMachine.Instance.inputs.move = Vector2.zero;
                PlayerStateMachine.Instance.inputs.sprint = false;
                PlayerStateMachine.Instance.inputs.jump = false;
            }
            yield return null;
        }
    }
}