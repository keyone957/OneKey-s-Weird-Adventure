using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectAdventure
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _gravity = 9.81f;

        [Header("Visual References")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Animator _animator;

        private CharacterController _characterController;
        private InputAction _moveAction;
        private Vector2 _moveInput;
        private Vector3 _velocity;
        private int _lastDirectionIndex = 0; // 0:S, 1:SE, 2:E, 3:NE, 4:N, 5:NW, 6:W, 7:SW

        public Vector2 MoveInput => _moveInput;
        public float CurrentSpeed => _characterController != null ? _characterController.velocity.magnitude : 0f;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();

            // Animator 자동 바인딩 시도
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            _moveAction = new InputAction("Move");
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up",    "<Keyboard>/w")
                .With("Down",  "<Keyboard>/s")
                .With("Left",  "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up",    "<Gamepad>/leftStick/up")
                .With("Down",  "<Gamepad>/leftStick/down")
                .With("Left",  "<Gamepad>/leftStick/left")
                .With("Right", "<Gamepad>/leftStick/right");
        }

        private void OnEnable()  => _moveAction?.Enable();
        private void OnDisable() => _moveAction?.Disable();

        private void Update()
        {
            _moveInput = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
            MovePlayer();
            UpdateAnimation();
        }

        private void MovePlayer()
        {
            Vector3 dir = new Vector3(_moveInput.x, 0f, _moveInput.y);
            if (dir.sqrMagnitude > 1f) dir.Normalize();

            if (_characterController.isGrounded)
                _velocity.y = -0.5f;
            else
                _velocity.y -= _gravity * Time.deltaTime;

            dir.y = _velocity.y;
            _characterController.Move(dir * _moveSpeed * Time.deltaTime);
        }

        private void UpdateAnimation()
        {
            if (_animator == null) return;

            bool isMoving = _moveInput.sqrMagnitude > 0.01f;

            if (isMoving)
            {
                _animator.speed = 1f;

                // 입력 각도 계산 (x=우측, y=상단)
                float angle = Mathf.Atan2(_moveInput.y, _moveInput.x) * Mathf.Rad2Deg;
                float adjusted = (angle + 90f + 360f) % 360f;
                
                // 8방향 분할 (0:S, 1:SE, 2:E, 3:NE, 4:N, 5:NW, 6:W, 7:SW)
                int dir = Mathf.RoundToInt(adjusted / 45f) % 8;
                _lastDirectionIndex = dir;

                switch (dir)
                {
                    case 0: _animator.Play("Walk_Down"); break;
                    case 1: _animator.Play("Walk_Down_Right"); break;
                    case 2: _animator.Play("Walk_Right"); break;
                    case 3: _animator.Play("Walk_Up_Right"); break;
                    case 4: _animator.Play("Walk_Up"); break;
                    case 5: _animator.Play("Walk_Up_Left"); break;
                    case 6: _animator.Play("Walk_Left"); break;
                    case 7: _animator.Play("Walk_Down_Left"); break;
                }

                if (_spriteRenderer != null)
                {
                    _spriteRenderer.flipX = false; // 걷기 애니메이션 자체에 좌우 방향이 다 있으므로 flipX는 비활성화
                }
            }
            else
            {
                // 정지 시 마지막 정면/후면 방향이면 걷기 프레임 첫 컷 고정, 좌우/대각선 방향이면 Idle 루프 재생
                switch (_lastDirectionIndex)
                {
                    case 0: // 남(S) 대기
                        _animator.Play("Walk_Down", 0, 0f);
                        _animator.speed = 0f;
                        if (_spriteRenderer != null) _spriteRenderer.flipX = false;
                        break;
                    case 4: // 북(N) 대기
                        _animator.Play("Walk_Up", 0, 0f);
                        _animator.speed = 0f;
                        if (_spriteRenderer != null) _spriteRenderer.flipX = false;
                        break;
                    case 1: // SE 대기
                    case 2: // E 대기
                    case 3: // NE 대기
                        _animator.Play("Idle");
                        _animator.speed = 1f;
                        if (_spriteRenderer != null) _spriteRenderer.flipX = false;
                        break;
                    case 5: // NW 대기
                    case 6: // W 대기
                    case 7: // SW 대기
                        _animator.Play("Idle");
                        _animator.speed = 1f;
                        if (_spriteRenderer != null) _spriteRenderer.flipX = true; // Idle은 오른쪽 기준이므로 반전
                        break;
                }
            }
        }
    }
}
