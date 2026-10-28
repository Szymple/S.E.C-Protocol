using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// S.E.C. Protocol - Entradas del jugador (teclado y mouse).
// Basado en StarterAssetsInputs de Unity Starter Assets.
// Cambio: se agrega el poder agacharse (crouch).
//
// Este script NO mueve al jugador: solo guarda qué teclas está presionando.
// El componente PlayerInput llama a OnMove, OnLook, OnJump, OnSprint y OnCrouch
// según las acciones definidas en PlayerControls.inputactions.
public class PlayerInputs : MonoBehaviour
{
    [Header("Valores de entrada (solo lectura)")]
    public Vector2 move;
    public Vector2 look;
    public bool jump;
    public bool sprint;
    public bool crouch;

    [Header("Movimiento")]
    public bool analogMovement;

    [Header("Mouse")]
    public bool cursorLocked = true;
    public bool cursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM
    public void OnMove(InputValue value)
    {
        MoveInput(value.Get<Vector2>());
    }

    public void OnLook(InputValue value)
    {
        if (cursorInputForLook)
        {
            LookInput(value.Get<Vector2>());
        }
    }

    public void OnJump(InputValue value)
    {
        JumpInput(value.isPressed);
    }

    public void OnSprint(InputValue value)
    {
        SprintInput(value.isPressed);
    }

    // Nuevo: se llama al presionar y al soltar CTRL (o C).
    public void OnCrouch(InputValue value)
    {
        CrouchInput(value.isPressed);
    }
#endif

    public void MoveInput(Vector2 newMoveDirection)
    {
        move = newMoveDirection;
    }

    public void LookInput(Vector2 newLookDirection)
    {
        look = newLookDirection;
    }

    public void JumpInput(bool newJumpState)
    {
        jump = newJumpState;
    }

    public void SprintInput(bool newSprintState)
    {
        sprint = newSprintState;
    }

    public void CrouchInput(bool newCrouchState)
    {
        crouch = newCrouchState;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        SetCursorState(cursorLocked);
    }

    // Lo usarán el menú de pausa y el Game Over para mostrar u ocultar el mouse.
    public void SetCursorState(bool newState)
    {
        Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
    }
}