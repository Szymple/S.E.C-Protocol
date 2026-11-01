using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// S.E.C. Protocol - WaveManager (sistema de oleadas)
//   - Oleadas infinitas con la fórmula E(n) = 10 + (n-1) + piso((n-1)/2) + R
//   - Máximo 10 enemigos vivos a la vez (el resto espera en la cola)
//   - +2 HP a los enemigos cada 2 oleadas (Health.SetMaxHealth)
//   - Victoria al completar la oleada 10 (evento OnVictory)
//   - Cuenta muertes escuchando Health.OnDeath (en vez de buscar por tag)

public class WaveManager : MonoBehaviour
{
    public enum SpawnState { SPAWNING, WAITING, COUNTING, STOPPED }

    [Header("Enemigos")]
    [Tooltip("Prefab del enemigo (Soldado)")]
    public GameObject enemyPrefab;

    [Tooltip("Puntos de aparición (deben estar sobre el NavMesh azul)")]
    public Transform[] spawnPoints;

    [Tooltip("Enemigos por segundo al aparecer")]
    public float spawnRate = 1f;

    [Tooltip("Máximo de enemigos vivos a la vez (GDD: 10)")]
    public int maxAlive = 10;

    [Header("Oleadas (GDD)")]
    [Tooltip("Descanso entre oleadas en segundos (GDD: 5)")]
    public float timeBetweenWaves = 5f;

    [Tooltip("Al completar esta oleada se gana")]
    public int victoryWave = 10;

    [Tooltip("Vida extra de los enemigos cada 2 oleadas (GDD: 2)")]
    public float healthIncrease = 2f;

    [Header("Avisos (eventos)")]
    public UnityEvent<int> OnWaveStarted;    // manda el número de oleada
    public UnityEvent<int> OnWaveCompleted;  // manda el número de oleada
    public UnityEvent OnVictory;
    public UnityEvent OnEnemyKilled;          // NUEVO (stats): cada vez que muere un enemigo

    [Header("Debug (solo para ver)")]
    [SerializeField] private SpawnState state = SpawnState.COUNTING;
    [SerializeField] private int currentWave = 0;
    [SerializeField] private int enemiesInWave;
    [SerializeField] private int spawnedThisWave;
    [SerializeField] private int killedThisWave;
    [SerializeField] private float currentEnemyHealth;
    [SerializeField] private bool hasWon;

    private float waveCountdown;
    private int nextWave = 1;
    private int enemiesInNextWave;
    private readonly List<Health> aliveEnemies = new List<Health>();

    // Datos para el HUD y el GameManager
    public SpawnState State => state;
    public int CurrentWave => currentWave;
    public int NextWave => nextWave;
    public int EnemiesRemaining => enemiesInWave - killedThisWave;
    public float WaveCountdown => waveCountdown;
    public bool HasWon => hasWon;

