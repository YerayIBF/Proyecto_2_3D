using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyBehaviourTree : MonoBehaviour
{
    [Header("Referencias")]
    public Transform    player;
    public LockerSystem lockerSystem;
    public Transform    eyes;

    [Header("GHOST — Modelo visual de Mixamo")]
    public Transform ghostModel;
    public float ghostPositionSmooth = 0.08f;
    public float ghostRotationSpeed = 12f;

    [Header("Patrulla")]
    public Transform[] patrolPoints;
    private int _patrolIndex = 0;

    [Header("Detección visual")]
    public float     detectionRange = 12f;
    public float     detectionAngle = 90f;
    public LayerMask visionBlockMask;
    public float visionMemoryDuration = 2f;
    private float _visionMemoryTimer = 0f;

    [Header("Combate")]
    public float attackRange        = 1.5f;
    public float chaseRange         = 15f;
    public float attackCooldownTime = 1.5f;
    public float attackDamage       = 30f;
    public float lockerKillDelay    = 0.8f;

    [Header("Aturdimiento")]
    public float stunDuration = 3f;
    public float stunCooldown = 5f;
    private float _stunCooldownTimer = 0f;

    [Header("Investigación")]
    public float investigateWaitTime    = 4f;
    public float approximateNoiseOffset = 2f;
    public float investigateCooldown    = 3f;
    private float _investigateCooldownTimer = 0f;

    [Header("Taquillas")]
    public float lockerCheckRange = 8f;
    public float lockerOpenWait   = 2f;
    public float lockerApproachDistance = 1.2f;

    private float _lockerMoveTimer = 0f;
    private const float _MAX_LOCKER_MOVE_TIME = 5f;

    [Header("Velocidades")]
    public float patrolSpeed      = 2f;
    public float chaseSpeed       = 4.5f;
    public float investigateSpeed = 3f;

    private NavMeshAgent _agent;
    private Animator     _anim;
    private EnemyAudio   _audio;

    private enum State { Wander, Chase, Attack, Stunned, Investigate, CheckLocker }
    private State _state = State.Wander;
    private State _previousState = State.Wander;

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
    private bool  _lockerKillTriggered = false;
    private bool  _frozenAtLocker = false;

    private bool _attackOnCooldown = false;

    public LockerInteractable _knownLockerWithPlayer = null;
    private bool _playerWasHiding = false;

    private Vector3 _ghostVelocity = Vector3.zero;

    private static readonly int HashSpeed   = Animator.StringToHash("Speed");
    private static readonly int HashState   = Animator.StringToHash("State");
    private static readonly int HashAttack  = Animator.StringToHash("Attack");
    private static readonly int HashStunned = Animator.StringToHash("Stunned");
    private static readonly int HashOpen    = Animator.StringToHash("Open");

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (ghostModel != null) _anim = ghostModel.GetComponent<Animator>();

        _audio = GetComponent<EnemyAudio>();
        if (_audio == null) _audio = GetComponentInChildren<EnemyAudio>();
        if (_audio == null && ghostModel != null) _audio = ghostModel.GetComponent<EnemyAudio>();

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

        if (ghostModel != null) ghostModel.SetParent(null);
    }

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

    private void LateUpdate()
    {
        if (ghostModel == null) return;

        if (_frozenAtLocker)
            ghostModel.position = transform.position;
        else
            ghostModel.position = Vector3.SmoothDamp(
                ghostModel.position, transform.position,
                ref _ghostVelocity, ghostPositionSmooth);

        Vector3 moveDir = _agent.velocity;
        moveDir.y = 0f;

        if (!_frozenAtLocker && moveDir.sqrMagnitude > 0.05f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            ghostModel.rotation = Quaternion.Slerp(
                ghostModel.rotation, targetRot,
                Time.deltaTime * ghostRotationSpeed);
        }

        if (_anim != null)
        {
            float normalizedSpeed = 0f;

            if (!_frozenAtLocker)
            {
                float realSpeed = _agent.velocity.magnitude;
                if (realSpeed > 0.1f)
                {
                    if (realSpeed <= patrolSpeed)
                        normalizedSpeed = Mathf.Lerp(0.3f, 0.5f, realSpeed / patrolSpeed);
                    else
                        normalizedSpeed = Mathf.Lerp(0.5f, 1f, (realSpeed - patrolSpeed) / (chaseSpeed - patrolSpeed));
                }
            }

            _anim.SetFloat(HashSpeed, normalizedSpeed, 0.1f, Time.deltaTime);
        }
    }

    private void Update()
    {
        if (_investigateCooldownTimer > 0f) _investigateCooldownTimer -= Time.deltaTime;
        if (_stunCooldownTimer > 0f) _stunCooldownTimer -= Time.deltaTime;
        if (_visionMemoryTimer > 0f) _visionMemoryTimer -= Time.deltaTime;

        if (_frozenAtLocker)
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
            _agent.ResetPath();
        }

        TrackPlayerHiding();

        // ─── Ejecutar comportamiento del estado actual con if/else ──────────
        if (_state == State.Wander)
        {
            UpdateWander();
        }
        else if (_state == State.Chase)
        {
            UpdateChase();
        }
        else if (_state == State.Attack)
        {
            UpdateAttack();
        }
        else if (_state == State.Stunned)
        {
            UpdateStunned();
        }
        else if (_state == State.Investigate)
        {
            UpdateInvestigate();
        }
        else if (_state == State.CheckLocker)
        {
            UpdateCheckLocker();
        }

        EvaluateTransitions();
    }

    private void EvaluateTransitions()
    {
        if (_state == State.Attack) return;
        if (_state == State.CheckLocker) return;
        if (_state == State.Stunned) return;

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
                _lockerKillTriggered = false;
                _frozenAtLocker = false;
                _lockerMoveTimer = 0f;
                _attackOnCooldown = false;
                ChangeState(State.CheckLocker);
                Debug.Log($"[BT] Cambio a CheckLocker porque el jugador está escondido en {_knownLockerWithPlayer.name}");
            }
            return;
        }

        bool seesPlayer = CanSeePlayer();
        if (seesPlayer)
        {
            _visionMemoryTimer = visionMemoryDuration;
            ChangeState(State.Chase);
            return;
        }

        if (_state == State.Chase && _visionMemoryTimer > 0f)
            return;

        if (_hasPendingNoise && _state != State.Investigate)
        {
            _hasPendingNoise = false;
            StartInvestigation(_pendingNoisePos);
            return;
        }

        if (_state != State.Investigate && _state != State.CheckLocker)
            ChangeState(State.Wander);
    }

    private void UpdateWander()
    {
        _agent.isStopped = false;
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
        _agent.isStopped = false;
        _agent.speed = chaseSpeed;

        Vector3 targetPos = player.position;
        if (NavMesh.SamplePosition(player.position, out NavMeshHit navHit, 5f, NavMesh.AllAreas))
            targetPos = navHit.position;

        _agent.SetDestination(targetPos);

        Vector3 enemyFlat  = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 playerFlat = new Vector3(player.position.x, 0f, player.position.z);
        float dist = Vector3.Distance(enemyFlat, playerFlat);

        if (dist <= attackRange && !_attackOnCooldown)
        {
            ChangeState(State.Attack);
        }
        else if (dist > chaseRange && !CanSeePlayer() && _visionMemoryTimer <= 0f)
        {
            ChangeState(State.Wander);
        }
    }

    private void UpdateAttack()
    {
        _agent.ResetPath();

        if (_anim != null) _anim.SetTrigger(HashAttack);
        _audio?.PlayAttack();

        bool playerHiding = lockerSystem != null && lockerSystem.IsHiding;
        if (!playerHiding)
        {
            DamagePlayer(attackDamage);
        }

        _attackOnCooldown = true;
        Invoke(nameof(ResetAttack), attackCooldownTime);

        _visionMemoryTimer = visionMemoryDuration;
        ChangeState(State.Chase);
    }

    private void ResetAttack() => _attackOnCooldown = false;

    private void UpdateStunned()
    {
        _agent.isStopped = true;
        _agent.ResetPath();

        if (_anim != null) _anim.SetBool(HashStunned, true);

        _stunTimer += Time.deltaTime;
        if (_stunTimer >= stunDuration)
        {
            _isStunned = false;
            _stunTimer = 0f;
            if (_anim != null) _anim.SetBool(HashStunned, false);
            _stunCooldownTimer = stunCooldown;
            ChangeState(State.Wander);
            Debug.Log($"[BT] Recuperado. Cooldown de stun: {stunCooldown}s");
        }
    }

    public void Stun()
    {
        if (_stunCooldownTimer > 0f)
        {
            Debug.Log($"[BT] Stun en cooldown. Quedan {_stunCooldownTimer:F1}s");
            return;
        }

        _isStunned = true;
        _stunTimer = 0f;
        _audio?.StopAll();
        Debug.Log("[BT] ¡Aturdido por la linterna!");
    }

    public bool CanBeStunned => _stunCooldownTimer <= 0f && !_isStunned;

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

        _agent.isStopped = false;
        _agent.speed = investigateSpeed;
        _agent.SetDestination(_investigateTarget);

        ChangeState(State.Investigate);
        Debug.Log($"[BT] Investigando en {_investigateTarget}");
    }

    private void UpdateInvestigate()
    {
        if (!_reachedInvestigation)
        {
            _agent.isStopped = false;

            if (!_agent.pathPending && _agent.remainingDistance < 0.6f)
            {
                _reachedInvestigation = true;
                _agent.ResetPath();
                _agent.isStopped = true;

                LockerInteractable locker = FindRandomNearbyLocker();
                if (locker != null)
                {
                    _targetLocker    = locker;
                    _lockerOpened    = false;
                    _lockerWaitTimer = 0f;
                    _lockerKillTriggered = false;
                    _frozenAtLocker  = false;
                    _agent.isStopped = false;
                    _agent.speed     = investigateSpeed;

                    Vector3 frontPos = GetFrontOfLockerPosition(_targetLocker);
                    _agent.SetDestination(frontPos);

                    ChangeState(State.CheckLocker);
                    return;
                }
            }
        }
        else
        {
            _investigateWaitTimer += Time.deltaTime;
            if (_investigateWaitTimer >= investigateWaitTime)
            {
                _investigateCooldownTimer = investigateCooldown;
                ChangeState(State.Wander);
            }
        }
    }

    private Vector3 GetFrontOfLockerPosition(LockerInteractable locker)
    {
        if (locker == null) return transform.position;

        Vector3 frontDir = locker.transform.forward;
        Vector3 frontPos = locker.transform.position + frontDir * lockerApproachDistance;

        if (NavMesh.SamplePosition(frontPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            return hit.position;

        return frontPos;
    }

    private void UpdateCheckLocker()
    {
        if (_targetLocker == null)
        {
            _frozenAtLocker = false;
            ChangeState(State.Wander);
            return;
        }

        Vector3 frontPos = GetFrontOfLockerPosition(_targetLocker);
        float distToFront = Vector3.Distance(transform.position, frontPos);

        if (!_lockerOpened)
        {
            _agent.isStopped = false;
            _frozenAtLocker = false;
            _agent.SetDestination(frontPos);
            _lockerMoveTimer += Time.deltaTime;

            if (_lockerMoveTimer > _MAX_LOCKER_MOVE_TIME || _agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                Debug.LogWarning("[BT] No se pudo alcanzar la taquilla.");
                _knownLockerWithPlayer = null;
                _targetLocker = null;
                ChangeState(State.Wander);
                return;
            }

            if (distToFront < 0.5f)
            {
                _lockerOpened = true;
                _lockerWaitTimer = 0f;
                _lockerMoveTimer = 0f;

                _frozenAtLocker = true;
                _agent.ResetPath();
                _agent.isStopped = true;
                _agent.velocity = Vector3.zero;

                Vector3 snapPos = frontPos;
                snapPos.y = transform.position.y;
                _agent.Warp(snapPos);

                Vector3 dirToLocker = (_targetLocker.transform.position - transform.position).normalized;
                dirToLocker.y = 0f;
                if (dirToLocker != Vector3.zero)
                {
                    Quaternion lookRot = Quaternion.LookRotation(dirToLocker);
                    if (ghostModel != null) ghostModel.rotation = lookRot;
                    transform.rotation = lookRot;
                }

                if (_anim != null) _anim.SetTrigger(HashOpen);
                _targetLocker.OpenDoor();
            }
        }
        else
        {
            _lockerWaitTimer += Time.deltaTime;

            if (_lockerWaitTimer >= lockerOpenWait)
            {
                bool playerInside = lockerSystem != null
                                 && lockerSystem.IsHiding
                                 && lockerSystem.CurrentLocker == _targetLocker;

                if (playerInside && !_lockerKillTriggered)
                {
                    _lockerKillTriggered = true;
                    if (_anim != null) _anim.SetTrigger(HashAttack);
                    _audio?.PlayAttack();
                    Invoke(nameof(KillPlayerInLocker), lockerKillDelay);
                    return;
                }

                if (!playerInside && !_lockerKillTriggered)
                {
                    _targetLocker.CloseDoor();

                    _frozenAtLocker = false;
                    _knownLockerWithPlayer = null;
                    _targetLocker = null;
                    _lockerOpened = false;
                    _investigateCooldownTimer = investigateCooldown;
                    ChangeState(State.Wander);
                }
            }
        }
    }

    private void KillPlayerInLocker()
    {
        if (PlayerStateMachine.Instance != null)
            PlayerStateMachine.Instance.TakeDamage(99999f);

        _frozenAtLocker = false;
        _knownLockerWithPlayer = null;
        _targetLocker = null;
        _lockerOpened = false;
        _investigateCooldownTimer = investigateCooldown;
        ChangeState(State.Wander);
    }

    private void TrackPlayerHiding()
    {
        if (lockerSystem == null) return;

        bool hidingNow = lockerSystem.IsHiding;

        if (hidingNow && !_playerWasHiding)
        {
            bool nearby = Vector3.Distance(transform.position, player.position) < detectionRange * 0.6f;

            if (CanSeePlayerRaw() || _state == State.Chase || nearby)
            {
                _knownLockerWithPlayer = lockerSystem.CurrentLocker;
                Debug.Log($"[BT] Vi/intuí entrar al jugador en: {_knownLockerWithPlayer?.name}");
            }
        }

        if (!hidingNow && _playerWasHiding)
        {
            _knownLockerWithPlayer = null;
        }

        _playerWasHiding = hidingNow;
    }

    private bool CanSeePlayer()
    {
        if (player == null) return false;
        if (lockerSystem != null && lockerSystem.IsHiding) return false;
        if (PlayerStateMachine.Instance != null && !PlayerStateMachine.Instance.IsAlive) return false;
        return CanSeePlayerRaw();
    }

    private bool CanSeePlayerRaw()
    {
        if (player == null) return false;
        if (eyes == null) return false;

        Vector3[] testPoints = new Vector3[]
        {
            player.position + Vector3.up * 0.3f,
            player.position + Vector3.up * 1.0f,
            player.position + Vector3.up * 1.7f
        };

        foreach (Vector3 targetPoint in testPoints)
        {
            float dist = Vector3.Distance(eyes.position, targetPoint);
            if (dist > detectionRange) continue;

            Vector3 dir = (targetPoint - eyes.position).normalized;
            if (Vector3.Angle(eyes.forward, dir) > detectionAngle * 0.5f) continue;

            if (Physics.Raycast(eyes.position, dir, out RaycastHit hit, dist, visionBlockMask))
            {
                if (!hit.collider.transform.IsChildOf(player) && hit.collider.transform != player)
                    continue;
            }

            return true;
        }

        return false;
    }

    private void DamagePlayer(float damage)
    {
        if (PlayerStateMachine.Instance != null)
            PlayerStateMachine.Instance.TakeDamage(damage);
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

        _previousState = _state;
        _state = newState;

        if (newState == State.Chase
            && _previousState != State.Chase
            && _previousState != State.Attack)
        {
            _audio?.PlayAlert();
        }

        Debug.Log($"[BT] → {newState}");

        if (_anim != null)
            _anim.SetInteger(HashState, (int)newState);
    }

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

        if (_targetLocker != null)
        {
            Gizmos.color = Color.green;
            Vector3 frontPos = _targetLocker.transform.position + _targetLocker.transform.forward * lockerApproachDistance;
            Gizmos.DrawSphere(frontPos, 0.2f);
            Gizmos.DrawLine(transform.position, frontPos);
        }
    }

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
            // Reescrito sin switch
            if (_state == State.Wander)
            {
                return patrolPoints.Length > 0 && _patrolIndex < patrolPoints.Length
                    ? $"Punto {_patrolIndex}"
                    : "Sin puntos";
            }
            else if (_state == State.Chase)       return "Jugador";
            else if (_state == State.Attack)      return "Atacando";
            else if (_state == State.Stunned)     return "Aturdido";
            else if (_state == State.Investigate) return _reachedInvestigation ? "Esperando" : "Yendo a investigar";
            else if (_state == State.CheckLocker) return _targetLocker != null ? _targetLocker.name : "Ninguna";

            return "";
        }
    }
}