using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("=== 移动速度 ===")]
        [Tooltip("走路速度（米/秒）")]
        public float MoveSpeed = 2.0f; 
        [Tooltip("跑步速度（米/秒）")]
        public float SprintSpeed = 4.5f; 
        [Range(0.0f, 0.3f)] public float RotationSmoothTime = 0.08f; 

        [Header("空中移动")]
        [Tooltip("空中非跳跃状态的移动速度（米/秒）")]
        public float AirMoveSpeed = 1.5f; 

        [Header("Audio")]
        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Header("Gravity & Ground")]
        public float Gravity = -20.0f;
        public float FallTimeout = 0.8f;
        public bool Grounded = true;
        public float GroundCheckOffset = -0.1f;
        public float GroundCheckRadius = 0.28f;
        public float GroundCheckHeight = 0.5f;
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        public GameObject CinemachineCameraTarget;
        public float TopClamp = 70.0f;
        public float BottomClamp = -30.0f;
        public float CameraAngleOverride = 0.0f;
        public bool LockCameraPosition = false;

        [Header("Jump Charge Settings")]
        public float maxChargeTime = 3f;
        public float minJumpHeight = 6f;
        public float maxJumpHeight = 40f;
        public float minJumpDistance = 12f;
        public float maxJumpDistance = 80f;
        public float jumpHorizontalSpeedMultiplier = 3.6f;
        public float jumpAirDamping = 1f;

        [Header("Friction Settings")]
        public float jumpLandFriction = 0.85f;
        public float minVelocityThreshold = 0.01f; // 仅用于动画判定

        // 核心变量
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;
        private float _speed; // 无加速度
        private float _animationBlend; // 动画参数直接跟随_speed
        private float _targetRotation;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 100.0f;
        private const float GroundStickVelocity = -8.0f;
        private float _fallTimeoutDelta;
        private bool _isGroundedPrev;

        // 蓄力跳跃相关
        private bool _isChargingJump;
        private float _jumpChargeTime;
        private Vector3 _jumpDirection;
        private Vector3 _jumpHorizontalVelocity;
        private bool _isJumping;
        private bool _isFalling;
        private Vector3 _chargeStartPos;

        // 动画ID
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

        // 组件引用
#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
        private InputAction _jumpAction;
        private InputAction _moveAction;
        private InputAction _sprintAction;
        private InputAction _lookAction;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private Camera _mainCamera;
        private bool _hasAnimator;
        private const float _threshold = 0.01f;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput != null && _playerInput.currentControlScheme == "KeyboardMouse";
#else
                return false;
#endif
            }
        }

        private void Awake()
        {
            // 组件检查
            _controller = GetComponent<CharacterController>();
            if (_controller == null)
            {
                Debug.LogError("CharacterController组件未挂载！");
                _controller = gameObject.AddComponent<CharacterController>();
            }
            _controller.skinWidth = 0.1f;
            _controller.stepOffset = 0.3f;
            _controller.minMoveDistance = 0f;

            _input = GetComponent<StarterAssetsInputs>();
            if (_input == null)
            {
                Debug.LogError("StarterAssetsInputs组件未挂载！");
                _input = gameObject.AddComponent<StarterAssetsInputs>();
            }

            // 相机获取
            if (CinemachineCameraTarget != null)
            {
                _mainCamera = CinemachineCameraTarget.GetComponentInParent<Camera>();
            }
            if (_mainCamera == null)
            {
                GameObject camObj = GameObject.FindGameObjectWithTag("MainCamera");
                if (camObj != null) _mainCamera = camObj.GetComponent<Camera>();
            }
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }

#if ENABLE_INPUT_SYSTEM 
            _playerInput = GetComponent<PlayerInput>();
            if (_playerInput != null)
            {
                _jumpAction = _playerInput.actions["Jump"];
                _moveAction = _playerInput.actions["Move"];
                _sprintAction = _playerInput.actions["Sprint"];
                _lookAction = _playerInput.actions["Look"];
            }
