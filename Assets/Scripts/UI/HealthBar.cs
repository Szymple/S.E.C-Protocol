using UnityEngine;
using UnityEngine.UI;
public class HealthBar : MonoBehaviour
{
    [Tooltip("La barra (Slider, de 0 a 1)")]
    [SerializeField] private Slider slider;

    [Tooltip("Activado: flota y mira a la cámara (enemigos). Desactivado: barra fija del HUD (jugador)")]
    [SerializeField] private bool faceCamera = true;

    [Tooltip("Ocultar la barra cuando la vida llega a 0")]
    [SerializeField] private bool hideWhenEmpty = true;

    private Camera cam;

    private void Awake()
    {
        if (slider == null) slider = GetComponentInChildren<Slider>();
        cam = Camera.main;
    }

    // Conectar con el "+" a Health.OnHealthChanged 
    public void UpdateHealthBar(float currentValue, float maxValue)
    {
        if (slider == null || maxValue <= 0f) return;

        slider.value = currentValue / maxValue;

        if (hideWhenEmpty && currentValue <= 0f)
            gameObject.SetActive(false);
    }

    // Mirar siempre a la cámara
    private void LateUpdate()
    {
        if (faceCamera && cam != null)
            transform.rotation = cam.transform.rotation;
    }
}