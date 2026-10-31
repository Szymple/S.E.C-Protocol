using UnityEngine;
using UnityEngine.SceneManagement;

// =====================================================================
// S.E.C. Protocol - GameManager (estado de la partida)
//
// Basado en Brackeys:
//   - "PAUSE MENU in Unity": pausa con ESC, Time.timeScale, panel con
//     SetActive, botones Reanudar / Menú / Salir.
//   - "GAME OVER - How to make a Video Game in Unity (E08)": GameManager,
//     terminar la partida una sola vez, espera con Invoke, reiniciar con
//     SceneManager.LoadScene(escena actual).
//   - "WINNING LEVELS - How to make a Video Game in Unity (E09)": al ganar,
//     prender el panel de nivel completado.
//
// Adaptado a S.E.C. Protocol:
//   - Estados Playing / Paused / GameOver / Victory (en vez de bool)
//   - Escucha Health.OnDeath (jugador) y WaveManager.OnVictory
//   - "Seguir jugando" después de la victoria (oleadas infinitas)
//   - Al congelar: apaga el PlayerController (cámara) y libera el mouse
// =====================================================================
public class GameManager : MonoBehaviour
{
    public enum GameState { Playing, Paused, GameOver, Victory }

    [Header("Paneles (los arrastra el equipo de UI)")]
    public GameObject pausePanel;
    public GameObject gameOverPanel;
    public GameObject victoryPanel;

    [Tooltip("Opcional: lo que se oculta cuando sale un panel (HUD)")]
    public GameObject hud;

    [Header("Ajustes")]
    [Tooltip("Nombre exacto de la escena del menú principal")]
    public string mainMenuScene = "MainMenu";

    [Tooltip("Segundos entre morir y mostrar el Game Over")]
    public float gameOverDelay = 1f;

    [Header("Debug (solo para ver)")]
    [SerializeField] private GameState state = GameState.Playing;

    private Health playerHealth;
    private PlayerController playerController;
    private PlayerInputs playerInputs;
    private WaveManager waveManager;

    public GameState State => state;

    private void Start()
    {
        Time.timeScale = 1f;
        HideAllPanels();

        // Buscar al jugador y escuchar su muerte
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerHealth = player.GetComponent<Health>();
            playerController = player.GetComponent<PlayerController>();
            playerInputs = player.GetComponent<PlayerInputs>();

            if (playerHealth != null) playerHealth.OnDeath.AddListener(OnPlayerDeath);
        }
        else
        {
            Debug.LogWarning("GameManager: no encontré ningún objeto con el tag Player.");
        }

        // Escuchar la victoria de las oleadas
        waveManager = FindObjectOfType<WaveManager>();
        if (waveManager != null) waveManager.OnVictory.AddListener(OnVictory);

        SetCursorLocked(true);
    }

    // Pausa con ESC 
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (state == GameState.Playing) Pausar();
            else if (state == GameState.Paused) Reanudar();
        }
    }

    // Pausa 
    public void Pausar()
    {
        if (state != GameState.Playing) return;
        state = GameState.Paused;
        Freeze(pausePanel);
    }

    public void Reanudar()
    {
        if (state != GameState.Paused) return;
        state = GameState.Playing;
        Unfreeze();
    }

    // Game Over (Brackeys E08) 
    private void OnPlayerDeath()
    {
        if (state == GameState.GameOver) return;   // solo una vez
        state = GameState.GameOver;

        // El jugador muerto ya no se mueve
        if (playerController != null) playerController.enabled = false;

        // Esperar un momento antes de mostrar el panel
        Invoke(nameof(ShowGameOver), gameOverDelay);
    }

    private void ShowGameOver()
    {
        Freeze(gameOverPanel);
    }

    // Victoria
    private void OnVictory()
    {
        if (state != GameState.Playing) return;
        state = GameState.Victory;
        Freeze(victoryPanel);
    }

    // Botón "Seguir jugando": oleadas infinitas
    public void SeguirJugando()
    {
        if (state != GameState.Victory) return;
        state = GameState.Playing;
        Unfreeze();
        if (waveManager != null) waveManager.ContinueEndless();
    }

    // Botones generales 
    // Reiniciar la escena actual
    public void Reiniciar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Volver al menú principal 
    public void IrAlMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }

    // Salir del juego. En el editor no cierra nada.
    public void Salir()
    {
        Debug.Log("Salir del juego");
        Application.Quit();
    }

    // Congelar / descongelar 
    private void Freeze(GameObject panel)
    {
        Time.timeScale = 0f;
        if (playerController != null) playerController.enabled = false;

        HideAllPanels();
        if (panel != null) panel.SetActive(true);
        if (hud != null) hud.SetActive(false);

        SetCursorLocked(false);
    }

    private void Unfreeze()
    {
        Time.timeScale = 1f;
        if (playerController != null) playerController.enabled = true;

        HideAllPanels();
        if (hud != null) hud.SetActive(true);

        SetCursorLocked(true);
    }

    private void HideAllPanels()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
    }

    // Bloquear juego o liberar menús el mouse
    private void SetCursorLocked(bool locked)
    {
        if (playerInputs != null)
        {
            playerInputs.cursorLocked = locked;   // para que no se vuelva a bloquear solo
            playerInputs.SetCursorState(locked);
        }
        else
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        }
    }
}