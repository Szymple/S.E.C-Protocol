using UnityEngine;
using TMPro;

// estadísticas de la partida

public class GameStats : MonoBehaviour
{
    [Header("Dónde mostrar (textos de los paneles)")]
    public TMP_Text gameOverStatsText;
    public TMP_Text victoryStatsText;

    [Header("Debug (solo para ver)")]
    [SerializeField] private int kills;
    [SerializeField] private int shotsFired;
    [SerializeField] private int shotsHit;
    [SerializeField] private float damageTaken;
    [SerializeField] private float timeSurvived;
    [SerializeField] private bool counting = true;

    private Health playerHealth;
    private WaveManager waveManager;

    // Datos para quien los necesite (HUD, menús)
    public int Kills => kills;
    public int ShotsFired => shotsFired;
    public int ShotsHit => shotsHit;
    public float Accuracy => shotsFired > 0 ? (float)shotsHit / shotsFired * 100f : 0f;
    public float DamageTaken => damageTaken;
    public float TimeSurvived => timeSurvived;
    public int WaveReached => waveManager != null ? waveManager.CurrentWave : 0;

    // Eventos estáticos: suscribirse y desuscribirse
    private void OnEnable()
    {
        Gun.OnShotFired += CountShot;
        Gun.OnShotHit += CountHit;
    }

    private void OnDisable()
    {
        Gun.OnShotFired -= CountShot;
        Gun.OnShotHit -= CountHit;
    }

    private void Start()
    {
        // Jugador: daño recibido y muerte
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerHealth = player.GetComponent<Health>();
            if (playerHealth != null)
            {
                playerHealth.OnDamaged.AddListener(CountDamage);
                playerHealth.OnDeath.AddListener(OnPlayerDeath);
            }
        }

        // Oleadas: eliminados y victoria
        waveManager = FindObjectOfType<WaveManager>();
        if (waveManager != null)
        {
            waveManager.OnEnemyKilled.AddListener(CountKill);
            waveManager.OnVictory.AddListener(OnVictory);
        }
    }

    // Reloj: Time.deltaTime es 0 en pausa, así que se detiene solo
    private void Update()
    {
        if (counting) timeSurvived += Time.deltaTime;
    }

    // Contar 
    private void CountShot() { shotsFired++; }
    private void CountHit() { shotsHit++; }
    private void CountKill() { kills++; }
    private void CountDamage(float amount) { damageTaken += amount; }

    // Mostrar
    private void OnPlayerDeath()
    {
        counting = false;
        if (gameOverStatsText != null) gameOverStatsText.text = BuildText();
    }

    private void OnVictory()
    {
        // El reloj sigue si elige "Seguir jugando"
        if (victoryStatsText != null) victoryStatsText.text = BuildText();
    }

    private string BuildText()
    {
        int minutes = (int)(timeSurvived / 60f);
        int seconds = (int)(timeSurvived % 60f);

        return
            $"Oleada alcanzada: {WaveReached}\n" +
            $"Enemigos eliminados: {kills}\n" +
            $"Tiempo: {minutes:00}:{seconds:00}\n" +
            $"Disparos: {shotsFired}   Aciertos: {shotsHit} ({Accuracy:0}%)\n" +
            $"Daño recibido: {damageTaken:0}";
    }

    [ContextMenu("Prueba: ver estadísticas en la Console")]
    private void DebugPrint()
    {
        Debug.Log(BuildText());
    }
}