    private void Start()
    {
        // Vida base = la que tenga el prefab del enemigo
        currentEnemyHealth = enemyPrefab.GetComponent<Health>().MaxHealth;

        // Preparar la primera oleada
        nextWave = 1;
        enemiesInNextWave = CalculateEnemyCount(nextWave);
        waveCountdown = timeBetweenWaves;

        // Si el jugador muere, se detienen las oleadas
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            Health playerHealth = player.GetComponent<Health>();
            if (playerHealth != null) playerHealth.OnDeath.AddListener(StopWaves);
        }
    }

    // Máquina de estados
    private void Update()
    {
        if (state == SpawnState.STOPPED) return;

        if (state == SpawnState.WAITING)
        {
            // ¿Ya mataron a todos los de esta oleada?
            if (killedThisWave >= enemiesInWave)
                WaveCompleted();
            else
                return;
        }

        if (waveCountdown <= 0f)
        {
            if (state != SpawnState.SPAWNING)
                StartCoroutine(SpawnWave());
        }
        else
        {
            waveCountdown -= Time.deltaTime;
        }
    }

    // E(n) = 10 + (n-1) + piso((n-1)/2) + R,  R al azar entre 0 y 2 (GDD)
    private int CalculateEnemyCount(int n)
    {
        int r = Random.Range(0, 3);
        return 10 + (n - 1) + (n - 1) / 2 + r;
    }

    // Hacer aparecer la oleada de a uno (Brackeys) con máximo de vivos (GDD)
    private IEnumerator SpawnWave()
    {
        state = SpawnState.SPAWNING;

        currentWave = nextWave;
        enemiesInWave = enemiesInNextWave;
        spawnedThisWave = 0;
        killedThisWave = 0;
        OnWaveStarted.Invoke(currentWave);
        Debug.Log($"Oleada {currentWave}: {enemiesInWave} enemigos con {currentEnemyHealth} HP");

        while (spawnedThisWave < enemiesInWave)
        {
            // Si ya hay el máximo vivo, esperar a que muera alguno
            while (aliveEnemies.Count >= maxAlive)
                yield return null;

            SpawnEnemy();
            yield return new WaitForSeconds(1f / spawnRate);
        }

        state = SpawnState.WAITING;
    }

    private void SpawnEnemy()
    {
        // Punto de aparición al azar
        Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject enemy = Instantiate(enemyPrefab, sp.position, sp.rotation);

        // Vida de esta oleada y escuchar su muerte
        Health health = enemy.GetComponent<Health>();
        health.SetMaxHealth(currentEnemyHealth);
        health.OnDeath.AddListener(() => OnEnemyDied(health));

        aliveEnemies.Add(health);
        spawnedThisWave++;
    }

    private void OnEnemyDied(Health enemy)
    {
        aliveEnemies.Remove(enemy);
        killedThisWave++;
        OnEnemyKilled.Invoke(); // NUEVO (stats): avisar que murió un enemigo
    }

    // Fin de la oleada: aquí sube la dificultad
    private void WaveCompleted()
    {
        Debug.Log($"Oleada {currentWave} completada");
        OnWaveCompleted.Invoke(currentWave);

        state = SpawnState.COUNTING;
        waveCountdown = timeBetweenWaves;

        // Dificultad: +2 HP cada 2 oleadas (GDD)
        if (currentWave % 2 == 0)
            currentEnemyHealth += healthIncrease;

        // Preparar la siguiente oleada con la fórmula
        nextWave = currentWave + 1;
        enemiesInNextWave = CalculateEnemyCount(nextWave);

        // Victoria al completar la oleada 10
        if (currentWave == victoryWave && !hasWon)
        {
            hasWon = true;
            state = SpawnState.STOPPED;
            OnVictory.Invoke();
            Debug.Log("¡VICTORIA!");
        }
    }

    // Para el botón "Seguir jugando" de la pantalla de victoria
    public void ContinueEndless()
    {
        if (!hasWon) return;
        state = SpawnState.COUNTING;
        waveCountdown = timeBetweenWaves;
    }

    private void StopWaves()
    {
        state = SpawnState.STOPPED;
        StopAllCoroutines();
    }

    // Botones de prueba: menú ⋮ del componente (en Play)
    [ContextMenu("Prueba: matar a todos los enemigos vivos")]
    private void DebugKillAll()
    {
        foreach (Health enemy in aliveEnemies.ToArray())
            enemy.TakeDamage(enemy.CurrentHealth);
    }

    [ContextMenu("Prueba: saltar a la oleada 9")]
    private void DebugJumpToWave9()
    {
        nextWave = 9;
        enemiesInNextWave = CalculateEnemyCount(nextWave);
    }

    [ContextMenu("Prueba: seguir jugando después de ganar")]
    private void DebugContinue()
    {
        ContinueEndless();
    }

    [ContextMenu("Prueba: ganar ya")]
    private void DebugWinNow()
    {
        StopAllCoroutines();                       

        foreach (Health enemy in aliveEnemies.ToArray())
            enemy.TakeDamage(enemy.CurrentHealth); 

        currentWave = victoryWave;                 
        WaveCompleted();                           
    }
}