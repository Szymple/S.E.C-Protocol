using UnityEngine;

// Ajustes de cada obstáculo (los lee el ArenaGenerator)
public class ObstacleSettings : MonoBehaviour
{
    [Tooltip("¿Se le cambia la altura al azar?")]
    public bool randomHeight = true;

    [Tooltip("Multiplicador mínimo de la altura original (1 = tal cual)")]
    public float minHeight = 0.8f;

    [Tooltip("Multiplicador máximo de la altura original")]
    public float maxHeight = 1.5f;

    [Tooltip("¿Se puede girar al azar (0, 90, 180 o 270 grados)?")]
    public bool randomRotation = true;
}