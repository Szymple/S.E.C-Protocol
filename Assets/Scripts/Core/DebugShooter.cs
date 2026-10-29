using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// S.E.C. Protocol - ARMA DE PRUEBA (temporal, solo para las pruebas).
// Clic izquierdo = raycast desde el centro de la pantalla (la mira).
// Si toca algo con Health, le quita vida.
// Solo para probar mientras no existe el arma real. Se borra al final.
public class DebugShooter : MonoBehaviour
{
    [Tooltip("Daño por disparo (igual que el rifle)")]
    public float damage = 20f;

    [Tooltip("Distancia máxima del disparo (metros)")]
    public float range = 100f;

    [Tooltip("Con qué capas puede chocar. Quitar la capa Player")]
    public LayerMask hitMask = ~0;

    private Camera _cam;

    private void Start()
    {
        _cam = Camera.main; // la cámara con el tag MainCamera
    }

    private void Update()
    {
        if (!FirePressed() || _cam == null) return;

        // Línea desde la cámara que pasa por el centro de la pantalla
        Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            Debug.DrawLine(ray.origin, hit.point, Color.green, 1f);

            Health target = hit.collider.GetComponentInParent<Health>();
            if (target != null)
            {
                target.TakeDamage(damage);
                Debug.Log($"Le pegué a {target.name}: le quedan {target.CurrentHealth:0} de vida");
            }
            else
            {
                Debug.Log($"Le pegué a {hit.collider.name} (no tiene vida)");
            }
        }
        else
        {
            Debug.DrawRay(ray.origin, ray.direction * range, Color.gray, 1f);
        }
    }

    private bool FirePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }
}