#endif
        }

        private void Start()
        {
            if (CinemachineCameraTarget != null)
            {
                _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            }

            _hasAnimator = TryGetComponent(out _animator);
            AssignAnimationIDs();

            // 初始化参数
            _fallTimeoutDelta = FallTimeout;
            _jumpChargeTime = 0f;
            _isChargingJump = false;
            _isJumping = false;
            _isFalling = false;
            _jumpHorizontalVelocity = Vector3.zero;
            _isGroundedPrev = Grounded;
            _chargeStartPos = transform.position;
            _speed = 0f;
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            GroundedCheck();

            // 落地重置跳跃状态
            if (Grounded)
            {
                if (_isJumping)
                {
                    _isJumping = false;
                    _jumpHorizontalVelocity = Vector3.zero;
                }
            }
            else
            {
                _jumpHorizontalVelocity *= jumpAirDamping;
                if (_jumpHorizontalVelocity.magnitude < minVelocityThreshold)
                    _jumpHorizontalVelocity = Vector3.zero;
            }

            HandleJumpCharge();

            if (!_isChargingJump)
            {
                Move(); // 核心移动逻辑（无加速度）
            }
            else
            {
                _speed = 0f;
                _animationBlend = 0f;
                _targetRotation = transform.eulerAngles.y;
                _controller.Move(new Vector3(0f, GroundStickVelocity * Time.deltaTime, 0f));
            }

            ApplyGravity();
            UpdateAnimationParams(); // 动画直接跟随速度，无渐变
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
            bool ccGrounded = _controller.isGrounded;
            bool customGrounded = false;

            Vector3 ccCenter = transform.position + _controller.center;
            Vector3 capsuleBottom = ccCenter + Vector3.up * (GroundCheckOffset - GroundCheckHeight / 2);
            Vector3 capsuleTop = ccCenter + Vector3.up * (GroundCheckOffset + GroundCheckHeight / 2);
            customGrounded = Physics.CheckCapsule(capsuleBottom, capsuleTop, GroundCheckRadius, GroundLayers, QueryTriggerInteraction.Ignore);

            Grounded = ccGrounded || customGrounded;

            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;
                _isGroundedPrev = true;
            }
            else
            {
                _isGroundedPrev = false;
                _fallTimeoutDelta -= Time.deltaTime;
            }
        }

        private void HandleJumpCharge()
        {
            bool jumpPressed = false;
#if ENABLE_INPUT_SYSTEM 
            jumpPressed = _jumpAction?.IsPressed() ?? Input.GetKey(KeyCode.Space);
#else
            jumpPressed = Input.GetKey(KeyCode.Space);
#endif

            if (Grounded)
            {
                if (jumpPressed && !_isChargingJump)
                {
                    _isChargingJump = true;
                    _jumpChargeTime = 0f;
                    _isFalling = false;
                    _chargeStartPos = transform.position;
                    _targetRotation = transform.eulerAngles.y;

                    Vector2 moveInput = _input?.move ?? Vector2.zero;
                    if (moveInput.sqrMagnitude < _threshold)
                    {
                        Vector3 cameraForward = _mainCamera ? _mainCamera.transform.forward : Vector3.forward;
                        cameraForward.y = 0f;
                        _jumpDirection = cameraForward.normalized;
                    }
                    else
                    {
                        float cameraYaw = _mainCamera ? _mainCamera.transform.rotation.eulerAngles.y : 0f;
                        Vector3 inputDir = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
                        _jumpDirection = Quaternion.Euler(0f, cameraYaw, 0f) * inputDir;
                    }

                    if (_jumpDirection.sqrMagnitude < _threshold)
                        _jumpDirection = transform.forward;

                    Debug.DrawLine(transform.position, transform.position + _jumpDirection * 5f, Color.red, 2f);
                }
                else if (jumpPressed && _isChargingJump)
                {
                    _jumpChargeTime = Mathf.Min(_jumpChargeTime + Time.deltaTime, maxChargeTime);
                    transform.position = _chargeStartPos;
                }
                else if (!jumpPressed && _isChargingJump)
                {
                    _isChargingJump = false;
                    _isJumping = true;
                    float chargeRatio = Mathf.Clamp01(_jumpChargeTime / maxChargeTime);

                    float jumpHeight = Mathf.Lerp(minJumpHeight, maxJumpHeight, chargeRatio);
                    float gravityAbs = Mathf.Abs(Gravity);
                    float verticalSpeed = Mathf.Sqrt(2 * gravityAbs * jumpHeight);
                    _verticalVelocity = verticalSpeed;

                    float jumpDistance = Mathf.Lerp(minJumpDistance, maxJumpDistance, chargeRatio);
                    float airTime = 2 * verticalSpeed / gravityAbs;
                    float horizontalSpeedBase = jumpDistance / airTime;
                    _jumpHorizontalVelocity = _jumpDirection * horizontalSpeedBase * jumpHorizontalSpeedMultiplier;
                }
            }
            else
            {
                _isChargingJump = false;
            }
        }

        #region 核心修改：无加速度的移动逻辑
        private void Move()
        {
            Vector2 moveInput = _input?.move ?? Vector2.zero;
            bool hasMoveInput = moveInput.sqrMagnitude >= _threshold;

            
            float targetSpeed = 0f;
            if (Grounded && !_isJumping)
            {
                
                targetSpeed = hasMoveInput ? (_input.sprint ? SprintSpeed : MoveSpeed) : 0f;
                _speed = targetSpeed; 
            }
            else if (!Grounded && !_isJumping)
            {
                targetSpeed = hasMoveInput ? AirMoveSpeed : 0f;
                _speed = targetSpeed;
            }

            // 角色旋转
            if (Grounded && hasMoveInput)
            {
                float cameraYaw = _mainCamera ? _mainCamera.transform.rotation.eulerAngles.y : 0f;
                _targetRotation = Mathf.Atan2(moveInput.x, moveInput.y) * Mathf.Rad2Deg + cameraYaw;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, rotation, 0f);
            }

            //  移动向量（距离=速度×时间）
            Vector3 moveVector = Vector3.zero;
            if (Grounded && !_isJumping)
            {
                Vector3 moveDir = Quaternion.Euler(0f, _targetRotation, 0f) * Vector3.forward;
                moveDir.Normalize();
                moveVector = moveDir * _speed * Time.deltaTime + new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime;
            }
            else
            {
                Vector3 airManualMove = Vector3.zero;
                if (!_isJumping && hasMoveInput)
                {
                    Vector3 moveDir = Quaternion.Euler(0f, _targetRotation, 0f) * Vector3.forward;
                    moveDir.Normalize();
                    airManualMove = moveDir * targetSpeed * Time.deltaTime;
                }
                Vector3 jumpMove = _jumpHorizontalVelocity * Time.deltaTime;
                Vector3 verticalMove = new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime;
                moveVector = airManualMove + jumpMove + verticalMove;
            }

            // 4. 执行移动
            _controller.Move(moveVector);

            // 5. 动画参数直接跟随速度
            _animationBlend = _speed; // 直接赋值
            if (_animationBlend < minVelocityThreshold) _animationBlend = 0f;
        }
        #endregion

        private void ApplyGravity()
        {
            if (Grounded)
            {
                _verticalVelocity = Mathf.Max(_verticalVelocity, GroundStickVelocity);
                _isFalling = false;
            }
            else
            {
                if (_verticalVelocity > -_terminalVelocity)
                    _verticalVelocity += Gravity * Time.deltaTime;

                bool shouldFall = _verticalVelocity < 0f && _fallTimeoutDelta <= 0f;
                if (shouldFall && !_isFalling) _isFalling = true;
                else if (!shouldFall && _isFalling) _isFalling = false;
            }
        }

        #region 
        private void UpdateAnimationParams()
        {
            if (!_hasAnimator) return;

            Vector2 moveInput = _input?.move ?? Vector2.zero;
            bool hasMoveInput = moveInput.sqrMagnitude >= _threshold;

            if (_isChargingJump)
            {
                // 蓄力站立
                _animator.SetFloat(_animIDSpeed, 0f);
                _animator.SetFloat(_animIDMotionSpeed, 0f);
                _animator.SetBool(_animIDGrounded, true);
                _animator.SetBool(_animIDJump, false);
                _animator.SetBool(_animIDFreeFall, false);
            }
            else if (Grounded)
            {
                // 地面：动画参数直接等于速度
                _animator.SetBool(_animIDGrounded, true);
                _animator.SetBool(_animIDJump, false);
                _animator.SetBool(_animIDFreeFall, false);
                _animator.SetFloat(_animIDSpeed, _animationBlend); // 直接赋值，立刻切换
                _animator.SetFloat(_animIDMotionSpeed, hasMoveInput ? 1f : 0f); // 移动状态立刻切换
            }
            else
            {
                //空中
                _animator.SetBool(_animIDGrounded, false);
                if (_isJumping && !_isFalling)
                {
                    _animator.SetBool(_animIDJump, true);
                    _animator.SetBool(_animIDFreeFall, false);
                }
                else
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, true);
                }
                _animator.SetFloat(_animIDSpeed, hasMoveInput ? 0.5f : 0f);
                _animator.SetFloat(_animIDMotionSpeed, hasMoveInput ? moveInput.magnitude : 0f);
            }
        }
        #endregion

        private void CameraRotation()
        {
            Vector2 lookInput = _input?.look ?? Vector2.zero;
            if (lookInput.sqrMagnitude >= _threshold && !LockCameraPosition && CinemachineCameraTarget != null)
            {
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
                _cinemachineTargetYaw += lookInput.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += lookInput.y * deltaTimeMultiplier;
            }

            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(
                _cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw,
                0f
            );
        }

        private static float ClampAngle(float angle, float min, float max)
        {
            angle = angle % 360f;
            if (angle < -360f) angle += 360f;
            if (angle > 360f) angle -= 360f;
            return Mathf.Clamp(angle, min, max);
        }

        private void OnDrawGizmosSelected()
        {
            Color groundColor = Grounded ? new Color(0f, 1f, 0f, 0.35f) : new Color(1f, 0f, 0f, 0.35f);
            Gizmos.color = groundColor;

            Vector3 ccCenter = transform.position;
            if (_controller != null) ccCenter += _controller.center;
            else ccCenter += Vector3.up * 0.9f;

            Vector3 capsuleBottom = ccCenter + Vector3.up * (GroundCheckOffset - GroundCheckHeight / 2);
            Vector3 capsuleTop = ccCenter + Vector3.up * (GroundCheckOffset + GroundCheckHeight / 2);

            Vector3 offsetX = Vector3.right * GroundCheckRadius;
            Vector3 offsetZ = Vector3.forward * GroundCheckRadius;
            Gizmos.DrawLine(capsuleBottom + offsetX, capsuleTop + offsetX);
            Gizmos.DrawLine(capsuleBottom - offsetX, capsuleTop - offsetX);
            Gizmos.DrawLine(capsuleBottom + offsetZ, capsuleTop + offsetZ);
            Gizmos.DrawLine(capsuleBottom - offsetZ, capsuleTop - offsetZ);

            Gizmos.DrawWireSphere(capsuleBottom, GroundCheckRadius);
            Gizmos.DrawWireSphere(capsuleTop, GroundCheckRadius);

            if (_isChargingJump)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(_chargeStartPos, 0.3f);
            }

            if (_isJumping)
            {
                Gizmos.color = Color.yellow;
                Vector3 nextPos = transform.position + _jumpHorizontalVelocity * 0.1f + new Vector3(0f, _verticalVelocity * 0.1f, 0f);
                Gizmos.DrawLine(transform.position, nextPos);
            }
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f && FootstepAudioClips != null && FootstepAudioClips.Length > 0)
            {
                int index = Random.Range(0, FootstepAudioClips.Length);
                Vector3 clipPos = transform.position;
                if (_controller != null) clipPos = transform.TransformPoint(_controller.center);
                AudioSource.PlayClipAtPoint(FootstepAudioClips[index], clipPos, FootstepAudioVolume);
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f && LandingAudioClip != null)
            {
                Vector3 clipPos = transform.position;
                if (_controller != null) clipPos = transform.TransformPoint(_controller.center);
                AudioSource.PlayClipAtPoint(LandingAudioClip, clipPos, FootstepAudioVolume);
            }
        }
    }
}