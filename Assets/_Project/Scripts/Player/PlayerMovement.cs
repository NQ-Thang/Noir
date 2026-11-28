using System.Collections;
using UnityEngine;

/// <summary>
/// Mảnh ghép quản lý di chuyển cơ bản của Người chơi: 
/// Chạy ngang, Nhảy (Variable Height + Apex Modifiers + Jump Buffer + Coyote Time + Ledge Corner Correction), Trượt tường, Bẩy tường.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Header("Ori-Style Horizontal Movement")]
    [SerializeField] private float moveSpeed = 8.5f;
    [SerializeField] private float groundAccelTime = 0.08f;
    [SerializeField] private float turnAccelTime = 0.05f;
    [Range(0.1f, 1f)]
    [SerializeField] private float airControlMultiplier = 0.8f;

    [Header("Physics-Driven Jump & Gravity")]
    [SerializeField] private float jumpHeight = 3.8f;
    [SerializeField] private float timeToJumpApex = 0.38f;
    [SerializeField] private float fallMultiplier = 1.7f;
    [Range(0.1f, 1f)]
    [SerializeField] private float jumpCutMultiplier = 0.45f;
    [SerializeField] private float maxFallSpeed = 19f;

    [Header("Wall Movement")]
    [SerializeField] private float wallSlideSpeed = 2.8f;
    [SerializeField] private Vector2 wallJumpForce = new Vector2(8f, 16.5f);
    [SerializeField] private float wallJumpDuration = 0.12f;
    [SerializeField] private float wallCatchDelay = 0.06f;

    [Header("Jump Assist")]
    [SerializeField] private float jumpBufferTime = 0.15f;

    [Header("Coyote Time Settings")]
    [SerializeField] private float coyoteTime = 0.15f;
    private float coyoteTimeCounter;

    [Header("Apex Modifiers Settings")]
    [SerializeField] private float apexThreshold = 3.0f;
    [SerializeField] private float apexGravityMultiplier = 0.5f;
    [SerializeField] private float apexBonusSpeedMultiplier = 1.15f;

    [Header("Ledge Corner Correction Settings")]
    [SerializeField] private float cornerCorrectionDistance = 0.2f;
    [SerializeField] private float cornerCorrectionNudge = 0.07f;
    [SerializeField] private Transform headCheckPosition;
    [SerializeField] private float headCheckDistance = 0.2f;

    private float currentApexBonus = 1f;
    private bool isAtApex;
    private float jumpBufferCounter;
    private bool isJumping;

    private Player player;
    private bool isWallSliding = false;
    private bool isWallJumping = false;
    private bool isWallFreezing = false;
    private float wallJumpCounter;

    private Coroutine wallCatchCoroutine;
    private WaitForSeconds wallCatchWait;

    // Calculated physics values (auto-computed from jumpHeight & timeToJumpApex)
    private float calculatedGravity;
    private float calculatedJumpVelocity;

    public bool isDashing { get; set; } = false;

    void Awake()
    {
        player = GetComponent<Player>();
        wallCatchWait = new WaitForSeconds(wallCatchDelay);

        if (headCheckPosition == null)
        {
            headCheckPosition = transform.Find("headCheck");
        }

        RecalculatePhysics();
    }

    private void OnValidate()
    {
        jumpHeight = Mathf.Max(0.5f, jumpHeight);
        timeToJumpApex = Mathf.Max(0.1f, timeToJumpApex);
        groundAccelTime = Mathf.Max(0.01f, groundAccelTime);
        turnAccelTime = Mathf.Max(0.01f, turnAccelTime);
        RecalculatePhysics();
    }

    /// <summary>
    /// Tự động tính trọng lực và vận tốc nhảy ban đầu theo công thức g = 2*H / t^2.
    /// </summary>
    private void RecalculatePhysics()
    {
        calculatedGravity = (2f * jumpHeight) / (timeToJumpApex * timeToJumpApex);
        calculatedJumpVelocity = calculatedGravity * timeToJumpApex;

        if (player == null) player = GetComponent<Player>();
        if (player != null && player.rb != null)
        {
            player.rb.gravityScale = calculatedGravity / Mathf.Abs(Physics2D.gravity.y);
        }
    }

    void Update()
    {
        if (isDashing) return;

        // 1. Quản lý Coyote Time & Jump Buffer Timers
        coyoteTimeCounter = player.IsGrounded() ? coyoteTime : coyoteTimeCounter - Time.deltaTime;

        if (player.jumpInput)
        {
            jumpBufferCounter = jumpBufferTime;
            player.UseJumpInput();
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // 2. Thả phím nhảy sớm (Variable Jump Height)
        if (player.jumpUpInput)
        {
            if (player.rb.linearVelocity.y > 0f && isJumping)
            {
                player.rb.linearVelocity = new Vector2(player.rb.linearVelocity.x, player.rb.linearVelocity.y * jumpCutMultiplier);
                isJumping = false;
            }
            player.UseJumpUpInput();
        }

        // 3. Đánh giá trạng thái Đỉnh cú nhảy (Apex Modifiers)
        isAtApex = !player.IsGrounded() && Mathf.Abs(player.rb.linearVelocity.y) < apexThreshold;
        currentApexBonus = isAtApex ? apexBonusSpeedMultiplier : 1f;
    }

    void FixedUpdate()
    {
        if (isDashing) return;

        CheckCornerCorrection();
        HandleWallSlideState();

        if (!isWallJumping)
        {
            MovePlayer();
        }

        ApplyCustomGravity();
        HandleWallSlidePhysics();
        HandleJump();
    }

    void MovePlayer()
    {
        float targetSpeed = player.horizontalInput * moveSpeed * currentApexBonus;
        float currentVelX = player.rb.linearVelocity.x;

        // Phân biệt tăng tốc thường (0.08s) vs phanh/đổi hướng gắt (0.05s)
        bool isTurningOrBraking = Mathf.Abs(targetSpeed) < 0.01f || (Mathf.Abs(currentVelX) > 0.1f && Mathf.Sign(targetSpeed) != Mathf.Sign(currentVelX));
        float accelTime = isTurningOrBraking ? turnAccelTime : groundAccelTime;
        float accelRate = moveSpeed / accelTime;

        // Giảm nhẹ gia tốc trên không (~80% mặt đất) để cú nhảy có trọng lượng thật hơn
        if (!player.IsGrounded())
        {
            accelRate *= airControlMultiplier;
        }

        float newVelX = Mathf.MoveTowards(currentVelX, targetSpeed, accelRate * Time.fixedDeltaTime);
        player.rb.linearVelocity = new Vector2(newVelX, player.rb.linearVelocity.y);
    }

    /// <summary>
    /// Áp dụng trọng lực linh hoạt: Rơi nặng hơn khi đi xuống, Lơ lửng hơn khi ở đỉnh Apex, và chặn tốc độ rơi tối đa.
    /// </summary>
    void ApplyCustomGravity()
    {
        if (isWallSliding) return;

        float velY = player.rb.linearVelocity.y;

        if (velY < 0f)
        {
            // Trạng thái đang rơi -> Rơi dứt khoát (1.6x - 1.8x)
            velY -= calculatedGravity * (fallMultiplier - 1f) * Time.fixedDeltaTime;
            isJumping = false;
        }
        else if (isAtApex)
        {
            // Trạng thái ở Đỉnh Apex -> Lơ lửng nhẹ (Hang Time)
            velY -= calculatedGravity * (apexGravityMultiplier - 1f) * Time.fixedDeltaTime;
        }

        // Chặn tốc độ rơi tối đa (Terminal Fall Speed)
        if (velY < -maxFallSpeed)
        {
            velY = -maxFallSpeed;
        }

        player.rb.linearVelocity = new Vector2(player.rb.linearVelocity.x, velY);
    }

    void HandleJump()
    {
        if (jumpBufferCounter > 0f)
        {
            if (coyoteTimeCounter > 0f)
            {
                player.rb.linearVelocity = new Vector2(player.rb.linearVelocity.x, calculatedJumpVelocity);
                jumpBufferCounter = 0f;
                coyoteTimeCounter = 0f;
                isJumping = true;
            }
            else if (isWallSliding || (player.IsTouchingWall() && !player.IsGrounded()))
            {
                isWallJumping = true;
                float wallJumpDirection = player.facingDirection * -1f;
                player.rb.linearVelocity = new Vector2(wallJumpDirection * wallJumpForce.x, wallJumpForce.y);
                wallJumpCounter = wallJumpDuration;

                // Tự động lật mặt nhân vật hướng ra ngoài tường để wallCheck hướng sẵn về phía tường đối diện
                player.Flip();

                jumpBufferCounter = 0f;
                coyoteTimeCounter = 0f;
                isWallSliding = false;
                isWallFreezing = false;
                StopWallCatchCoroutine();
                isJumping = true;
            }
        }

        if (isWallJumping)
        {
            wallJumpCounter -= Time.fixedDeltaTime;
            if (wallJumpCounter <= 0) isWallJumping = false;
        }
    }

    /// <summary>
    /// Tự động nhích nhẹ nhân vật sang bên khi bị vướng mép bục lúc nhảy lên.
    /// Tối ưu: Dùng rb.position thay cho transform.position để tránh giật hình vật lý.
    /// </summary>
    private void CheckCornerCorrection()
    {
        if (player.rb.linearVelocity.y <= 0f || headCheckPosition == null) return;

        Vector2 rayOrigin = headCheckPosition.position;

        bool leftHit = Physics2D.Raycast(rayOrigin - new Vector2(cornerCorrectionDistance, 0), Vector2.up, headCheckDistance, player.GetGroundLayer());
        bool rightHit = Physics2D.Raycast(rayOrigin + new Vector2(cornerCorrectionDistance, 0), Vector2.up, headCheckDistance, player.GetGroundLayer());

        if (leftHit && !rightHit)
        {
            player.rb.position += new Vector2(cornerCorrectionNudge, 0);
        }
        else if (rightHit && !leftHit)
        {
            player.rb.position -= new Vector2(cornerCorrectionNudge, 0);
        }
    }

    public void ForceWallJumpState(float counterDuration)
    {
        isWallJumping = true;
        wallJumpCounter = counterDuration;
    }

    void HandleWallSlideState()
    {
        if (isWallJumping)
        {
            isWallSliding = false;
            isWallFreezing = false;
            StopWallCatchCoroutine();
            return;
        }

        bool currentlyTouchingWall = player.IsTouchingWall() && !player.IsGrounded();

        if (currentlyTouchingWall)
        {
            // Rút gọn phép kiểm tra bấm phím hướng vào tường bằng toán học
            bool pressingIntoWall = Mathf.Abs(player.horizontalInput) > 0.1f && Mathf.Sign(player.horizontalInput) == player.facingDirection;

            if (player.rb.linearVelocity.y > 0.1f || !pressingIntoWall)
            {
                isWallSliding = false;
                isWallFreezing = false;
                StopWallCatchCoroutine();
            }
            else
            {
                isWallSliding = true;
                if (!isWallFreezing && player.rb.linearVelocity.y <= 0.1f && wallCatchCoroutine == null)
                {
                    wallCatchCoroutine = StartCoroutine(WallCatchRoutine());
                }
            }
        }
        else
        {
            isWallSliding = false;
            isWallFreezing = false;
            StopWallCatchCoroutine();
        }
    }

    IEnumerator WallCatchRoutine()
    {
        isWallFreezing = true;
        isWallSliding = true;
        yield return wallCatchWait;
        isWallFreezing = false;
    }

    void StopWallCatchCoroutine()
    {
        if (wallCatchCoroutine != null)
        {
            StopCoroutine(wallCatchCoroutine);
            wallCatchCoroutine = null;
        }
    }

    void HandleWallSlidePhysics()
    {
        if (isWallSliding)
        {
            if (isWallFreezing)
            {
                player.rb.linearVelocity = new Vector2(player.rb.linearVelocity.x, 0f);
            }
            else
            {
                player.rb.linearVelocity = new Vector2(player.rb.linearVelocity.x, -wallSlideSpeed);
            }
        }
    }

    public void ApplyKnockbackStun(float duration)
    {
        isWallJumping = true;
        wallJumpCounter = duration;
    }

    private void OnDrawGizmos()
    {
        if (headCheckPosition != null)
        {
            Gizmos.color = Color.green;
            Vector3 origin = headCheckPosition.position;
            Gizmos.DrawLine(origin - new Vector3(cornerCorrectionDistance, 0, 0), origin - new Vector3(cornerCorrectionDistance, 0, 0) + Vector3.up * headCheckDistance);
            Gizmos.DrawLine(origin + new Vector3(cornerCorrectionDistance, 0, 0), origin + new Vector3(cornerCorrectionDistance, 0, 0) + Vector3.up * headCheckDistance);
        }
    }
}