using UnityEngine;

public class WeaponSwitching : MonoBehaviour
{
    [Tooltip("Arma con la que empieza: 0 = primer hijo")]
    public int selectedWeapon = 0;

    // El arma activa (la leerá el HUD)
    public Gun CurrentGun { get; private set; }

    private void Start()
    {
        SelectWeapon();
    }

    private void Update()
    {
        int previousSelectedWeapon = selectedWeapon;

        // Rueda del mouse 
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
        {
            if (selectedWeapon >= transform.childCount - 1) selectedWeapon = 0;
            else selectedWeapon++;
        }
        if (scroll < 0f)
        {
            if (selectedWeapon <= 0) selectedWeapon = transform.childCount - 1;
            else selectedWeapon--;
        }

        // Teclas numéricas
        if (Input.GetKeyDown(KeyCode.Alpha1)) selectedWeapon = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2) && transform.childCount >= 2) selectedWeapon = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3) && transform.childCount >= 3) selectedWeapon = 2;

        // Cambio automático si el arma actual no tiene balas ni reserva
        if (CurrentGun != null && CurrentGun.IsOutOfAmmo)
        {
            selectedWeapon = FindInfiniteWeapon(selectedWeapon);
        }

        if (previousSelectedWeapon != selectedWeapon)
        {
            SelectWeapon();
        }
    }

    // Activar solo el arma elegida
    private void SelectWeapon()
    {
        int i = 0;
        foreach (Transform weapon in transform)
        {
            bool isSelected = (i == selectedWeapon);
            weapon.gameObject.SetActive(isSelected);

            if (isSelected) CurrentGun = weapon.GetComponent<Gun>();
            i++;
        }
    }

    // Buscar el arma con recarga infinita (pistola)
    private int FindInfiniteWeapon(int fallback)
    {
        int i = 0;
        foreach (Transform weapon in transform)
        {
            Gun gun = weapon.GetComponent<Gun>();
            if (gun != null && gun.infiniteReserve) return i;
            i++;
        }
        return fallback;
    }
}