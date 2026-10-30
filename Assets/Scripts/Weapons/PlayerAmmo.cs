using UnityEngine;

// La pistola no la usa: su reserva es infinita.
// El orbe amarillo llama a AddAmmo().
public class PlayerAmmo : MonoBehaviour
{
    [Tooltip("Balas de reserva al empezar")]
    [SerializeField] private int reserve = 75;

    [Tooltip("Máximo de balas de reserva")]
    [SerializeField] private int maxReserve = 150;

    public int Reserve => reserve;
    public int MaxReserve => maxReserve;

    // Saca balas de la reserva para recargar. Devuelve cuántas pudo dar.
    public int TakeAmmo(int amount)
    {
        int taken = Mathf.Min(amount, reserve);
        reserve -= taken;
        return taken;
    }

    // Suma balas a la reserva, sin pasarse del máximo (orbe amarillo).
    public void AddAmmo(int amount)
    {
        reserve = Mathf.Min(reserve + amount, maxReserve);
    }
}