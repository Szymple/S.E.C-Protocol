using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Gun : MonoBehaviour
{
    [Header("Disparo")]
    public string weaponName = "Rifle";
    public float damage = 20f;
    public float range = 100f;

    [Tooltip("Balas por segundo al MANTENER el clic")]
    public float fireRate = 1f;

    [Tooltip("¿Dispara sola al mantener el clic?")]
    public bool canHoldToFire = true;

    [Tooltip("Tiempo mínimo entre clics")]
    public float minTimeBetweenClicks = 0.15f;

    [Tooltip("Con qué capas choca el disparo. Quitar la capa Player")]
    public LayerMask hitMask = ~0;

    [Header("Munición")]
    [Tooltip("Tamaño del cargador")]
    public int maxAmmo = 25;
    public float reloadTime = 1.5f;

    [Tooltip("Activado: recarga infinita (pistola). Desactivado: usa la reserva compartida")]
    public bool infiniteReserve = false;

    [Header("Láser")]
    [Tooltip("La punta del arma (el hijo Muzzle)")]
    public Transform gunEnd;

    [Tooltip("Cuánto tiempo se ve el láser")]
    public float shotDuration = 0.07f;

    [Header("Debug (solo para ver)")]
    [SerializeField] private int currentAmmo = -1;
    [SerializeField] private bool isReloading = false;

    // Datos para el HUD y el cambio de arma
    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => maxAmmo;
    public bool IsReloading => isReloading;
    public bool IsOutOfAmmo => currentAmmo <= 0 && !HasReserve();

    private Camera cam;
    private PlayerAmmo playerAmmo;
    private LineRenderer laserLine;
    private WaitForSeconds shotDurationWait;
    private float nextTimeToFire = 0f;
    private float nextClickTime = 0f;

    private void Awake()
    {
        cam = Camera.main;
        playerAmmo = GetComponentInParent<PlayerAmmo>();
        laserLine = GetComponent<LineRenderer>();
        shotDurationWait = new WaitForSeconds(shotDuration);

        // -1 = cargador sin llenar todavía 
        if (currentAmmo == -1) currentAmmo = maxAmmo;
    }

    private void OnEnable()
    {
        // Si se cambió de arma a mitad de la recarga, se cancela
        isReloading = false;
        if (laserLine != null) laserLine.enabled = false;
    }

    private void Update()
    {
        if (isReloading) return;

        // RECARGA
        bool reloadPressed = Input.GetKeyDown(KeyCode.R);
        bool magazineNotFull = currentAmmo < maxAmmo;

        if ((currentAmmo <= 0 || (reloadPressed && magazineNotFull)) && HasReserve())
        {
            StartCoroutine(Reload());
            return;
        }

        if (currentAmmo <= 0) return; // sin balas y sin reserva

        // DISPARO 
        // Clic izquierdo (no "Fire1", porque "Fire1" también responde a Ctrl = agacharse)
        bool pressed = Input.GetMouseButtonDown(0);
        bool held = Input.GetMouseButton(0);

        if (pressed && Time.time >= nextClickTime)
        {
            // Un clic = una bala
            Shoot();
            nextClickTime = Time.time + minTimeBetweenClicks;
            nextTimeToFire = Time.time + 1f / fireRate;
        }
        else if (canHoldToFire && held && !pressed && Time.time >= nextTimeToFire)
        {
            // Mantener = fireRate balas por segundo (Brackeys)
            Shoot();
            nextTimeToFire = Time.time + 1f / fireRate;
        }
    }

    private void Shoot()
    {
        currentAmmo--;
        StartCoroutine(ShotEffect());

        // PASO 1: apuntar desde la cámara, por el centro de la pantalla
        Ray aimRay = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint;
        if (Physics.Raycast(aimRay, out RaycastHit aimHit, range, hitMask, QueryTriggerInteraction.Ignore))
            targetPoint = aimHit.point;
        else
            targetPoint = aimRay.origin + aimRay.direction * range;

        // PASO 2: disparar desde la punta del arma hacia ese punto
        Vector3 origin = gunEnd != null ? gunEnd.position : transform.position;
        Vector3 direction = (targetPoint - origin).normalized;

        laserLine.SetPosition(0, origin);

        if (Physics.Raycast(origin, direction, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            laserLine.SetPosition(1, hit.point);

            Health target = hit.collider.GetComponentInParent<Health>();
            if (target != null)
            {
                target.TakeDamage(damage);
            }
        }
        else
        {
            laserLine.SetPosition(1, origin + direction * range);
        }
    }

    // Mostrar el láser 
    private IEnumerator ShotEffect()
    {
        laserLine.enabled = true;
        yield return shotDurationWait;
        laserLine.enabled = false;
    }

    // Recargar con espera
    private IEnumerator Reload()
    {
        isReloading = true;
        yield return new WaitForSeconds(reloadTime);

        if (infiniteReserve)
        {
            currentAmmo = maxAmmo;
        }
        else
        {
            int needed = maxAmmo - currentAmmo;
            currentAmmo += playerAmmo.TakeAmmo(needed);
        }

        isReloading = false;
    }

    private bool HasReserve()
    {
        return infiniteReserve || (playerAmmo != null && playerAmmo.Reserve > 0);
    }
}