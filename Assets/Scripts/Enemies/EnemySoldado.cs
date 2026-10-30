using System.Collections; // NUEVO: para la corrutina del láser
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(LineRenderer))] // NUEVO: el láser
public class EnemySoldado : MonoBehaviour
{
    [Header("Movimiento")]
    [Tooltip("Velocidad caminando (paso semi lento). El jugador camina a 2 y corre a 5.3")]
    public float moveSpeed = 2.5f;

    [Tooltip("Si ve al jugador a esta distancia o menos, se detiene (media distancia)")]
    public float stopDistance = 10f;

    [Tooltip("Qué tan rápido gira hacia el jugador cuando está quieto")]
    public float turnSpeed = 6f;

    [Header("Disparo")]
    [Tooltip("Distancia máxima a la que dispara")]
    public float attackRange = 15f;

    [Tooltip("Daño de cada bala")]
    public float damagePerBullet = 5f;

    [Tooltip("Balas por cargador")]
    public int magazineSize = 10;

    [Tooltip("Segundos entre bala y bala")]
    public float timeBetweenShots = 0.25f;

    [Tooltip("Segundos que tarda en recargar")]
    public float reloadTime = 2f;

    [Tooltip("Margen de error. 0 = nunca falla, 0.2 = falla mucho")]
    [Range(0f, 0.3f)]
    public float inaccuracy = 0.06f;

    [Tooltip("Desde dónde salen las balas (el hijo FirePoint)")]
    public Transform firePoint;

    [Tooltip("Con qué capas chocan sus balas. Quitar la capa Enemy")]
    public LayerMask hitMask = ~0;

    // NUEVO: láser
    [Header("Láser")]
    [Tooltip("Cuánto tiempo se ve el láser")]
    public float shotDuration = 0.07f;

    [Header("Muerte")]
    [Tooltip("Segundos antes de desaparecer al morir")]
    public float deathDelay = 0.3f;

    // ESTADO de la IA: lo que intenta hacer ahora mismo
    [Header("Debug (solo para ver)")]
    [SerializeField] private State _state = State.Chase;
    [SerializeField] private int _ammo;
    [SerializeField] private bool _isReloading;

    private enum State { Chase, Attack, Dead }

    // componentes propios
    private NavMeshAgent _agent;
    private Health _health;
    private LineRenderer _laserLine;            // NUEVO: el láser
    private WaitForSeconds _shotDurationWait;   // NUEVO: cuánto se ve el láser

    // jugador
    private Transform _player;
    private Collider _playerCollider;
    private Health _playerHealth;

    // temporizadores
    private float _shotTimer;
    private float _reloadTimer;
    private float _repathTimer;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _health = GetComponent<Health>();

        // NUEVO: tomar el láser y empezar con él apagado
        _laserLine = GetComponent<LineRenderer>();
        _laserLine.enabled = false;
        _shotDurationWait = new WaitForSeconds(shotDuration);

