using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]

public class EnemyBehaviourTree : MonoBehaviour
{
    // ─── Referencias ─────────────────────────────────────────────────────────

    [Header("Referencias")]
    public Transform    player;
    public LockerSystem lockerSystem;
    public Transform    eyes;

    [Header("GHOST — Modelo visual de Mixamo")]
    [Tooltip("El modelo visual con el Animator. Sigue al agent suavizado.")]
    public Transform ghostModel;
    [Tooltip("Suavizado de posición (más bajo = más pegado, más alto = más fluido con retraso)")]
    public float ghostPositionSmooth = 0.08f;
    [Tooltip("Velocidad de rotación del ghost hacia la dirección de movimiento")]
    public float ghostRotationSpeed = 12f;

    [Header("Patrulla")]
    public Transform[] patrolPoints;
    private int _patrolIndex = 0;

    [Header("Detección visual")]
    public float     detectionRange = 12f;
    public float     detectionAngle = 90f;
    public LayerMask visionBlockMask;

    [Header("Combate")]
    public float attackRange        = 1.5f;
    public float chaseRange         = 15f;
    public float attackCooldownTime = 1.5f;
    public float attackDamage       = 100f;

    [Header("Aturdimiento")]
    public float stunDuration = 3f;

    [Header("Investigación")]
    public float investigateWaitTime    = 4f;
    public float approximateNoiseOffset = 2f;
    [Tooltip("Tiempo mínimo entre investigaciones (para no spamear)")]
    public float investigateCooldown    = 3f;
    private float _investigateCooldownTimer = 0f;

    [Header("Taquillas")]
    public float lockerCheckRange = 8f;
    public float lockerOpenWait   = 2f;

    private float _lockerMoveTimer = 0f;
    private const float _MAX_LOCKER_MOVE_TIME = 5f;

    [Header("Velocidades")]
    public float patrolSpeed      = 2f;
    public float chaseSpeed       = 4.5f;
    public float investigateSpeed = 3f;

    // ─── Componentes ─────────────────────────────────────────────────────────

    private NavMeshAgent _agent;
    private Animator     _anim;   // está en el ghostModel

    // ─── Estado principal ─────────────────────────────────────────────────────

    private enum State { Wander, Chase, Attack, Stunned, Investigate, CheckLocker }
    private State _state = State.Wander;

    private bool  _isStunned = false;
    private float _stunTimer = 0f;

    private bool    _reachedInvestigation = false;
    private float   _investigateWaitTimer = 0f;
    private Vector3 _investigateTarget    = Vector3.zero;
    private bool    _hasPendingNoise      = false;
    private Vector3 _pendingNoisePos      = Vector3.zero;

    private LockerInteractable[] _allLockers    = null;
    private LockerInteractable   _targetLocker  = null;
    private bool  _lockerOpened    = false;
    private float _lockerWaitTimer = 0f;

    private bool _attackOnCooldown = false;

    public LockerInteractable _knownLockerWithPlayer = null;
    private bool _playerWasHiding = false;

    private Vector3 _ghostVelocity = Vector3.zero;

    // ─── Hashes de Animator ──────────────────────────────────────────────────

    private static readonly int HashSpeed   = Animator.StringToHash("Speed");
    private static readonly int HashState   = Animator.StringToHash("State");
    private static readonly int HashAttack  = Animator.StringToHash("Attack");
    private static readonly int HashStunned = Animator.StringToHash("Stunned");
    private static readonly int HashOpen    = Animator.StringToHash("Open");

    // ─── Init ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();

        if (ghostModel != null)
            _anim = ghostModel.GetComponent<Animator>();

