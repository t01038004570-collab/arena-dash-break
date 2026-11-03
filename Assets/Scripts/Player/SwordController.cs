using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaBreak.Player
{
    public class SwordController : MonoBehaviour
    {
        private const string ActionMapName = "Player";
        private const string AttackActionName = "Attack";

        [Header("Input")]
        [SerializeField] private InputActionAsset _inputActions;

        [Header("Sword")]
        [SerializeField] private Transform _swordPivot;
        [SerializeField] private float _swingDuration = 0.3f;
        [SerializeField] private float _swingAngle = 90f;

        private InputAction _attackAction;
        private Quaternion _restRotation;
        private bool _isSwinging;
        private float _swingStartTime;

        private void Awake()
        {
            if (_inputActions == null)
            {
                Debug.LogError($"{nameof(SwordController)}: InputActionAsset이 할당되지 않았습니다.", this);
                enabled = false;
                return;
            }

            _attackAction = _inputActions.FindActionMap(ActionMapName, true).FindAction(AttackActionName, true);

            // 휘두른 뒤 정확히 원래 자세로 돌아오려고 씬에서 잡아둔 각도를 기준으로 삼는다
            _restRotation = _swordPivot.localRotation;
        }

        private void OnEnable()
        {
            _attackAction.Enable();
        }

        private void OnDisable()
        {
            if (_attackAction == null)
            {
                return;
            }

            _attackAction.Disable();
            ResetSword();
        }

        private void Update()
        {
            if (_isSwinging)
            {
                UpdateSwing();
                return;
            }

            // 누르고 있는 동안 반복되면 연타 없이도 계속 휘둘러져서 눌린 프레임만 본다
            if (_attackAction.WasPressedThisFrame())
            {
                _isSwinging = true;
                _swingStartTime = Time.time;
            }
        }

        private void UpdateSwing()
        {
            float progress = (Time.time - _swingStartTime) / _swingDuration;

            if (progress >= 1f)
            {
                ResetSword();
                return;
            }

            // sin 한 주기의 절반이라 올라갔다 내려오는 왕복이 끊김 없이 이어진다
            float angle = Mathf.Sin(progress * Mathf.PI) * _swingAngle;
            _swordPivot.localRotation = _restRotation * Quaternion.AngleAxis(angle, Vector3.right);
        }

        private void ResetSword()
        {
            _isSwinging = false;
            _swordPivot.localRotation = _restRotation;
        }
    }
}
