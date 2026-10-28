using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [Header("Vida")]
    [Tooltip("Vida máxima. Jugador: 150. Soldado: 50.")]
    [SerializeField] private float maxHealth = 100f;

    [Tooltip("Vida actual (solo para ver en el Inspector)")]
    [SerializeField] private float currentHealth;

    
    [Header("Avisos (eventos)")]
    [Tooltip("Suena cada vez que cambia la vida. Manda: (vida actual, vida máxima)")]
    public UnityEvent<float, float> OnHealthChanged;

    [Tooltip("Suena al recibir daño. Manda: (cantidad de daño)")]
    public UnityEvent<float> OnDamaged;

    [Tooltip("Suena al morir. No manda datos.")]
    public UnityEvent OnDeath;

    // Datos que otros scripts pueden LEER, pero no cambiar directamente
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead { get; private set; }

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Start()
    {
        // Avisar la vida inicial, para que el HUD arranque con la barra llena.
        OnHealthChanged.Invoke(currentHealth, maxHealth);
    }

    
    // RECIBIR DAÑO
    // Igual que el TakeDamage(float damage) del ejemplo PlayerHealth del
    // artículo, pero sin dejar que la vida baje de 0.
    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        currentHealth = Mathf.Max(currentHealth - amount, 0f);

        // Llamar a un Unity Event sin suscriptores no da error,
        // así que no hace falta revisar si está vacío (French, 2021).
        OnDamaged.Invoke(amount);
        OnHealthChanged.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    
    // CURAR (para el orbe verde). No pasa del máximo.
  
    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        OnHealthChanged.Invoke(currentHealth, maxHealth);
    }

    // Curar un porcentaje de la vida máxima. 0.1 = 10%.
    public void HealPercent(float percent)
    {
        Heal(maxHealth * percent);
    }

    // CAMBIAR LA VIDA MÁXIMA (para las oleadas: +2 HP cada 2 oleadas)

    public void SetMaxHealth(float newMax, bool refill = true)
    {
        maxHealth = Mathf.Max(1f, newMax);

        if (refill) currentHealth = maxHealth;
        else currentHealth = Mathf.Min(currentHealth, maxHealth);

        OnHealthChanged.Invoke(currentHealth, maxHealth);
    }


    // MORIR
    // Como el "On Player Death" del artículo: solo avisa. Cada observador
    // decide qué hacer (el soldado se destruye, el jugador muestra Game Over).
 
    private void Die()
    {
        IsDead = true;
        OnDeath.Invoke();
    }

    // ---- Botones de prueba: menú ⋮ del componente en el Inspector (en Play) ----
    [ContextMenu("Prueba: recibir 10 de daño")]
    private void DebugDamage()
    {
        TakeDamage(10f);
    }

    [ContextMenu("Prueba: morir")]
    private void DebugKill()
    {
        TakeDamage(currentHealth);
    }
}