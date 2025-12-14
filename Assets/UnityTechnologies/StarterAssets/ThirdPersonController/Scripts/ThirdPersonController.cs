using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

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
        [Range(0.0f, 0.3f)] public float RotationSmoothTime = 0.12f;
        public float SpeedChangeRate = 10.0f;
        [Tooltip("空中非跳跃状态的移动速度（如坠落时的手动调整）")]
        public float AirMoveSpeed = 4.0f; // 重命名并调整，区分跳跃和普通空中移动

        [Header("Audio")]
        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Header("Gravity & Ground")]
        public float Gravity = -20.0f;
        [Tooltip("离开地面后触发下落动画的延迟（秒），越大容错越高")]
        public float FallTimeout = 0.8f;
        public bool Grounded = true;
        [Tooltip("地面检测胶囊体的偏移（相对于CharacterController的Center）")]
        public float GroundCheckOffset = -0.1f;
        [Tooltip("地面检测胶囊体的半径（建议与CharacterController半径一致）")]
        public float GroundCheckRadius = 0.28f;
        [Tooltip("地面检测胶囊体的高度（建议为CharacterController高度的1/3）")]
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
        [Tooltip("水平速度放大系数（仅作用于跳跃的初始水平速度）")]
        public float jumpHorizontalSpeedMultiplier = 3.6f;
        [Tooltip("跳跃水平速度的空气阻尼（1为无阻尼，<1为有阻尼）")]
        public float jumpAirDamping = 1f; // 新增：跳跃水平速度的空气阻尼（可选）

        [Header("Friction Settings")]
        public float jumpLandFriction = 0.85f; // 跳跃落地后的水平摩擦力（0-1）
        public float moveStopFriction = 0.9f; // 正常移动停止输入时的摩擦力（0-1）
        public float minVelocityThreshold = 0.1f; // 最小速度阈值，低于此值视为0

        // 核心变量
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;
        private float _speed;
        private float _animationBlend;
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
        private Vector3 _jumpHorizontalVelocity; // 跳跃的初始水平速度（抛物线水平分量）
        private bool _isJumping; // 标记是否处于跳跃轨迹中（从起跳到落地）
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
            // 优化相机获取逻辑
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

            // 获取CharacterController并设置参数
            _controller = GetComponent<CharacterController>();
            if (_controller != null)
            {
                _controller.skinWidth = 0.1f;
                _controller.stepOffset = 0.3f;
                _controller.minMoveDistance = 0f;
            }
        }

        private void Start()
        {
            if (CinemachineCameraTarget != null)
            {
                _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            }

            _hasAnimator = TryGetComponent(out _animator);
            _input = GetComponent<StarterAssetsInputs>();

            AssignAnimationIDs();

            _fallTimeoutDelta = FallTimeout;
            _jumpChargeTime = 0f;
            _isChargingJump = false;
            _isJumping = false;
            _isFalling = false;
            _jumpHorizontalVelocity = Vector3.zero;
            _isGroundedPrev = Grounded;
            _chargeStartPos = transform.position;
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            GroundedCheck();

            // 落地后重置跳跃状态并衰减水平速度（摩擦力）
            if (Grounded)
            {
                if (_isJumping)
                {
                    _isJumping = false;
                    _jumpHorizontalVelocity = Vector3.zero; // 落地后直接清空跳跃水平速度
                }
                else
                {
                    // 正常地面移动的摩擦力（非跳跃落地）
                    _speed *= moveStopFriction;
                    if (_speed < minVelocityThreshold) _speed = 0f;
                }
            }
            else
            {
                // 跳跃过程中水平速度仅受空气阻尼（可选，抛物线可设为1）
                _jumpHorizontalVelocity *= jumpAirDamping;
                if (_jumpHorizontalVelocity.magnitude < minVelocityThreshold)
                    _jumpHorizontalVelocity = Vector3.zero;
            }

            HandleJumpCharge();

            // 蓄力时跳过移动逻辑
            if (!_isChargingJump)
            {
                Move();
            }
            else
            {
                // 蓄力时固定位置和参数
                _speed = 0f;
                _animationBlend = 0f;
                _targetRotation = transform.eulerAngles.y;
                _controller?.Move(new Vector3(0f, GroundStickVelocity * Time.deltaTime, 0f));
            }

            ApplyGravity();
            UpdateAnimationParams();
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
            bool ccGrounded = _controller != null ? _controller.isGrounded : false;
            bool customGrounded = false;

            if (_controller != null)
            {
                Vector3 ccCenter = transform.position + _controller.center;
                Vector3 capsuleBottom = ccCenter + Vector3.up * (GroundCheckOffset - GroundCheckHeight / 2);
                Vector3 capsuleTop = ccCenter + Vector3.up * (GroundCheckOffset + GroundCheckHeight / 2);
                customGrounded = Physics.CheckCapsule(capsuleBottom, capsuleTop, GroundCheckRadius, GroundLayers, QueryTriggerInteraction.Ignore);
            }

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
                    // 开始蓄力
                    _isChargingJump = true;
                    _jumpChargeTime = 0f;
                    _isFalling = false;
                    _chargeStartPos = transform.position;
                    _targetRotation = transform.eulerAngles.y;

                    // 计算跳跃方向（蓄力时确定，后续不再改变）
                    Vector2 moveInput = _input?.move ?? Vector2.zero;
                    if (moveInput.sqrMagnitude < _threshold)
                    {
                        Vector3 cameraForward = _mainCamera ? _mainCamera.transform.forward : Vector3.forward;
                        cameraForward.y = 0f;
                        _jumpDirection = cameraForward.normalized;
                    }
                    else
                    {
                        float cameraYaw = _mainCamera ? _mainCamera.transform.eulerAngles.y : 0f;
                        Vector3 inputDir = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
                        _jumpDirection = Quaternion.Euler(0f, cameraYaw, 0f) * inputDir;
                    }

                    if (_jumpDirection.sqrMagnitude < _threshold)
                        _jumpDirection = transform.forward;

                    Debug.DrawLine(transform.position, transform.position + _jumpDirection * 5f, Color.red, 2f);
                }
                else if (jumpPressed && _isChargingJump)
                {
                    // 持续蓄力
                    _jumpChargeTime = Mathf.Min(_jumpChargeTime + Time.deltaTime, maxChargeTime);
                    transform.position = _chargeStartPos; // 固定蓄力位置
                }
                else if (!jumpPressed && _isChargingJump)
                {
                    // 结束蓄力，触发跳跃（抛物线初始速度计算）
                    _isChargingJump = false;
                    _isJumping = true;
                    float chargeRatio = Mathf.Clamp01(_jumpChargeTime / maxChargeTime);

                    // 计算竖直方向速度（匀变速）
                    float jumpHeight = Mathf.Lerp(minJumpHeight, maxJumpHeight, chargeRatio);
                    float gravityAbs = Mathf.Abs(Gravity);
                    float verticalSpeed = Mathf.Sqrt(2 * gravityAbs * jumpHeight);
                    _verticalVelocity = verticalSpeed;

                    // 计算水平方向速度（匀速，抛物线水平分量）
                    float jumpDistance = Mathf.Lerp(minJumpDistance, maxJumpDistance, chargeRatio);
                    float airTime = 2 * verticalSpeed / gravityAbs; // 抛物线总空中时间（不计空气阻力）
                    float horizontalSpeedBase = jumpDistance / airTime;
                    _jumpHorizontalVelocity = _jumpDirection * horizontalSpeedBase * jumpHorizontalSpeedMultiplier;
                }
            }
            else
            {
                // 离地时取消蓄力
                _isChargingJump = false;
            }
        }

        private void Move()
        {
            Vector2 moveInput = _input?.move ?? Vector2.zero;
            bool hasMoveInput = moveInput.sqrMagnitude >= _threshold;

            // 目标速度计算
            float targetSpeed = 0f;
            if (Grounded && !_isJumping)
            {
                // 地面正常移动速度
                targetSpeed = hasMoveInput ? (_input?.sprint ?? false ? SprintSpeed : MoveSpeed) : 0f;
            }
            else if (!Grounded && !_isJumping)
            {
                // 非跳跃状态的空中移动（如坠落时的手动调整）
                targetSpeed = hasMoveInput ? AirMoveSpeed : 0f;
            }
            // 跳跃状态（_isJumping=true）时，targetSpeed为0，禁用手动水平移动

            // 角色旋转（仅地面有输入时）
            if (Grounded && hasMoveInput)
            {
                float cameraYaw = _mainCamera ? _mainCamera.transform.eulerAngles.y : 0f;
                _targetRotation = Mathf.Atan2(moveInput.x, moveInput.y) * Mathf.Rad2Deg + cameraYaw;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, rotation, 0f);
            }

            // 速度插值（地面移动）
            if (Grounded && !_isJumping)
            {
                if (hasMoveInput)
                {
                    _speed = Mathf.Lerp(_speed, targetSpeed, Time.deltaTime * SpeedChangeRate);
                    _speed = Mathf.Clamp(_speed, 0f, targetSpeed);
                }
                else
                {
                    _speed *= moveStopFriction;
                    if (_speed < minVelocityThreshold) _speed = 0f;
                }
            }

            // 移动向量计算
            Vector3 moveVector = Vector3.zero;
            if (Grounded && !_isJumping)
            {
                // 地面正常移动
                Vector3 moveDir = Quaternion.Euler(0f, _targetRotation, 0f) * Vector3.forward;
                moveDir.Normalize();
                moveVector = moveDir * _speed * Time.deltaTime + new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime;
            }
            else
            {
                // 空中移动：跳跃状态仅用抛物线速度，非跳跃状态可用手动移动
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

            _controller?.Move(moveVector);

            // 动画混合值更新（直接关联当前速度，加快响应）
            _animationBlend = Mathf.Lerp(_animationBlend, _speed, Time.deltaTime * SpeedChangeRate * 3f);
            if (_animationBlend < minVelocityThreshold) _animationBlend = 0f;
        }

        private void ApplyGravity()
        {
            if (Grounded)
            {
                _verticalVelocity = Mathf.Max(_verticalVelocity, GroundStickVelocity);
                _isFalling = false;
            }
            else
            {
                // 竖直方向匀加速（重力）
                if (_verticalVelocity > -_terminalVelocity)
                    _verticalVelocity += Gravity * Time.deltaTime;

                // 下落判断
                bool shouldFall = _verticalVelocity < 0f && _fallTimeoutDelta <= 0f;
                if (shouldFall && !_isFalling) _isFalling = true;
                else if (!shouldFall && _isFalling) _isFalling = false;
            }
        }

        private void UpdateAnimationParams()
        {
            if (!_hasAnimator) return;

            Vector2 moveInput = _input?.move ?? Vector2.zero;
            bool hasMoveInput = moveInput.sqrMagnitude >= _threshold;

            // 蓄力状态：强制Idle
            if (_isChargingJump)
            {
                _animator.SetFloat(_animIDSpeed, 0f);
                _animator.SetFloat(_animIDMotionSpeed, 0f);
                _animator.SetBool(_animIDGrounded, true);
                _animator.SetBool(_animIDJump, false);
                _animator.SetBool(_animIDFreeFall, false);
            }
            // 地面状态
            else if (Grounded)
            {
                _animator.SetBool(_animIDGrounded, true);
                _animator.SetBool(_animIDJump, false);
                _animator.SetBool(_animIDFreeFall, false);

                // 无输入时强制Idle，解决定格问题
                if (hasMoveInput)
                {
                    _animator.SetFloat(_animIDSpeed, _animationBlend);
                    _animator.SetFloat(_animIDMotionSpeed, moveInput.magnitude);
                }
                else
                {
                    _animator.SetFloat(_animIDSpeed, 0f);
                    _animator.SetFloat(_animIDMotionSpeed, 0f);
                }
            }
            // 空中状态
            else
            {
                _animator.SetBool(_animIDGrounded, false);
                if (_isJumping && !_isFalling)
                {
                    // 跳跃上升阶段
                    _animator.SetBool(_animIDJump, true);
                    _animator.SetBool(_animIDFreeFall, false);
                }
                else
                {
                    // 下落/自由落体阶段
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, true);
                }
                // 空中动画速度（简化）
                _animator.SetFloat(_animIDSpeed, hasMoveInput ? 0.5f : 0f);
                _animator.SetFloat(_animIDMotionSpeed, hasMoveInput ? moveInput.magnitude : 0f);
            }
        }

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
            // 地面检测Gizmo
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

            // 蓄力位置Gizmo
            if (_isChargingJump)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(_chargeStartPos, 0.3f);
            }

            // 跳跃方向Gizmo（抛物线轨迹预览）
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