using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaBreak.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        private const string ActionMapName = "Player";
        private const string MoveActionName = "Move";
        private const string LookActionName = "Look";
        private const string JumpActionName = "Jump";

        [Header("Input")]
        [SerializeField] private InputActionAsset _inputActions;

        [Header("Camera")]
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private float _pitchLimit = 80f;

        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _jumpHeight = 1.2f;
        [SerializeField] private float _gravity = -20f;
        [SerializeField] private float _groundedStickVelocity = -2f;

        [Header("Look")]
        [SerializeField] private float _lookSensitivity = 0.1f;
        [SerializeField] private float _maxLookDeltaPerFrame = 50f;

        [Header("Cursor")]
        [SerializeField] private bool _lockCursor = true;

        private CharacterController _characterController;
        private InputActionMap _playerMap;
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _jumpAction;

        private float _pitch;
        private float _verticalVelocity;

        private CursorLockMode _previousLockState;
        private bool _previousCursorVisible;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();

            if (_inputActions == null)
            {
                Debug.LogError($"{nameof(PlayerController)}: InputActionAsset이 할당되지 않았습니다.", this);
                enabled = false;
                return;
            }

            _playerMap = _inputActions.FindActionMap(ActionMapName, true);
            _moveAction = _playerMap.FindAction(MoveActionName, true);
            _lookAction = _playerMap.FindAction(LookActionName, true);
            _jumpAction = _playerMap.FindAction(JumpActionName, true);
        }

        private void OnEnable()
        {
            _playerMap.Enable();

            if (_lockCursor)
            {
                _previousLockState = Cursor.lockState;
                _previousCursorVisible = Cursor.visible;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void OnDisable()
        {
            // Awake에서 에셋 누락으로 조기 종료되면 맵이 없다
            if (_playerMap == null)
            {
                return;
            }

            _playerMap.Disable();

            if (_lockCursor)
            {
                Cursor.lockState = _previousLockState;
                Cursor.visible = _previousCursorVisible;
            }
        }

        private void Update()
        {
            Look();
            Move();
        }

        private void Look()
        {
            Vector2 delta = _lookAction.ReadValue<Vector2>();

            // 커서가 잠기는 첫 프레임에 delta가 튀어 시점이 순간 회전하는 것을 막는다
            delta = Vector2.ClampMagnitude(delta, _maxLookDeltaPerFrame);

            // 마우스 delta는 이미 프레임당 변위라서 deltaTime을 곱하면 프레임레이트에 따라 감도가 변한다
            transform.Rotate(Vector3.up, delta.x * _lookSensitivity, Space.Self);

            _pitch = Mathf.Clamp(_pitch - delta.y * _lookSensitivity, -_pitchLimit, _pitchLimit);
            _cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void Move()
        {
            bool isGrounded = _characterController.isGrounded;

            // 0으로 두면 경사·모서리에서 isGrounded가 프레임마다 깜빡인다
            if (isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = _groundedStickVelocity;
            }

            if (isGrounded && _jumpAction.WasPressedThisFrame())
            {
                _verticalVelocity = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
            }

            _verticalVelocity += _gravity * Time.deltaTime;

            Vector2 input = _moveAction.ReadValue<Vector2>();
            Vector3 horizontal = (transform.right * input.x + transform.forward * input.y) * _moveSpeed;
            Vector3 velocity = horizontal + Vector3.up * _verticalVelocity;

            _characterController.Move(velocity * Time.deltaTime);
        }
    }
}