        // Escuchar el "timbre" de muerte de mi propia vida
        _health.OnDeath.AddListener(HandleDeath);
    }

    private void Start()
    {
        _agent.speed = moveSpeed;
        _agent.stoppingDistance = 0f;
        _ammo = magazineSize;

        // Encontrar al jugador UNA vez y guardar la referencia
        
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            _player = playerObj.transform;
            _playerCollider = playerObj.GetComponent<Collider>();
            _playerHealth = playerObj.GetComponent<Health>();
        }
        else
        {
            Debug.LogWarning("EnemySoldado: no encontré al jugador (tag Player).");
        }
    }

    private void Update()
    {
        if (_state == State.Dead) return;

        // Sin jugador, o jugador muerto: quedarse quieto
        if (_player == null || (_playerHealth != null && _playerHealth.IsDead))
        {
            StopMoving();
            return;
        }

        // ENTRADAS: lo que hace cambiar de estado 
        float distance = Vector3.Distance(transform.position, _player.position);
        bool canSeePlayer = distance <= attackRange && HasLineOfSight();

        // ACCIONES
        UpdateMovement(distance, canSeePlayer);
        UpdateShooting(canSeePlayer);
    }

    // MOVIMIENTO (con obstáculos hace falta pathfinding: NavMesh)

    private void UpdateMovement(float distance, bool canSeePlayer)
    {
        if (canSeePlayer && distance <= stopDistance)
        {
            _state = State.Attack;
            StopMoving();
            FacePlayer();
        }
        else
        {
            _state = State.Chase;
            ChasePlayer();
        }
    }

    private void ChasePlayer()
    {
        if (!_agent.isOnNavMesh) return;
        _agent.isStopped = false;

        // recalcular el camino cada 0.2 s (no en cada frame, para ahorrar)
        _repathTimer -= Time.deltaTime;
        if (_repathTimer <= 0f)
        {
            _agent.SetDestination(_player.position);
            _repathTimer = 0.2f;
        }
    }

    private void StopMoving()
    {
        if (_agent.isOnNavMesh) _agent.isStopped = true;
    }

    private void FacePlayer()
    {
        Vector3 dir = _player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;

        Quaternion target = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * turnSpeed);
    }

    // DISPARO Y RECARGA

    private void UpdateShooting(bool canSeePlayer)
    {
        if (_shotTimer > 0f) _shotTimer -= Time.deltaTime;

        if (_isReloading)
        {
            _reloadTimer -= Time.deltaTime;
            if (_reloadTimer <= 0f)
            {
                _ammo = magazineSize;
                _isReloading = false;
            }
            return;
        }

        if (canSeePlayer && _shotTimer <= 0f)
        {
            Shoot();
            _ammo--;
            _shotTimer = timeBetweenShots;

            if (_ammo <= 0)
            {
                _isReloading = true;
                _reloadTimer = reloadTime;
            }
        }
    }

    private void Shoot()
    {
        Vector3 origin = GetFireOrigin();
        Vector3 direction = (GetPlayerAimPoint() - origin).normalized;

        // Margen de error: punto al azar dentro de una esfera, multiplicado
        // por el margen, para que algunas balas fallen
        direction = (direction + Random.insideUnitSphere * inaccuracy).normalized;

        // NUEVO: el láser empieza en el FirePoint y se muestra un instante
        _laserLine.SetPosition(0, origin);
        StartCoroutine(ShotEffect());

        if (Physics.Raycast(origin, direction, out RaycastHit hit, attackRange, hitMask, QueryTriggerInteraction.Ignore))
        {
            Debug.DrawLine(origin, hit.point, Color.red, 0.2f);
            _laserLine.SetPosition(1, hit.point); // NUEVO: el láser termina donde pegó

            if (IsPlayer(hit.transform) && _playerHealth != null)
            {
                _playerHealth.TakeDamage(damagePerBullet);
            }
        }
        else
        {
            Debug.DrawRay(origin, direction * attackRange, Color.yellow, 0.2f);
            _laserLine.SetPosition(1, origin + direction * attackRange); // NUEVO: si no pegó, llega hasta el alcance
        }
    }

    // NUEVO: mostrar el láser un instante y apagarlo
    private IEnumerator ShotEffect()
    {
        _laserLine.enabled = true;
        yield return _shotDurationWait;
        _laserLine.enabled = false;
    }

    // ¿Hay línea directa hacia el jugador (nada en medio)?
    private bool HasLineOfSight()
    {
        Vector3 origin = GetFireOrigin();
        Vector3 toPlayer = GetPlayerAimPoint() - origin;

        if (Physics.Raycast(origin, toPlayer.normalized, out RaycastHit hit, toPlayer.magnitude + 0.5f, hitMask, QueryTriggerInteraction.Ignore))
        {
            return IsPlayer(hit.transform);
        }
        return false;
    }

    private bool IsPlayer(Transform t)
    {
        return t == _player || t.IsChildOf(_player);
    }

    private Vector3 GetFireOrigin()
    {
        return firePoint != null ? firePoint.position : transform.position + Vector3.up * 1.4f;
    }

    private Vector3 GetPlayerAimPoint()
    {
        // centro de la hitbox del jugador (no sus pies)
        if (_playerCollider != null) return _playerCollider.bounds.center;
        return _player.position + Vector3.up;
    }

    // MUERTE (escucha el OnDeath de Health)

    private void HandleDeath()
    {
        _state = State.Dead;
        StopMoving();
        _agent.enabled = false;
        _laserLine.enabled = false; // NUEVO: apagar el láser al morir

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, deathDelay);
    }

    // Círculos en la ventana Scene al seleccionar el soldado:
    // amarillo = dónde se detiene, rojo = hasta dónde dispara
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}