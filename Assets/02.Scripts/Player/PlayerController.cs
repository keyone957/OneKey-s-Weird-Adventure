using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectAdventure
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _gravity = 25f;       // 자연스러운 점프 하강을 위해 중력 값 보정
        [SerializeField] private float _jumpHeight = 1.8f;    // 점프 높이

        [Header("Visual References")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Animator _animator;

        [Header("Weapon Settings")]
        [SerializeField] private SpriteRenderer _weaponSpriteRenderer;

        [Header("Attack Settings")]
        [SerializeField] private float _attackDuration = 0.4f;  // 공격 모션이 끝나는 시간 (Slash 애니메이션 시간)

        private CharacterController _characterController;
        private InputAction _moveAction;
        private InputAction _attackAction;
        private InputAction _jumpAction;
        private Vector2 _moveInput;
        private Vector3 _velocity;
        
        // 애니메이션 및 물리 상태 제어 변수
        private int _lastDirectionIndex = 0; // 0:S, 1:SE, 2:E, 3:NE, 4:N, 5:NW, 6:W, 7:SW
        private bool _isAttacking;
        private float _attackEndTime;
        private bool _lastHorizontalFacingLeft = false; // 기본값 우측

        // 공격 방향 보정을 위한 임시 백업
        private int _preAttackDirectionIndex;

        // Animator State Hashes for performance optimization
        private static readonly int HashSlashDownRight = Animator.StringToHash("Character_SlashDownRight");
        private static readonly int HashSlashDownLeft = Animator.StringToHash("Character_SlashDownLeft");
        private static readonly int HashSlashUpLeft = Animator.StringToHash("Character_SlashUpLeft");
        private static readonly int HashSlashUpRight = Animator.StringToHash("Character_SlashUpRight");

        private static readonly int HashRollDown = Animator.StringToHash("Character_RollDown");
        private static readonly int HashRollDownRight = Animator.StringToHash("Character_RollDownRight");
        private static readonly int HashRollRight = Animator.StringToHash("Character_RollRight");
        private static readonly int HashRollUpRight = Animator.StringToHash("Character_RollUpRight");
        private static readonly int HashRollUp = Animator.StringToHash("Character_RollUp");
        private static readonly int HashRollUpLeft = Animator.StringToHash("Character_RollUpLeft");
        private static readonly int HashRollLeft = Animator.StringToHash("Character_RollLeft");
        private static readonly int HashRollDownLeft = Animator.StringToHash("Character_RollDownLeft");

        private static readonly int HashWalkDown = Animator.StringToHash("Character_Down");
        private static readonly int HashWalkDownRight = Animator.StringToHash("Character_DownRight");
        private static readonly int HashWalkRight = Animator.StringToHash("Character_Right");
        private static readonly int HashWalkUpRight = Animator.StringToHash("Character_UpRight");
        private static readonly int HashWalkUp = Animator.StringToHash("Character_Up");
        private static readonly int HashWalkUpLeft = Animator.StringToHash("Character_UpLeft");
        private static readonly int HashWalkLeft = Animator.StringToHash("Character_Left");
        private static readonly int HashWalkDownLeft = Animator.StringToHash("Character_DownLeft");


        // 애니메이션 이벤트에서 호출됩니다. (현재는 애니메이션 클립 자체 키프레임을 사용하므로 비워둡니다)
        public void SetWeaponFrame(int frameIndex)
        {
        }

        public Vector2 MoveInput => _moveInput;
        public float CurrentSpeed => _characterController != null ? _characterController.velocity.magnitude : 0f;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();

            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            // Project-wide Input Actions 연결
            if (InputSystem.actions != null)
            {
                _moveAction = InputSystem.actions.FindAction("Player/Move");
                _attackAction = InputSystem.actions.FindAction("Player/Attack");
                _jumpAction = InputSystem.actions.FindAction("Player/Jump");
            }
            else
            {
                Debug.LogError("Project-wide Input Actions asset is not assigned under Project Settings.");
            }

            // 게임 시작 즉시 무기(칼) 비활성화 처리 (둥둥 떠있는 버그 방지)
            if (_weaponSpriteRenderer != null)
            {
                _weaponSpriteRenderer.enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_attackAction != null)
            {
                _attackAction.performed += OnAttackPerformed;
            }
        }

        private void OnDisable()
        {
            if (_attackAction != null)
            {
                _attackAction.performed -= OnAttackPerformed;
            }
        }

        private void OnAttackPerformed(InputAction.CallbackContext context)
        {
            OnAttack();
        }

        private void Update()
        {
            _moveInput = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
            
            // 공격 타이머 완료 검사 및 원래 보던 방향 복귀
            if (_isAttacking && Time.time >= _attackEndTime)
            {
                _isAttacking = false;
                _lastDirectionIndex = _preAttackDirectionIndex; // 회전 복구
            }

            MovePlayer();
            UpdateAnimation();
            UpdateWeaponPlacement();
        }

        private void MovePlayer()
        {
            bool isGrounded = _characterController.isGrounded;

            if (isGrounded)
            {
                _velocity.y = -0.5f;

                // 접지 상태일 때 스페이스바 입력 감지하여 점프 개시
                if (_jumpAction != null && _jumpAction.triggered && !_isAttacking)
                {
                    _velocity.y = Mathf.Sqrt(_jumpHeight * 2.0f * _gravity);
                }
            }
            else
            {
                _velocity.y -= _gravity * Time.deltaTime;
            }

            Vector3 dir = Vector3.zero;
            
            // 공격 애니메이션 중에는 횡방향 움직임 제한
            if (!_isAttacking)
            {
                dir = new Vector3(_moveInput.x, 0f, _moveInput.y);
                if (dir.sqrMagnitude > 1f) dir.Normalize();
                dir *= _moveSpeed;
            }

            dir.y = _velocity.y;
            _characterController.Move(dir * Time.deltaTime);
        }

        private void OnAttack()
        {
            if (_isAttacking) return;

            _isAttacking = true;
            _animator.speed = 1f;

            // 공격 개시 전 바라보던 본래 방향 백업
            _preAttackDirectionIndex = _lastDirectionIndex;

            // 공격 모션의 방향성 결정 및 애니메이터 실행
            int attackStateHash = HashSlashDownRight;

            switch (_lastDirectionIndex)
            {
                case 4: // N
                    attackStateHash = HashSlashUpRight;
                    break;
                case 5: // NW
                case 6: // W
                    attackStateHash = HashSlashUpLeft;
                    break;
                case 1: // SE
                case 2: // E
                case 3: // NE
                    attackStateHash = HashSlashDownRight;
                    break;
                case 0: // S
                case 7: // SW
                    attackStateHash = HashSlashDownLeft;
                    break;
            }

            if (_animator != null)
            {
                _animator.speed = 1f;
                _animator.Play(attackStateHash, 0, 0f);
            }

            if (_spriteRenderer != null)
            {
                _spriteRenderer.flipX = false; // 이미지 자체로 분할 제작되었으므로 flipX 비활성화
            }

            _attackEndTime = Time.time + _attackDuration;
        }

        private void UpdateAnimation()
        {
            if (_animator == null) return;

            // 공격 애니메이션 도중에는 다른 애니메이션으로 상태 덮어쓰기 방지
            if (_isAttacking) return;

            bool isGrounded = _characterController.isGrounded;
            bool isMoving = _moveInput.sqrMagnitude > 0.01f;

            if (isMoving)
            {
                // 이동 방향 변경
                float angle = Mathf.Atan2(_moveInput.y, _moveInput.x) * Mathf.Rad2Deg;
                float adjusted = (angle + 90f + 360f) % 360f;
                int dir = Mathf.RoundToInt(adjusted / 45f) % 8;
                _lastDirectionIndex = dir;

                // 최근 횡방향 기억
                if (dir == 2 || dir == 1 || dir == 3) _lastHorizontalFacingLeft = false;
                else if (dir == 6 || dir == 5 || dir == 7) _lastHorizontalFacingLeft = true;
            }

            if (!isGrounded)
            {
                // 1. 공중 상태 (점프) ➔ 구르기(Roll) 애니메이션 8방향 매핑
                _animator.speed = 1f;
                int rollStateHash = HashRollDown;

                switch (_lastDirectionIndex)
                {
                    case 0: rollStateHash = HashRollDown; break;
                    case 1: rollStateHash = HashRollDownRight; break;
                    case 2: rollStateHash = HashRollRight; break;
                    case 3: rollStateHash = HashRollUpRight; break;
                    case 4: rollStateHash = HashRollUp; break;
                    case 5: rollStateHash = HashRollUpLeft; break;
                    case 6: rollStateHash = HashRollLeft; break;
                    case 7: rollStateHash = HashRollDownLeft; break;
                }

                _animator.Play(rollStateHash);
                if (_spriteRenderer != null) _spriteRenderer.flipX = false;
            }
            else
            {
                // 2. 지상 상태 (걷기 및 대기)
                if (isMoving)
                {
                    _animator.speed = 1f;
                    int walkStateHash = HashWalkDown;

                    switch (_lastDirectionIndex)
                    {
                        case 0: walkStateHash = HashWalkDown; break;
                        case 1: walkStateHash = HashWalkDownRight; break;
                        case 2: walkStateHash = HashWalkRight; break;
                        case 3: walkStateHash = HashWalkUpRight; break;
                        case 4: walkStateHash = HashWalkUp; break;
                        case 5: walkStateHash = HashWalkUpLeft; break;
                        case 6: walkStateHash = HashWalkLeft; break;
                        case 7: walkStateHash = HashWalkDownLeft; break;
                    }

                    _animator.Play(walkStateHash);
                }
                else
                {
                    // 대기 상태 ➔ 해당하는 방향 걷기 애니메이션의 0번째 프레임 상태에서 멈춤(speed = 0)
                    int idleStateHash = HashWalkDown;

                    switch (_lastDirectionIndex)
                    {
                        case 0: idleStateHash = HashWalkDown; break;
                        case 1: idleStateHash = HashWalkDownRight; break;
                        case 2: idleStateHash = HashWalkRight; break;
                        case 3: idleStateHash = HashWalkUpRight; break;
                        case 4: idleStateHash = HashWalkUp; break;
                        case 5: idleStateHash = HashWalkUpLeft; break;
                        case 6: idleStateHash = HashWalkLeft; break;
                        case 7: idleStateHash = HashWalkDownLeft; break;
                    }

                    _animator.Play(idleStateHash, 0, 0f);
                    _animator.speed = 0f;
                }

                if (_spriteRenderer != null) _spriteRenderer.flipX = false;
            }
        }

        private void UpdateWeaponPlacement()
        {
            if (_weaponSpriteRenderer == null) return;

            // 1. 공격 중이 아닐 때는 무기 스프라이트 숨김
            if (!_isAttacking)
            {
                _weaponSpriteRenderer.enabled = false;
                return;
            }

            // 2. 공격 동작 중에만 활성화
            _weaponSpriteRenderer.enabled = true;

            int baseOrder = _spriteRenderer != null ? _spriteRenderer.sortingOrder : 0;
            int orderOffset = 1;

            // 위로 베는 방향(N: 4, NW: 5, W: 6)일 때는 무기가 몸 뒤로 가도록 렌더링 순서 변경
            if (_lastDirectionIndex == 4 || _lastDirectionIndex == 5 || _lastDirectionIndex == 6)
            {
                orderOffset = -1;
            }
            
            _weaponSpriteRenderer.sortingOrder = baseOrder + orderOffset;
        }
    }
}
