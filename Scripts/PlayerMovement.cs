using UnityEngine;

public class PlayerMovement : MonoBehaviour {
    [Header("References")]
    [SerializeField] private Collider2D _feetColl;
    [SerializeField] private Collider2D _bodyColl;
    public PlayerMovementStats MoveStats;

    private Rigidbody2D _rb;

    private Vector2 _moveVelocity;
    private bool _isFacingRight;

    private RaycastHit2D _groundedHit;
    private RaycastHit2D _headHit;
    private bool _isGrounded;
    private bool _bumpedHead;

    public float VerticalVelocity { get; private set; }
    private bool _isJumping;
    private bool _isFastFalling;
    private bool _isFalling;
    private float _fastFallTime;
    private float _fastFallReleaseSpeed;
    private int _numberOfJumpsUsed;

    private float _apexPoint;
    private float _timePastApexThreshold;
    private bool _isPastApexTreshold;

    private float _jumpBufferTimer;
    private bool _jumpReleaseDuringBuffer;

    private float _coyoteTimer;

    private void Awake() {
        _isFacingRight = true;
        _rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate() {
        CollisionChecks();
        Jump();

        if (_isGrounded) {
            Move(MoveStats.GroundAcceleration, MoveStats.GroundDeceleration, InputManager.Movement);
        } else {
            Move(MoveStats.AirAcceleration, MoveStats.AirAcceleration, InputManager.Movement);
        }
    }

    private void Update() {
        CountTimers();
        JumpChecks();
    }

    private void Move(float acceleration, float decerelation, Vector2 moveInput) {
        if (moveInput != Vector2.zero) {
            TurnCheck(moveInput);

            Vector2 targetVelocity = Vector2.zero;

            targetVelocity = new Vector2(moveInput.x, 0f) * MoveStats.MaxSpeed;
            _moveVelocity = Vector2.Lerp(_moveVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
            _rb.velocity = new Vector2(_moveVelocity.x, _rb.velocity.y);
        } else if (moveInput == Vector2.zero) {
            _moveVelocity = Vector2.Lerp(_moveVelocity, Vector2.zero, decerelation * Time.fixedDeltaTime);
            _rb.velocity = new Vector2(_moveVelocity.x, _rb.velocity.y);
        }
    }

    private void TurnCheck(Vector2 moveInput) {
        if (_isFacingRight && moveInput.x < 0) {
            Turn(false);
        } else if (!_isFacingRight && moveInput.x > 0) {
            Turn(true);
        }
    }

    private void Turn(bool turnRight) {
        if (turnRight) {
            _isFacingRight = true;
            transform.Rotate(0f, 180f, 0f);
        } else {
            _isFacingRight = false;
            transform.Rotate(0f, -180f, 0f);
        }
    }

    private void IsGrounded() {
        Vector2 boxCastOrigin = new Vector2(_feetColl.bounds.center.x, _feetColl.bounds.min.y);
        Vector2 boxCastSize = new Vector2(_feetColl.bounds.size.x, MoveStats.GroundDetectionRayLength);

        _groundedHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.down, MoveStats.GroundDetectionRayLength, MoveStats.GroundLayer);

        if (_groundedHit.collider != null) {
            _isGrounded = true;
        } else {
            _isGrounded = false;
        }

        Color rayColor;

        if (_isGrounded) {
            rayColor = Color.green;
        }
        else {
            rayColor = Color.red;
        }

        Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y), Vector2.down * MoveStats.GroundDetectionRayLength, rayColor);
        Debug.DrawRay(new Vector2(boxCastOrigin.x + boxCastSize.x / 2, boxCastOrigin.y), Vector2.down * MoveStats.GroundDetectionRayLength, rayColor);
        Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y - MoveStats.GroundDetectionRayLength), Vector2.right * boxCastSize.x, rayColor);
    }

    private void CollisionChecks() {
        IsGrounded();
    }

    private void JumpChecks() {
        if (InputManager.JumpWasPressed) {
            _jumpBufferTimer = MoveStats.JumpBufferTime;
            _jumpReleaseDuringBuffer = false;
        }

        if (InputManager.JumpWasReleased) {
            if (_jumpBufferTimer > 0f) {
                _jumpReleaseDuringBuffer = true;
            }

            if (_isJumping && VerticalVelocity > 0f) {
                if (_isPastApexTreshold) {
                    _isPastApexTreshold = false;
                    _isFastFalling = true;
                    _fastFallTime = MoveStats.TimeForUpwardsCancel;
                    VerticalVelocity = 0f;
                } else {
                    _isFastFalling = true;
                    _fastFallReleaseSpeed = VerticalVelocity;
                }
            }
        }

        if (_jumpBufferTimer > 0f && !_isJumping && (_isGrounded || _coyoteTimer > 0f)) {
            InitiateJump(1);

            if (_jumpReleaseDuringBuffer) {
                _isFastFalling = true;
                _fastFallReleaseSpeed = VerticalVelocity;
            }
        }

        else if (_jumpBufferTimer > 0f && _isJumping && _numberOfJumpsUsed < MoveStats.NumberOfJumpsAllowed) {
            _isFastFalling = false;
            InitiateJump(1);
        }

        else if (_jumpBufferTimer > 0f && _isFalling && _numberOfJumpsUsed < MoveStats.NumberOfJumpsAllowed) {
            InitiateJump(2);
            _isFastFalling = false;
        }

        if ((_isJumping || _isFalling) && _isGrounded && VerticalVelocity <= 0f) {
            _isJumping = false;
            _isFalling = false;
            _isFastFalling = false;
            _fastFallTime = 0f;
            _isPastApexTreshold = false;
            _numberOfJumpsUsed = 0;

            VerticalVelocity = Physics2D.gravity.y;
        }
    }

    private void Jump() {

    }

    private void InitiateJump(int numberOfJumpsUsed) {
        if (!_isJumping) {
            _isJumping = true;
        }

        _jumpBufferTimer = 0f;
        _numberOfJumpsUsed += numberOfJumpsUsed;
        VerticalVelocity = MoveStats.InitialJumpVelocity;
    }

    private void CountTimers() {
        _jumpBufferTimer -= Time.deltaTime;

        if (!_isGrounded) {
            _coyoteTimer -= Time.deltaTime;
        } else {
            _coyoteTimer = MoveStats.JumpCoyoteTime;
        }
    }
}
