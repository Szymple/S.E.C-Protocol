using UnityEngine;

// S.E.C. Protocol - HUD DE PRUEBA (temporal).
// Muestra la vida, la stamina, las balas y la oleada en la esquina de la pantalla.
// Solo para probar mientras no existe el HUD real. Se borra al final.
public class DebugHUD : MonoBehaviour
{
    [Tooltip("Si se deja vacío, lo busca solo por el tag Player")]
    public Health playerHealth;

    [Tooltip("Si se deja vacío, lo busca solo por el tag Player")]
    public PlayerController playerController;

    private GUIStyle _style;

    // NUEVO: armas y munición
    private WeaponSwitching _weapons;
    private PlayerAmmo _ammo;

    // NUEVO (oleadas): el WaveManager de la escena
    private WaveManager _waves;

    private void Start()
    {
        // Buscar al jugador por su etiqueta (tag) y tomar sus componentes
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            if (playerHealth == null) playerHealth = player.GetComponent<Health>();
            if (playerController == null) playerController = player.GetComponent<PlayerController>();

            // NUEVO: el WeaponSwitching está en un hijo (WeaponHolder, en la mano)
            // y PlayerAmmo está en el Player
            _weapons = player.GetComponentInChildren<WeaponSwitching>();
            _ammo = player.GetComponent<PlayerAmmo>();
        }
        else
        {
            Debug.LogWarning("DebugHUD: no encontré ningún objeto con el tag Player.");
        }

        // NUEVO (oleadas): buscar el WaveManager en la escena
        _waves = FindObjectOfType<WaveManager>();
    }

    private void OnGUI()
    {
        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label);
            _style.fontSize = 22;
            _style.normal.textColor = Color.white;
        }

        // NUEVO (oleadas): área más alta para que quepa la línea de la oleada
        GUILayout.BeginArea(new Rect(20, 20, 600, 300));

        if (playerHealth != null)
        {
            string estado = playerHealth.IsDead ? "  (MUERTO)" : "";
            GUILayout.Label($"Vida: {playerHealth.CurrentHealth:0} / {playerHealth.MaxHealth:0}{estado}", _style);
        }

        if (playerController != null)
        {
            string agotado = playerController.IsExhausted ? "  (agotado)" : "";
            GUILayout.Label($"Stamina: {playerController.StaminaNormalized * 100f:0}%{agotado}", _style);
            GUILayout.Label($"Agachado: {(playerController.IsCrouching ? "sí" : "no")}", _style);
        }

        // NUEVO: arma actual y balas (cargador / reserva)
        if (_weapons != null && _weapons.CurrentGun != null)
        {
            Gun gun = _weapons.CurrentGun;
            string reserva = gun.infiniteReserve ? "∞" : (_ammo != null ? _ammo.Reserve.ToString() : "0");
            string recargando = gun.IsReloading ? "  (recargando...)" : "";
            GUILayout.Label($"Arma: {gun.weaponName}   Balas: {gun.CurrentAmmo} / {reserva}{recargando}", _style);
        }

        // NUEVO (oleadas): victoria, cuenta regresiva o enemigos restantes
        if (_waves != null)
        {
            string texto;
            if (_waves.HasWon && _waves.State == WaveManager.SpawnState.STOPPED)
                texto = "¡VICTORIA! Oleada 10 completada";
            else if (_waves.State == WaveManager.SpawnState.COUNTING)
                texto = $"Oleada {_waves.NextWave} en {Mathf.CeilToInt(_waves.WaveCountdown)}...";
            else
                texto = $"Oleada {_waves.CurrentWave}   Enemigos restantes: {_waves.EnemiesRemaining}";
            GUILayout.Label(texto, _style);
        }

        GUILayout.EndArea();
    }
}