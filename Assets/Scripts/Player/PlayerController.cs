using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// S.E.C. Protocol - Controlador del jugador.
// Basado en ThirdPersonController de Unity Starter Assets.
//
// Cambios respecto al original:
//   1. El personaje siempre mira hacia donde apunta la cámara (estilo shooter).
//   2. Stamina del sprint: 5 s de uso y 5 s para recargarse por completo.
//      Si se agota, no se puede correr hasta que se llene de nuevo.
//   3. Doble salto (2 saltos en total). Caer de un borde cuenta como el primer salto.
//   4. Agacharse con CTRL o C (mantener): la hitbox baja a la mitad,
//      la velocidad baja y la cámara baja un poco. No se puede correr ni saltar agachado.
[RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
[RequireComponent(typeof(PlayerInput))]
#endif
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [Tooltip("Velocidad caminando (m/s)")]
    public float MoveSpeed = 2.0f;

    [Tooltip("Velocidad corriendo (m/s)")]
    public float SprintSpeed = 5.335f;

    [Tooltip("Velocidad agachado (m/s)")]
    public float CrouchSpeed = 1.0f;

    [Tooltip("Qué tan rápido gira el personaje hacia la cámara. Más bajo = más rápido")]
    [Range(0.0f, 0.3f)]
    public float RotationSmoothTime = 0.05f;

    [Tooltip("Aceleración y frenado")]
    public float SpeedChangeRate = 10.0f;

    [Header("Stamina del sprint")]
    [Tooltip("Segundos que se puede correr con la stamina llena")]
    public float MaxSprintTime = 5f;

    [Tooltip("Segundos que tarda la stamina en llenarse desde cero")]
    public float SprintRechargeTime = 5f;

    [Header("Salto")]
    [Tooltip("Altura de cada salto")]
    public float JumpHeight = 1.2f;

    [Tooltip("Cantidad total de saltos (2 = doble salto)")]
    public int MaxJumps = 2;

    [Tooltip("Gravedad propia del personaje. La de Unity por defecto es -9.81")]
    public float Gravity = -15.0f;

    [Tooltip("Tiempo de espera en el suelo antes de poder volver a saltar")]
    public float JumpTimeout = 0.2f;

    [Tooltip("Tiempo en el aire antes de pasar a la animación de caída")]
    public float FallTimeout = 0.15f;

    [Header("Agacharse")]
    [Tooltip("Altura de la hitbox agachado (0.5 = la mitad)")]
    [Range(0.3f, 1f)]
    public float CrouchHeightMultiplier = 0.5f;

    [Tooltip("Cuánto baja la cámara al agacharse (metros)")]
    public float CrouchCameraDrop = 0.5f;

    [Tooltip("Qué tan rápido se agacha y se levanta")]
    public float CrouchTransitionSpeed = 10f;

    [Header("Suelo")]
    [Tooltip("Si el personaje está tocando el suelo")]
    public bool Grounded = true;

    public float GroundedOffset = -0.14f;

    [Tooltip("Debe ser igual al radio del CharacterController")]
    public float GroundedRadius = 0.28f;

    [Tooltip("Layers que cuentan como suelo")]
    public LayerMask GroundLayers;

    [Header("Cinemachine")]
    [Tooltip("El objeto que sigue la cámara (PlayerCameraRoot)")]
    public GameObject CinemachineCameraTarget;

    [Tooltip("Cuántos grados puede mirar hacia arriba")]
    public float TopClamp = 70.0f;

    [Tooltip("Cuántos grados puede mirar hacia abajo")]
    public float BottomClamp = -30.0f;

    public float CameraAngleOverride = 0.0f;
    public bool LockCameraPosition = false;

    [Header("Audio (opcional)")]
    public AudioClip LandingAudioClip;
    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    [Header("Debug (solo para ver en el Inspector)")]
    [SerializeField] private float _stamina;
    [SerializeField] private bool _exhausted;
    [SerializeField] private int _jumpsUsed;

    // ---- Datos públicos para el HUD y otros scripts ----
    public bool IsSprinting { get; private set; }
    public bool IsCrouching { get; private set; }
    public bool IsExhausted => _exhausted;
    public float StaminaNormalized => MaxSprintTime > 0f ? _stamina / MaxSprintTime : 0f;

    // cinemachine
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;

    // jugador
    private float _speed;
    private float _animationBlend;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private readonly float _terminalVelocity = 53.0f;
    private Vector3 _lastMoveDirection = Vector3.forward;

    // agacharse
    private float _standHeight;
    private float _standCenterY;
    private float _cameraRootStandY;

    // temporizadores
    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

    // animaciones (las del Animator de Starter Assets)
    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDMotionSpeed;

#if ENABLE_INPUT_SYSTEM
    private PlayerInput _playerInput;
#endif
    private Animator _animator;
    private CharacterController _controller;
    private PlayerInputs _input;
    private GameObject _mainCamera;

    private const float _threshold = 0.01f;
    private bool _hasAnimator;

    private bool IsCurrentDeviceMouse
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return _playerInput.currentControlScheme == "KeyboardMouse";
#else
            return false;
#endif
        }
    }

    private void Awake()
    {
        if (_mainCamera == null)
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }
    }

    private void Start()
    {
        _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

        _hasAnimator = TryGetComponent(out _animator);
        _controller = GetComponent<CharacterController>();
        _input = GetComponent<PlayerInputs>();
#if ENABLE_INPUT_SYSTEM
        _playerInput = GetComponent<PlayerInput>();
#else
        Debug.LogError("Falta el paquete Input System.");
#endif

        AssignAnimationIDs();

        // guardar medidas de pie, para volver a ellas al levantarse
        _standHeight = _controller.height;
        _standCenterY = _controller.center.y;
        _cameraRootStandY = CinemachineCameraTarget.transform.localPosition.y;

        _jumpTimeoutDelta = JumpTimeout;
        _fallTimeoutDelta = FallTimeout;
        _stamina = MaxSprintTime;
    }

    private void Update()
    {
        _hasAnimator = TryGetComponent(out _animator);

        JumpAndGravity();
        GroundedCheck();
        UpdateCrouch();
        Move();
        UpdateStamina();
    }

    private void LateUpdate()
    {
        CameraRotation();
    }

    private void AssignAnimationIDs()
    {
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDJump = Animator.StringToHash("Jump");
        _animIDFreeFall = Animator.StringToHash("FreeFall");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
    }

    private void GroundedCheck()
    {
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
            transform.position.z);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
            QueryTriggerInteraction.Ignore);

        if (_hasAnimator)
        {
            _animator.SetBool(_animIDGrounded, Grounded);
        }
    }

    private void CameraRotation()
    {
        if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
        {
            // el mouse no se multiplica por Time.deltaTime
            float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

            _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
            _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
        }

        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
            _cinemachineTargetYaw, 0.0f);
    }

    // AGACHARSE

    private void UpdateCrouch()
    {
        // Solo se puede empezar a agachar estando en el suelo.
        if (_input.crouch && Grounded)
        {
            IsCrouching = true;
        }
        else if (!_input.crouch && IsCrouching && CanStandUp())
        {
            // Solo se levanta si hay espacio arriba (por ejemplo, no debajo de un puente bajo).
            IsCrouching = false;
        }

        // Cambiar la altura de la hitbox poco a poco
        float targetHeight = IsCrouching ? _standHeight * CrouchHeightMultiplier : _standHeight;
        float newHeight = Mathf.Lerp(_controller.height, targetHeight, Time.deltaTime * CrouchTransitionSpeed);
        _controller.height = newHeight;
        _controller.center = new Vector3(_controller.center.x, _standCenterY * (newHeight / _standHeight),
            _controller.center.z);

        // Bajar la cámara al agacharse
        Vector3 rootPos = CinemachineCameraTarget.transform.localPosition;
        float targetCamY = IsCrouching ? _cameraRootStandY - CrouchCameraDrop : _cameraRootStandY;
        rootPos.y = Mathf.Lerp(rootPos.y, targetCamY, Time.deltaTime * CrouchTransitionSpeed);
        CinemachineCameraTarget.transform.localPosition = rootPos;
    }

    private bool CanStandUp()
    {
        float radius = _controller.radius;
        Vector3 origin = transform.position + Vector3.up * (_controller.height - radius);
        float distance = _standHeight - _controller.height;
        if (distance <= 0.01f) return true;

        // Lanza una esfera hacia arriba: si choca con algo, no hay espacio para levantarse.
        return !Physics.SphereCast(origin, radius * 0.9f, Vector3.up, out _, distance,
            ~0, QueryTriggerInteraction.Ignore);
    }

    // MOVIMIENTO

    private void Move()
    {
        bool hasMoveInput = _input.move != Vector2.zero;

        // Solo corre si: presiona SHIFT, se está moviendo, no está agachado y tiene stamina
        IsSprinting = _input.sprint && hasMoveInput && !IsCrouching && !_exhausted && _stamina > 0f;

        float targetSpeed = MoveSpeed;
        if (IsCrouching) targetSpeed = CrouchSpeed;
        else if (IsSprinting) targetSpeed = SprintSpeed;

        if (!hasMoveInput) targetSpeed = 0.0f;

        float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;
        float speedOffset = 0.1f;
        float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

        if (currentHorizontalSpeed < targetSpeed - speedOffset ||
            currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                Time.deltaTime * SpeedChangeRate);
            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        }
        else
        {
            _speed = targetSpeed;
        }

        _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
        if (_animationBlend < 0.01f) _animationBlend = 0f;

        // CAMBIO SHOOTER: el personaje siempre mira hacia donde mira la cámara
        float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _cinemachineTargetYaw,
            ref _rotationVelocity, RotationSmoothTime);
        transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);

        // WASD relativo a la cámara: W = hacia la mira, A/D = de lado, S = hacia atrás
        if (hasMoveInput)
        {
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;
            _lastMoveDirection = Quaternion.Euler(0.0f, _cinemachineTargetYaw, 0.0f) * inputDirection;
        }

        _controller.Move(_lastMoveDirection.normalized * (_speed * Time.deltaTime) +
                         new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

        if (_hasAnimator)
        {
            _animator.SetFloat(_animIDSpeed, _animationBlend);
            _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
        }
    }

    // STAMINA DEL SPRINT

    private void UpdateStamina()
    {
        if (IsSprinting)
        {
            _stamina -= Time.deltaTime;
            if (_stamina <= 0f)
            {
                _stamina = 0f;
                _exhausted = true; // se agotó: no puede correr hasta llenarse
            }
        }
        else
        {
            // se llena de 0 a MaxSprintTime en SprintRechargeTime segundos
            _stamina += Time.deltaTime * (MaxSprintTime / SprintRechargeTime);
            if (_stamina >= MaxSprintTime)
            {
                _stamina = MaxSprintTime;
                _exhausted = false;
            }
        }
    }

    // SALTO, DOBLE SALTO Y GRAVEDAD
    private void JumpAndGravity()
    {
        if (Grounded)
        {
            _fallTimeoutDelta = FallTimeout;

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDJump, false);
                _animator.SetBool(_animIDFreeFall, false);
            }

            // ya aterrizó: reiniciar velocidad vertical y saltos
            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2f;
                _jumpsUsed = 0;
            }

            if (_jumpTimeoutDelta >= 0.0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }
        }
        else
        {
            _jumpTimeoutDelta = JumpTimeout;

            // si cayó de un borde sin saltar, eso cuenta como el primer salto
            if (_jumpsUsed == 0) _jumpsUsed = 1;

            if (_fallTimeoutDelta >= 0.0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
            }
            else if (_hasAnimator)
            {
                _animator.SetBool(_animIDFreeFall, true);
            }
        }

        // ¿Presionó saltar?
        if (_input.jump)
        {
            bool canGroundJump = Grounded && _jumpTimeoutDelta <= 0.0f && !IsCrouching;
            bool canAirJump = !Grounded && _jumpsUsed < MaxJumps;

            if (canGroundJump || canAirJump)
            {
                // velocidad necesaria para llegar a JumpHeight: raíz de (altura * -2 * gravedad)
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                _jumpsUsed = canGroundJump ? 1 : _jumpsUsed + 1;

                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, true);
                }
            }

            // cada presión de espacio se usa una sola vez
            _input.jump = false;
        }

        if (_verticalVelocity < _terminalVelocity)
        {
            _verticalVelocity += Gravity * Time.deltaTime;
        }
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void OnDrawGizmosSelected()
    {
        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

        Gizmos.color = Grounded ? transparentGreen : transparentRed;
        Gizmos.DrawSphere(
            new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
            GroundedRadius);
    }

    // Las animaciones de Starter Assets llaman a estos dos métodos (pasos y aterrizaje).
    // Si no hay sonidos asignados, simplemente no hacen nada. (se actualizaran en el futuro)
    private void OnFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f && FootstepAudioClips != null && FootstepAudioClips.Length > 0)
        {
            var index = Random.Range(0, FootstepAudioClips.Length);
            AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center),
                FootstepAudioVolume);
        }
    }

    private void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f && LandingAudioClip != null)
        {
            AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center),
                FootstepAudioVolume);
        }
    }
}