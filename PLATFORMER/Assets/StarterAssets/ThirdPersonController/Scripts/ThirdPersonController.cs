using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
#endif

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Player Movement")]
        public float MoveSpeed = 2.0f;
        public float SprintSpeed = 5.335f;
        [SerializeField] private float originalMoveSpeed;
        public float RotationSmoothTime = 0.12f;
        public float SpeedChangeRate = 10.0f;

        [Header("Jump & Gravity")]
        public float JumpHeight = 1.2f;
        public float Gravity = -15.0f;
        public float JumpTimeout = 0.50f;
        public float FallTimeout = 0.15f;

        [Header("Double Jump")]
        public bool enableDoubleJump = false;
        public float doubleJumpMultiplier = 1.0f;
        private bool canDoubleJump = false;
        private bool hasDoubleJumped = false;

        [Header("Crouch Settings")]
        public bool canCrouch = true;
        public float crouchHeight = 1f;
        public float standHeight = 2f;
        public float crouchSpeed = 1.5f;
        private bool isCrouching = false;

        [Header("Grounded")]
        public bool Grounded = true;
        public float GroundedOffset = -0.14f;
        public float GroundedRadius = 0.28f;
        public LayerMask GroundLayers;

        [Header("Camera")]
        public GameObject CinemachineCameraTarget;
        public float TopClamp = 70.0f;
        public float BottomClamp = -30.0f;
        public float CameraAngleOverride = 0.0f;
        public bool LockCameraPosition = false;

        [Header("Audio Clips")]
        public AudioClip[] FootstepAudioClips;
        public AudioClip LandingAudioClip;
        [Range(0, 1f)] public float FootstepAudioVolume = 0.5f;

        [Header("Fall Damage")]
        public float fatalFallSpeed = 20f;
        public LayerMask safeLandingLayers;

        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;
        private int _animIDCrouch;

        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;
        private AudioSource _audioSource;
        private PlayerStateManager playerState;

#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif

        private const float _threshold = 0.01f;
        private bool IsCurrentDeviceMouse => _playerInput.currentControlScheme == "KeyboardMouse";

        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        private void Awake()
        {
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

            _animator = GetComponent<Animator>();
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#endif
            playerState = PlayerStateManager.Instance;
            originalMoveSpeed = MoveSpeed;

            AssignAnimationIDs();

            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            if (playerState == null)
                playerState = PlayerStateManager.Instance;

            JumpAndGravity();
            GroundedCheck();
            HandleCrouch();
            Move();
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
            _animIDCrouch = Animator.StringToHash("IsCrouching");
        }

        private void HandleCrouch()
        {
            if (!canCrouch || !Grounded) return;

            Debug.Log($"[CROUCH] Input: {_input.crouch}, isCrouching: {isCrouching}");

            if (_input.crouch && !isCrouching)
            {
                originalMoveSpeed = MoveSpeed;
                _controller.height = crouchHeight;
                _controller.center = new Vector3(0, crouchHeight * 0.5f, 0);
                MoveSpeed = crouchSpeed;
                isCrouching = true;
                if (_animator != null) _animator.SetBool("IsCrouching", true);

                Debug.Log("🔽 Entrant a crouch.");
            }
            else if (!_input.crouch && isCrouching && !CheckCeiling())
            {
                _controller.height = standHeight;
                _controller.center = new Vector3(0, standHeight * 0.5f, 0);
                MoveSpeed = originalMoveSpeed;
                isCrouching = false;
                if (_animator != null) _animator.SetBool("IsCrouching", false);

                Debug.Log("🔼 Sortint de crouch.");
            }
        }

        private bool CheckCeiling()
        {
            return Physics.Raycast(transform.position, Vector3.up, standHeight - crouchHeight + 0.1f);
        }

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            bool wasGrounded = Grounded;

            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);

            if (!wasGrounded && Grounded)
            {
                hasDoubleJumped = false;
                canDoubleJump = false;

                float fallSpeed = Mathf.Abs(_verticalVelocity);
                bool landedOnSafeSurface = Physics.CheckSphere(spherePosition, GroundedRadius, safeLandingLayers);

                if (fallSpeed > fatalFallSpeed && !landedOnSafeSurface)
                    playerState.TakeDamage(playerState.currentHealth);
            }

            if (_animator != null)
                _animator.SetBool(_animIDGrounded, Grounded);
        }

        private void Move()
        {
            if (playerState == null || !playerState.CanMove) return;

            float targetSpeed = (_input.sprint && !isCrouching && playerState.TryUseStamina(playerState.staminaDrainRate * Time.deltaTime))
                ? SprintSpeed
                : MoveSpeed;

            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            if (Mathf.Abs(currentHorizontalSpeed - targetSpeed) > 0.1f)
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
            else
                _speed = targetSpeed;

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }

            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) + Vector3.up * _verticalVelocity * Time.deltaTime);

            if (_animator != null)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;

                if (_animator != null)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                if (_verticalVelocity < 0.0f)
                    _verticalVelocity = -2f;

                if (_input.jump && _jumpTimeoutDelta <= 0.0f && !isCrouching)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                    hasDoubleJumped = false;
                    canDoubleJump = false;

                    if (_animator != null)
                        _animator.SetBool(_animIDJump, true);

                    _input.jump = false;
                }

                if (_jumpTimeoutDelta >= 0.0f)
                    _jumpTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                _jumpTimeoutDelta = JumpTimeout;

                if (_fallTimeoutDelta >= 0.0f)
                    _fallTimeoutDelta -= Time.deltaTime;
                else if (_animator != null)
                    _animator.SetBool(_animIDFreeFall, true);

                if (enableDoubleJump && !hasDoubleJumped && canDoubleJump && _input.jump)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * doubleJumpMultiplier * -2f * Gravity);
                    hasDoubleJumped = true;

                    if (_animator != null)
                        _animator.SetBool(_animIDJump, true);

                    _input.jump = false;
                }

                if (enableDoubleJump && !canDoubleJump)
                    canDoubleJump = true;
            }

            if (_verticalVelocity < _terminalVelocity)
                _verticalVelocity += Gravity * Time.deltaTime;
        }

        private void CameraRotation()
        {
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw, 0.0f);
        }

        private static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360f) angle += 360f;
            if (angle > 360f) angle -= 360f;
            return Mathf.Clamp(angle, min, max);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f && FootstepAudioClips.Length > 0)
            {
                int index = Random.Range(0, FootstepAudioClips.Length);
                _audioSource.PlayOneShot(FootstepAudioClips[index], FootstepAudioVolume);
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f && LandingAudioClip != null)
            {
                _audioSource.PlayOneShot(LandingAudioClip, FootstepAudioVolume);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Color color = Grounded ? new Color(0f, 1f, 0f, 0.35f) : new Color(1f, 0f, 0f, 0.35f);
            Gizmos.color = color;
            Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);

            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, Vector3.up * (standHeight - crouchHeight + 0.1f));
        }
    }
}
