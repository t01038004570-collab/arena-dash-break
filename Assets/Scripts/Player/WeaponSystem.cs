using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaBreak.Player
{
    public class WeaponSystem : MonoBehaviour
    {
        private const string ActionMapName = "Player";
        private const string AttackActionName = "Attack";
        private const string ReloadBinding = "<Keyboard>/r";

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Header("Input")]
        [SerializeField] private InputActionAsset _inputActions;

        [Header("Aim")]
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private LayerMask _hitMask = ~0;

        [Header("Fire")]
        [SerializeField] private float _fireInterval = 0.15f;
        [SerializeField] private float _range = 50f;

        [Header("Ammo")]
        [SerializeField] private int _maxAmmo = 12;
        [SerializeField] private float _reloadDuration = 1.5f;

        [Header("Hit Marker")]
        [SerializeField] private Color _hitMarkerColor = Color.yellow;
        [SerializeField] private float _hitMarkerSize = 0.2f;
        [SerializeField] private float _hitMarkerDuration = 0.5f;

        private InputAction _attackAction;
        private InputAction _reloadAction;
        private MaterialPropertyBlock _markerPropertyBlock;

        private int _currentAmmo;
        private bool _isReloading;
        private float _nextFireTime;
        private float _reloadEndTime;

        private void Awake()
        {
            if (_inputActions == null)
            {
                Debug.LogError($"{nameof(WeaponSystem)}: InputActionAsset이 할당되지 않았습니다.", this);
                enabled = false;
                return;
            }

            InputActionMap playerMap = _inputActions.FindActionMap(ActionMapName, true);
            _attackAction = playerMap.FindAction(AttackActionName, true);

            // 에셋에 Reload 액션이 없어서 코드에서 직접 만든다
            _reloadAction = new InputAction("Reload", InputActionType.Button, ReloadBinding);

            _markerPropertyBlock = new MaterialPropertyBlock();
            _currentAmmo = _maxAmmo;
        }

        private void OnEnable()
        {
            // 맵 전체가 아니라 액션만 켜서, PlayerController가 맵을 끌 때 상태를 건드리지 않는다
            _attackAction.Enable();
            _reloadAction.Enable();
        }

        private void OnDisable()
        {
            if (_attackAction == null)
            {
                return;
            }

            _attackAction.Disable();
            _reloadAction.Disable();
        }

        private void OnDestroy()
        {
            // 코드로 만든 액션은 에셋 소유가 아니라서 직접 해제해야 한다
            _reloadAction?.Dispose();
        }

        private void Update()
        {
            if (_isReloading)
            {
                if (Time.time >= _reloadEndTime)
                {
                    FinishReload();
                }

                return;
            }

            if (_reloadAction.WasPressedThisFrame() && _currentAmmo < _maxAmmo)
            {
                StartReload();
                return;
            }

            if (_attackAction.IsPressed() && _currentAmmo > 0 && Time.time >= _nextFireTime)
            {
                Fire();
            }
        }

        private void Fire()
        {
            _nextFireTime = Time.time + _fireInterval;
            _currentAmmo--;

            Ray ray = new Ray(_cameraTransform.position, _cameraTransform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, _range, _hitMask, QueryTriggerInteraction.Ignore))
            {
                SpawnHitMarker(hit.point);
            }

            Debug.Log($"[Weapon] 발사 - 남은 탄약 {_currentAmmo}/{_maxAmmo}");
        }

        private void StartReload()
        {
            _isReloading = true;
            _reloadEndTime = Time.time + _reloadDuration;

            Debug.Log($"[Weapon] 재장전 시작 - 남은 탄약 {_currentAmmo}/{_maxAmmo}");
        }

        private void FinishReload()
        {
            _isReloading = false;
            _currentAmmo = _maxAmmo;

            Debug.Log($"[Weapon] 재장전 완료 - 남은 탄약 {_currentAmmo}/{_maxAmmo}");
        }

        // 연사 중 생성/파괴가 반복되므로 풀링 대상
        private void SpawnHitMarker(Vector3 position)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "HitMarker";
            marker.transform.position = position;
            marker.transform.localScale = Vector3.one * _hitMarkerSize;

            // 콜라이더가 남으면 다음 Raycast가 마커에 맞는다
            Destroy(marker.GetComponent<Collider>());

            // material 인스턴스를 만들면 마커가 파괴돼도 머티리얼이 남아 누수된다
            Renderer markerRenderer = marker.GetComponent<Renderer>();
            _markerPropertyBlock.SetColor(BaseColorId, _hitMarkerColor);
            markerRenderer.SetPropertyBlock(_markerPropertyBlock);

            Destroy(marker, _hitMarkerDuration);
        }
    }
}
