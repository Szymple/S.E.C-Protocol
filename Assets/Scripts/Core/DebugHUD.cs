using UnityEngine;

// S.E.C. Protocol - HUD DE PRUEBA (temporal).
// Muestra la vida y la stamina del jugador en la esquina de la pantalla.
// Solo para probar mientras no existe el HUD real. Se borra al final.
public class DebugHUD : MonoBehaviour
{
    [Tooltip("Si se deja vacío, lo busca solo por el tag Player")]
    public Health playerHealth;

    [Tooltip("Si se deja vacío, lo busca solo por el tag Player")]
    public PlayerController playerController;

    private GUIStyle _style;

    private void Start()
    {
        // Buscar al jugador por su etiqueta (tag) y tomar sus componentes
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            if (playerHealth == null) playerHealth = player.GetComponent<Health>();
            if (playerController == null) playerController = player.GetComponent<PlayerController>();
        }
        else
        {
            Debug.LogWarning("DebugHUD: no encontré ningún objeto con el tag Player.");
        }
    }

    private void OnGUI()
    {
        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label);
            _style.fontSize = 22;
            _style.normal.textColor = Color.white;
        }

        GUILayout.BeginArea(new Rect(20, 20, 500, 200));

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

        GUILayout.EndArea();
    }
}