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

        [Header("8방향 걷기 애니메이션 (4프레임)")]
        [SerializeField] private Sprite[] _animWalkS;
        [SerializeField] private Sprite[] _animWalkSE;
        [SerializeField] private Sprite[] _animWalkE;
        [SerializeField] private Sprite[] _animWalkNE;
        [SerializeField] private Sprite[] _animWalkN;
        [SerializeField] private Sprite[] _animWalkNW;
        [SerializeField] private Sprite[] _animWalkW;
        [SerializeField] private Sprite[] _animWalkSW;

        [Header("8방향 점프/구르기 애니메이션 (4프레임)")]
        [SerializeField] private Sprite[] _animRollS;
        [SerializeField] private Sprite[] _animRollSE;
        [SerializeField] private Sprite[] _animRollE;
        [SerializeField] private Sprite[] _animRollNE;
        [SerializeField] private Sprite[] _animRollN;
        [SerializeField] private Sprite[] _animRollNW;
        [SerializeField] private Sprite[] _animRollW;
        [SerializeField] private Sprite[] _animRollSW;

        [Header("4방향 공격(Slash) 애니메이션 (5프레임)")]
        [SerializeField] private Sprite[] _animSlashDownLeft;
        [SerializeField] private Sprite[] _animSlashDownRight;
        [SerializeField] private Sprite[] _animSlashUpLeft;
        [SerializeField] private Sprite[] _animSlashUpRight;

        [Header("애니메이션 속도 설정")]
        [SerializeField] private float _walkFrameRate = 0.12f;
        [SerializeField] private float _attackFrameRate = 0.08f;

        private CharacterController _characterController;
        private InputAction _moveAction;
        private InputAction _attackAction;
        private InputAction _jumpAction;
        private Vector2 _moveInput;
        private Vector3 _velocity;
        
        // 애니메이션 재생 상태 변수
        private int _lastDirectionIndex = 0; // 0:S, 1:SE, 2:E, 3:NE, 4:N, 5:NW, 6:W, 7:SW
        private bool _isAttacking;
        private float _attackEndTime;
        
        private float _animationTimer;
        private int _currentFrameIndex;
        private bool _lastHorizontalFacingLeft = false; // 기본값 우측

        public Vector2 MoveInput => _moveInput;
        public float CurrentSpeed => _characterController != null ? _characterController.velocity.magnitude : 0f;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();

            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            // 이동 입력 바인딩
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

            // 공격 입력 바인딩
            _attackAction = new InputAction("Attack", binding: "<Mouse>/leftButton");
            _attackAction.AddBinding("<Gamepad>/buttonWest");
            _attackAction.performed += ctx => OnAttack();

            // 점프 입력 바인딩 (스페이스바 및 게임패드 South 버튼)
            _jumpAction = new InputAction("Jump", binding: "<Keyboard>/space");
            _jumpAction.AddBinding("<Gamepad>/buttonSouth");
        }

        private void OnEnable()
        {
            _moveAction?.Enable();
            _attackAction?.Enable();
            _jumpAction?.Enable();
        }

        private void OnDisable()
        {
            _moveAction?.Disable();
            _attackAction?.Disable();
            _jumpAction?.Disable();
        }

        private void Update()
        {
            _moveInput = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
            
            // 공격 타이머 완료 검사
            if (_isAttacking && Time.time >= _attackEndTime)
            {
                _isAttacking = false;
                _currentFrameIndex = 0;
                _animationTimer = 0f;
            }

            MovePlayer();
            UpdateAnimation();
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
                    _currentFrameIndex = 0; // 점프 모션 첫 프레임부터 재생 유도
                    _animationTimer = 0f;
                }
            }
            else
            {
                // 공중 상태에서는 중력 가속도 적용
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
            _currentFrameIndex = 0;
            _animationTimer = 0f;

            // 베기 애니메이션은 총 5프레임이므로 총 소요시간 계산
            _attackEndTime = Time.time + (_attackFrameRate * 5f);
        }

        private void UpdateAnimation()
        {
            if (_spriteRenderer == null) return;

            Sprite[] activeFrames = null;
            bool flipX = false;
            float currentFrameRate = _walkFrameRate;
            bool isGrounded = _characterController.isGrounded;

            if (_isAttacking)
            {
                currentFrameRate = _attackFrameRate;
                
                // 공격 시 시선 방향에 기반한 4방향 베기(Slash) 애니메이션 매핑
                switch (_lastDirectionIndex)
                {
                    case 0:
                        activeFrames = _lastHorizontalFacingLeft ? _animSlashDownLeft : _animSlashDownRight;
                        break;
                    case 4:
                        activeFrames = _lastHorizontalFacingLeft ? _animSlashUpLeft : _animSlashUpRight;
                        break;
                    case 1:
                    case 2:
                    case 3:
                        activeFrames = (_lastDirectionIndex == 3) ? _animSlashUpRight : _animSlashDownRight;
                        break;
                    case 5:
                    case 6:
                    case 7:
                        activeFrames = (_lastDirectionIndex == 5) ? _animSlashUpLeft : _animSlashDownLeft;
                        break;
                }
            }
            else if (!isGrounded)
            {
                // 공중 상태(점프)일 때는 구르기(Roll) 애니메이션 적용
                currentFrameRate = _walkFrameRate;

                // 점프 중에도 방향 키를 누르면 시선 방향 변경
                bool isMoving = _moveInput.sqrMagnitude > 0.01f;
                if (isMoving)
                {
                    float angle = Mathf.Atan2(_moveInput.y, _moveInput.x) * Mathf.Rad2Deg;
                    float adjusted = (angle + 90f + 360f) % 360f;
                    int dir = Mathf.RoundToInt(adjusted / 45f) % 8;
                    _lastDirectionIndex = dir;
                }

                switch (_lastDirectionIndex)
                {
                    case 0: activeFrames = _animRollS;  break;
                    case 1: activeFrames = _animRollSE; break;
                    case 2: activeFrames = _animRollE;  break;
                    case 3: activeFrames = _animRollNE; break;
                    case 4: activeFrames = _animRollN;  break;
                    case 5: activeFrames = _animRollNW; break;
                    case 6: activeFrames = _animRollW;  break;
                    case 7: activeFrames = _animRollSW; break;
                }
            }
            else
            {
                // 지상 걷기 / 대기 애니메이션 제어
                bool isMoving = _moveInput.sqrMagnitude > 0.01f;

                if (isMoving)
                {
                    float angle = Mathf.Atan2(_moveInput.y, _moveInput.x) * Mathf.Rad2Deg;
                    float adjusted = (angle + 90f + 360f) % 360f;
                    int dir = Mathf.RoundToInt(adjusted / 45f) % 8;
                    _lastDirectionIndex = dir;

                    if (dir == 2 || dir == 1 || dir == 3) _lastHorizontalFacingLeft = false;
                    else if (dir == 6 || dir == 5 || dir == 7) _lastHorizontalFacingLeft = true;

                    switch (dir)
                    {
                        case 0: activeFrames = _animWalkS;  break;
                        case 1: activeFrames = _animWalkSE; break;
                        case 2: activeFrames = _animWalkE;  break;
                        case 3: activeFrames = _animWalkNE; break;
                        case 4: activeFrames = _animWalkN;  break;
                        case 5: activeFrames = _animWalkNW; break;
                        case 6: activeFrames = _animWalkW;  break;
                        case 7: activeFrames = _animWalkSW; break;
                    }
                }
                else
                {
                    // 대기 상태 고정
                    Sprite[] standFrames = null;
                    switch (_lastDirectionIndex)
                    {
                        case 0: standFrames = _animWalkS;  break;
                        case 1: standFrames = _animWalkSE; break;
                        case 2: standFrames = _animWalkE;  break;
                        case 3: standFrames = _animWalkNE; break;
                        case 4: standFrames = _animWalkN;  break;
                        case 5: standFrames = _animWalkNW; break;
                        case 6: standFrames = _animWalkW;  break;
                        case 7: standFrames = _animWalkSW; break;
                    }

                    if (standFrames != null && standFrames.Length > 0)
                    {
                        _spriteRenderer.sprite = standFrames[0];
                    }
                    _spriteRenderer.flipX = false;
                    return;
                }
            }

            // 프레임 애니메이팅 처리
            if (activeFrames != null && activeFrames.Length > 0)
            {
                _animationTimer += Time.deltaTime;
                if (_animationTimer >= currentFrameRate)
                {
                    _animationTimer -= currentFrameRate;
                    _currentFrameIndex = (_currentFrameIndex + 1) % activeFrames.Length;
                }

                _currentFrameIndex %= activeFrames.Length;
                _spriteRenderer.sprite = activeFrames[_currentFrameIndex];
                _spriteRenderer.flipX = flipX;
            }
        }
    }
}