        if (_agent != null)
        {
            _agent.updateRotation = false;
            _agent.updatePosition = true;
        }
    }

    private void Start()
    {
        _allLockers = Object.FindObjectsByType<LockerInteractable>(FindObjectsSortMode.None);
        Debug.Log($"[BT] Taquillas en escena: {_allLockers.Length}");

        if (ghostModel != null)
            ghostModel.SetParent(null);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  API PÚBLICA — el sistema de sonido del compañero llama esto
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Llamado desde EmitirSonido.EmitirRuido cuando el enemigo oye un ruido.
    /// </summary>
    public void OnHeardNoise(Vector3 noisePosition)
    {
        if (_isStunned) return;
        if (_state == State.Chase || _state == State.Attack) return;
        if (_state == State.CheckLocker) return;
        if (_investigateCooldownTimer > 0f) return;

        _hasPendingNoise = true;
        _pendingNoisePos = noisePosition;
        Debug.Log($"[BT] Ruido oído en {noisePosition}");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  GHOST
    // ═══════════════════════════════════════════════════════════════════════════

    private void LateUpdate()
    {
        if (ghostModel == null) return;

        ghostModel.position = Vector3.SmoothDamp(
            ghostModel.position, transform.position,
            ref _ghostVelocity, ghostPositionSmooth);

        Vector3 moveDir = _agent.velocity;
        moveDir.y = 0f;

        if (moveDir.sqrMagnitude > 0.05f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            ghostModel.rotation = Quaternion.Slerp(
                ghostModel.rotation, targetRot,
                Time.deltaTime * ghostRotationSpeed);
        }

        if (_anim != null)
        {
            float normalizedSpeed = _agent.velocity.magnitude / chaseSpeed;
            _anim.SetFloat(HashSpeed, normalizedSpeed, 0.1f, Time.deltaTime);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  UPDATE
    // ═══════════════════════════════════════════════════════════════════════════

    private void Update()
    {
        if (_investigateCooldownTimer > 0f)
            _investigateCooldownTimer -= Time.deltaTime;

        TrackPlayerHiding();

        switch (_state)
        {
            case State.Wander:       UpdateWander();      break;
            case State.Chase:        UpdateChase();       break;
            case State.Attack:       UpdateAttack();      break;
            case State.Stunned:      UpdateStunned();     break;
            case State.Investigate:  UpdateInvestigate(); break;
            case State.CheckLocker:  UpdateCheckLocker(); break;
        }

        EvaluateTransitions();
    }

    // ─── Transiciones ────────────────────────────────────────────────────────

    private void EvaluateTransitions()
    {
        if (_state == State.Attack) return;
        if (_state == State.CheckLocker) return;

        if (_isStunned)
        {
            ChangeState(State.Stunned);
            return;
        }

        if (_knownLockerWithPlayer != null)
        {
            if (_state != State.CheckLocker)
            {
                _targetLocker  = _knownLockerWithPlayer;
                _lockerOpened  = false;
                _lockerWaitTimer = 0f;
                ChangeState(State.CheckLocker);
            }
            return;
        }

        if (CanSeePlayer())
        {
            ChangeState(State.Chase);
            return;
        }

        // Oyó ruido y no está investigando → Investigate
        if (_hasPendingNoise && _state != State.Investigate)
        {
            _hasPendingNoise = false;
            StartInvestigation(_pendingNoisePos);
            return;
        }

        if (_state != State.Investigate && _state != State.CheckLocker)
        {
            ChangeState(State.Wander);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  COMPORTAMIENTOS
    // ═══════════════════════════════════════════════════════════════════════════

    private void UpdateWander()
    {
        _agent.speed = patrolSpeed;
        if (patrolPoints.Length == 0) return;
        if (!_agent.pathPending && _agent.remainingDistance < 0.5f)
        {
            _patrolIndex = (_patrolIndex + 1) % patrolPoints.Length;
            _agent.SetDestination(patrolPoints[_patrolIndex].position);
        }
    }

    private void UpdateChase()
    {
        _agent.speed = chaseSpeed;
        _agent.SetDestination(player.position);

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= attackRange && !_attackOnCooldown)
            ChangeState(State.Attack);
        else if (dist > chaseRange && !CanSeePlayer())
            ChangeState(State.Wander);
    }

    private void UpdateAttack()
    {
        _agent.ResetPath();

        if (_anim != null) _anim.SetTrigger(HashAttack);

        bool playerHiding = lockerSystem != null && lockerSystem.IsHiding;
        if (!playerHiding)
            KillPlayer();

        _attackOnCooldown = true;
        Invoke(nameof(ResetAttack), attackCooldownTime);

        ChangeState(State.Chase);
    }

    private void ResetAttack() => _attackOnCooldown = false;

    private void UpdateStunned()
    {
        _agent.ResetPath();

        if (_anim != null) _anim.SetBool(HashStunned, true);

        _stunTimer += Time.deltaTime;
        if (_stunTimer >= stunDuration)
        {
            _isStunned = false;
            _stunTimer = 0f;
            if (_anim != null) _anim.SetBool(HashStunned, false);
            ChangeState(State.Wander);
            Debug.Log("[BT] Recuperado.");
        }
    }

    public void Stun()
    {
        _isStunned = true;
        _stunTimer = 0f;
        Debug.Log("[BT] ¡Aturdido por la linterna!");
    }

    private void StartInvestigation(Vector3 noisePos)
    {
        _reachedInvestigation = false;
        _investigateWaitTimer = 0f;
        _targetLocker         = null;
        _lockerOpened         = false;

        Vector2 offset = Random.insideUnitCircle * approximateNoiseOffset;
        _investigateTarget = noisePos + new Vector3(offset.x, 0f, offset.y);

        if (NavMesh.SamplePosition(_investigateTarget, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            _investigateTarget = hit.position;

        _agent.speed = investigateSpeed;
        _agent.SetDestination(_investigateTarget);

        ChangeState(State.Investigate);
        Debug.Log($"[BT] Investigando en {_investigateTarget}");
    }

    private void UpdateInvestigate()
    {
        if (!_reachedInvestigation)
        {
            if (!_agent.pathPending && _agent.remainingDistance < 0.6f)
            {
                _reachedInvestigation = true;
                _agent.ResetPath();
                Debug.Log("[BT] Llegó al punto investigado.");

                LockerInteractable locker = FindRandomNearbyLocker();
                if (locker != null)
                {
                    _targetLocker    = locker;
                    _lockerOpened    = false;
                    _lockerWaitTimer = 0f;
                    _agent.speed     = investigateSpeed;
                    _agent.SetDestination(_targetLocker.transform.position);
                    ChangeState(State.CheckLocker);
                    Debug.Log($"[BT] Taquilla cercana → {_targetLocker.name}");
                    return;
                }
                else
                {
                    Debug.Log("[BT] No hay taquillas cercanas. Esperando...");
                }
            }
        }
        else
        {
            _investigateWaitTimer += Time.deltaTime;
            if (_investigateWaitTimer >= investigateWaitTime)
            {
                Debug.Log("[BT] Investigación terminada → Wander.");
                _investigateCooldownTimer = investigateCooldown;
                ChangeState(State.Wander);
            }
        }
    }

    private void UpdateCheckLocker()
    {
        if (_targetLocker == null)
        {
            ChangeState(State.Wander);
            return;
        }

        if (_knownLockerWithPlayer == null && !_lockerOpened)
        {
            Debug.Log("[BT] Jugador salió de la taquilla, cancelando.");
            _targetLocker = null;
            ChangeState(State.Wander);
            return;
        }

        float dist = Vector3.Distance(transform.position, _targetLocker.transform.position);

        if (!_lockerOpened)
        {
            _agent.SetDestination(_targetLocker.transform.position);
            _lockerMoveTimer += Time.deltaTime;

            if (_lockerMoveTimer > _MAX_LOCKER_MOVE_TIME || _agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                Debug.LogWarning("[BT] No se pudo alcanzar la taquilla. Abortando.");
                _knownLockerWithPlayer = null;
                _targetLocker = null;
                ChangeState(State.Wander);
                return;
            }

            if (dist < 1.5f)
            {
                _lockerOpened = true;
                _lockerWaitTimer = 0f;
                _lockerMoveTimer = 0f;
                _agent.ResetPath();

                Vector3 dir = (_targetLocker.transform.position - transform.position).normalized;
                dir.y = 0f;
                if (dir != Vector3.zero)
                {
                    if (ghostModel != null)
                        ghostModel.rotation = Quaternion.LookRotation(dir);
                    transform.rotation = Quaternion.LookRotation(dir);
                }

                if (_anim != null) _anim.SetTrigger(HashOpen);

                _targetLocker.OpenDoor();
                Debug.Log($"[BT] Abriendo: {_targetLocker.name}");
            }
        }
        else
        {
            if (_targetLocker == null)
            {
                ChangeState(State.Wander);
                return;
            }

            _lockerWaitTimer += Time.deltaTime;
            if (_lockerWaitTimer >= lockerOpenWait)
            {
                bool playerInside = lockerSystem != null
                                 && lockerSystem.IsHiding
                                 && lockerSystem.CurrentLocker == _targetLocker;

                if (playerInside)
                {
                    Debug.Log("[BT] ¡Jugador encontrado!");
                    KillPlayer();
                }
                else
                {
                    Debug.Log("[BT] Taquilla vacía.");
                    _targetLocker.CloseDoor();
                }

                _knownLockerWithPlayer = null;
                _targetLocker = null;
                _lockerOpened = false;
                _investigateCooldownTimer = investigateCooldown;
                ChangeState(State.Wander);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  DETECCIÓN
    // ═══════════════════════════════════════════════════════════════════════════

    private void TrackPlayerHiding()
    {
        if (lockerSystem == null) return;

        bool hidingNow = lockerSystem.IsHiding;

        if (hidingNow && !_playerWasHiding)
        {
            if (CanSeePlayerRaw())
            {
                _knownLockerWithPlayer = lockerSystem.CurrentLocker;
                Debug.Log($"[BT] Vi entrar al jugador en: {_knownLockerWithPlayer?.name}");
            }
        }

        if (!hidingNow && _playerWasHiding)
        {
            _knownLockerWithPlayer = null;
            Debug.Log("[BT] Jugador salió de la taquilla.");
        }

        _playerWasHiding = hidingNow;
    }

    private bool CanSeePlayer()
    {
        if (player == null) return false;
        if (lockerSystem != null && lockerSystem.IsHiding) return false;
        return CanSeePlayerRaw();
    }

    private bool CanSeePlayerRaw()
    {
        if (player == null) return false;
        if (eyes == null) return false;

        float dist = Vector3.Distance(eyes.position, player.position);
        if (dist > detectionRange) return false;

        // Apuntar al pecho del jugador, no a los pies
        Vector3 targetPoint = player.position + Vector3.up * 1f;
        Vector3 dir = (targetPoint - eyes.position).normalized;

        if (Vector3.Angle(eyes.forward, dir) > detectionAngle * 0.5f) return false;

        if (Physics.Raycast(eyes.position, dir, out RaycastHit hit, dist, visionBlockMask))
        {
            if (!hit.collider.transform.IsChildOf(player) && hit.collider.transform != player)
                return false;
        }

        return true;
    }

    private void KillPlayer()
    {
        Debug.Log("[BT] GAME OVER.");
        if (PlayerStateMachine.Instance != null)
            PlayerStateMachine.Instance.TakeDamage(attackDamage);
    }

    private LockerInteractable FindRandomNearbyLocker()
    {
        var inRange = new System.Collections.Generic.List<LockerInteractable>();
        foreach (var l in _allLockers)
        {
            if (l == null) continue;
            if (Vector3.Distance(transform.position, l.transform.position) <= lockerCheckRange)
                inRange.Add(l);
        }
        return inRange.Count == 0 ? null : inRange[Random.Range(0, inRange.Count)];
    }

    private void ChangeState(State newState)
    {
        if (_state == newState) return;
        _state = newState;
        Debug.Log($"[BT] → {newState}");

        if (_anim != null)
            _anim.SetInteger(HashState, (int)newState);
    }

    // ─── Gizmos ──────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        if (eyes != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(eyes.position, detectionRange);
        }
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, lockerCheckRange);

        if (_state == State.Investigate)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(_investigateTarget, 0.3f);
            Gizmos.DrawLine(transform.position, _investigateTarget);
        }
        if (_knownLockerWithPlayer != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, _knownLockerWithPlayer.transform.position);
        }
        else if (_targetLocker != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, _targetLocker.transform.position);
        }
    }

    // ─── Propiedades Debug UI ───────────────────────────────────────────────

    public string CurrentStateName => _state.ToString();
    public float DistanceToPlayer => player == null ? -1f : Vector3.Distance(transform.position, player.position);
    public bool IsSeeingPlayer => CanSeePlayer();
    public bool IsCurrentlyStunned => _isStunned;
    public bool HasKnownLockerWithPlayer => _knownLockerWithPlayer != null;
    public float AgentRemainingDistance => _agent != null && _agent.hasPath ? _agent.remainingDistance : -1f;
    public float AgentSpeed => _agent != null ? _agent.speed : 0f;

    public string CurrentTargetInfo
    {
        get
        {
            switch (_state)
            {
                case State.Wander:
                    return patrolPoints.Length > 0 && _patrolIndex < patrolPoints.Length
                        ? $"Punto {_patrolIndex}"
                        : "Sin puntos";
                case State.Chase:       return "Jugador";
                case State.Attack:      return "Atacando";
                case State.Stunned:     return "Aturdido";
                case State.Investigate: return _reachedInvestigation ? "Esperando" : "Yendo a investigar";
                case State.CheckLocker: return _targetLocker != null ? _targetLocker.name : "Ninguna";
                default:                return "";
            }
        }
    }
}