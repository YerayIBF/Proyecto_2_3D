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

    [Header("Aturdimiento")]
    public float stunDuration = 3f;

    [Header("Investigación")]
    public float investigateWaitTime    = 4f;
    public float approximateNoiseOffset = 2f;

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
    
    // private Animator  _anim;

    // ─── Estado principal ─────────────────────────────────────────────────────

    private enum State { Wander, Chase, Attack, Stunned, Investigate, CheckLocker }
    private State _state = State.Wander;

    // Aturdimiento
    private bool  _isStunned = false;
    private float _stunTimer = 0f;

    // Investigación
    private bool    _reachedInvestigation = false;
    private float   _investigateWaitTimer = 0f;
    private Vector3 _investigateTarget    = Vector3.zero;

    // Taquillas
    private LockerInteractable[] _allLockers    = null;
    private LockerInteractable   _targetLocker  = null;
    private bool  _lockerOpened    = false;
    private float _lockerWaitTimer = 0f;

    // Combate
    private bool _attackOnCooldown = false;

    // Vio al jugador esconderse
    public LockerInteractable _knownLockerWithPlayer = null;
    private bool _playerWasHiding = false;

    // ─── Init ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _agent  = GetComponent<NavMeshAgent>();
        
    }

    private void Start()
    {
        _allLockers = Object.FindObjectsByType<LockerInteractable>(FindObjectsSortMode.None);
        Debug.Log($"[BT] Taquillas en escena: {_allLockers.Length}");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  UPDATE — el árbol evalúa el estado actual cada frame
    // ═══════════════════════════════════════════════════════════════════════════

    private void Update()
    {
        TrackPlayerHiding();

        // Ejecutar el comportamiento del estado actual
        switch (_state)
        {
            case State.Wander:       UpdateWander();      break;
            case State.Chase:        UpdateChase();       break;
            case State.Attack:       UpdateAttack();      break;
            case State.Stunned:      UpdateStunned();     break;
            case State.Investigate:  UpdateInvestigate(); break;
            case State.CheckLocker:  UpdateCheckLocker(); break;
        }

        // Transiciones globales que pueden interrumpir cualquier estado
        EvaluateTransitions();
    }

    // ─── Transiciones ────────────────────────────────────────────────────────


    private void EvaluateTransitions()
    {
        // No interrumpir Attack ni CheckLocker en mitad de su secuencia
        if (_state == State.Attack) return;
        if (_state == State.CheckLocker) return;

        // 1. Aturdido
        if (_isStunned)
        {
            ChangeState(State.Stunned);
            return;
        }

        // 2. Sabe en qué taquilla está el jugador → ir directo
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

        // 3. Ve al jugador → Chase
        if (CanSeePlayer())
        {
            ChangeState(State.Chase);
            return;
        }

        // 4. Oyó ruido y no está investigando → Investigate
        

        // 5. Sin estímulos y no está en medio de algo → Wander
        if (_state != State.Investigate && _state != State.CheckLocker)
        {
            ChangeState(State.Wander);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  COMPORTAMIENTOS POR ESTADO
    // ═══════════════════════════════════════════════════════════════════════════

    // ── WANDER ───────────────────────────────────────────────────────────────

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

    // ── CHASE ────────────────────────────────────────────────────────────────

    private void UpdateChase()
    {
        _agent.speed = chaseSpeed;
        _agent.SetDestination(player.position);

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= attackRange && !_attackOnCooldown)
        {
            ChangeState(State.Attack);
        }
        else if (dist > chaseRange && !CanSeePlayer())
        {
            ChangeState(State.Wander);
        }
    }

    // ── ATTACK ───────────────────────────────────────────────────────────────

    private void UpdateAttack()
    {
        _agent.ResetPath();
        // _anim?.SetTrigger("Attack");

        bool playerHiding = lockerSystem != null && lockerSystem.IsHiding;
        if (!playerHiding)
            KillPlayer();

        _attackOnCooldown = true;
        Invoke(nameof(ResetAttack), attackCooldownTime);

        // Volver a Chase tras el ataque
        ChangeState(State.Chase);
    }

    private void ResetAttack() => _attackOnCooldown = false;

    // ── STUNNED ──────────────────────────────────────────────────────────────

    private void UpdateStunned()
    {
        _agent.ResetPath();
        _stunTimer += Time.deltaTime;
        if (_stunTimer >= stunDuration)
        {
            _isStunned = false;
            _stunTimer = 0f;
            ChangeState(State.Wander);
            Debug.Log("[BT] Recuperado.");
        }
    }

    public void Stun()
    {
        _isStunned = true;
        _stunTimer = 0f;
    }

    // ── INVESTIGATE ──────────────────────────────────────────────────────────

    private void StartInvestigation(Vector3 noisePos)
    {
        _reachedInvestigation = false;
        _investigateWaitTimer = 0f;
        _targetLocker         = null;
        _lockerOpened         = false;

        Vector2 offset = Random.insideUnitCircle * approximateNoiseOffset;
        _investigateTarget = noisePos + new Vector3(offset.x, 0f, offset.y);

        _agent.speed = investigateSpeed;
        _agent.SetDestination(_investigateTarget);

        ChangeState(State.Investigate);
        Debug.Log($"[BT] Investigando en {_investigateTarget}");
    }

    private void UpdateInvestigate()
{
    if (!_reachedInvestigation)
    {
        // Moviéndose al punto de ruido
        if (!_agent.pathPending && _agent.remainingDistance < 0.6f)
        {
            _reachedInvestigation = true;
            _agent.ResetPath();
            Debug.Log("[BT] Llegó al punto investigado.");

            // ¿Hay taquillas cerca? Siempre revisar si existe alguna en rango
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
        // Esperando en el punto (sin taquillas)
        _investigateWaitTimer += Time.deltaTime;
        if (_investigateWaitTimer >= investigateWaitTime)
        {
            Debug.Log("[BT] Investigación terminada → Wander.");
            ChangeState(State.Wander);
        }
    }
}

    // ── CHECK LOCKER ─────────────────────────────────────────────────────────

   private void UpdateCheckLocker()
{
    // Si no hay taquilla objetivo, salir
    if (_targetLocker == null)
    {
        ChangeState(State.Wander);
        return;
    }

    // Si el jugador ya no está escondido y no estamos abriendo la puerta, cancelar
    if (_knownLockerWithPlayer == null && !_lockerOpened)
    {
        Debug.Log("[BT] Jugador salió de la taquilla, cancelando inspección.");
        _targetLocker = null;
        ChangeState(State.Wander);
        return;
    }

    float dist = Vector3.Distance(transform.position, _targetLocker.transform.position);

    if (!_lockerOpened)
    {
        _agent.SetDestination(_targetLocker.transform.position);
        _lockerMoveTimer += Time.deltaTime;

        // Timeout de movimiento
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
                transform.rotation = Quaternion.LookRotation(dir);

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
            // Acaba de esconderse — ¿lo veía?
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
        float dist = Vector3.Distance(eyes.position, player.position);
        if (dist > detectionRange) return false;
        Vector3 dir = (player.position - eyes.position).normalized;
        if (Vector3.Angle(eyes.forward, dir) > detectionAngle * 0.5f) return false;
        if (Physics.Raycast(eyes.position, dir, dist, visionBlockMask)) return false;
        return true;
    }

    private void KillPlayer()
    {
        Debug.Log("[BT] GAME OVER.");
        // GameManager.Instance?.GameOver();
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

        // ─── Propiedades para Debug UI ──────────────────────────────────────────

    public string CurrentStateName => _state.ToString();
    public float DistanceToPlayer
    {
        get
        {
            if (player == null) return -1f;
            return Vector3.Distance(transform.position, player.position);
        }
    }
    public bool IsSeeingPlayer => CanSeePlayer();
    public bool IsCurrentlyStunned => _isStunned;
    public bool HasKnownLockerWithPlayer => _knownLockerWithPlayer != null;
    public string CurrentTargetInfo
    {
        get
        {
            switch (_state)
            {
                case State.Wander:
                    return patrolPoints.Length > 0 && _patrolIndex < patrolPoints.Length 
                        ? $"Punto {_patrolIndex}: {patrolPoints[_patrolIndex].position}" 
                        : "Sin puntos";
                case State.Chase:
                    return "Jugador";
                case State.Attack:
                    return "Atacando";
                case State.Stunned:
                    return "Aturdido";
                case State.Investigate:
                    return _reachedInvestigation ? "Esperando en punto" : $"Investigando: {_investigateTarget}";
                case State.CheckLocker:
                    return _targetLocker != null ? _targetLocker.name : "Ninguna";
                default:
                    return "";
            }
        }
    }
    public float AgentRemainingDistance => _agent != null && _agent.hasPath ? _agent.remainingDistance : -1f;
    public float AgentSpeed => _agent != null ? _agent.speed : 0f;
}