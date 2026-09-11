using UnityEngine;

public class PlayerController : MonoBehaviour
{
    const int AnimIdle = 0;
    const int AnimRunning = 1;
    const int AnimDeath = 2;
    const int AnimJump = 3;

    static readonly int MoveHash = Animator.StringToHash("move");

    public float moveSpeed = 6f;
    public float jumpForce = 7f;
    public float rotateSpeed = 12f;

    public CameraFollow cameraFollow;

    public LayerMask obstacleLayer;

    [Header("Knockback")]
    public float knockbackDistance = 0.4f;
    public float knockbackDuration = 0.15f;

    private Rigidbody rb;
    private Animator animator;
    private Collider[] playerColliders;

    public bool isGrounded;
    public bool isDead;

    private bool isKnockingBack;

    private Vector3 knockbackStart;
    private Vector3 knockbackTarget;
    private float knockbackTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("PlayerController: Rigidbody is missing on Player!");
            return;
        }

        rb.freezeRotation = true;

        animator = GetComponent<Animator>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        playerColliders = GetComponentsInChildren<Collider>();

        if (cameraFollow == null)
            cameraFollow = FindFirstObjectByType<CameraFollow>();

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        SetMoveAnim(AnimIdle);
    }

    void Update()
    {
        if (isDead)
            return;

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            isGrounded = false;

            SetMoveAnim(AnimJump);
        }
    }

    void FixedUpdate()
    {
        if (rb == null)
            return;

        if (isKnockingBack)
        {
            HandleKnockback();
            return;
        }

        if (isDead)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            return;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Quaternion camYaw = cameraFollow != null
            ? Quaternion.Euler(0f, cameraFollow.Yaw, 0f)
            : Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        Vector3 move =
            (camYaw * Vector3.forward * v +
             camYaw * Vector3.right * h).normalized;

        bool isMoving = move.sqrMagnitude > 0.01f;

        if (isMoving)
        {
            Quaternion targetRot =
                Quaternion.LookRotation(move, Vector3.up);

            rb.MoveRotation(
                Quaternion.Slerp(
                    rb.rotation,
                    targetRot,
                    rotateSpeed * Time.fixedDeltaTime
                )
            );
        }

        rb.linearVelocity = new Vector3(
            move.x * moveSpeed,
            rb.linearVelocity.y,
            move.z * moveSpeed
        );

        if (isGrounded)
        {
            SetMoveAnim(
                isMoving
                    ? AnimRunning
                    : AnimIdle
            );
        }
    }

    void HandleKnockback()
    {
        knockbackTimer += Time.fixedDeltaTime;

        float t = Mathf.Clamp01(
            knockbackTimer / knockbackDuration
        );

        Vector3 newPosition = Vector3.Lerp(
            knockbackStart,
            knockbackTarget,
            t
        );

        rb.MovePosition(newPosition);

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (t >= 1f)
        {
            isKnockingBack = false;

            rb.isKinematic = true;

            if (animator != null)
            {
                animator.applyRootMotion = false;
                SetMoveAnim(AnimDeath);
            }

            if (playerColliders != null)
            {
                foreach (Collider col in playerColliders)
                {
                    col.enabled = false;
                }
            }

            Debug.Log("PLAYER DIED!");
        }
    }

    public void Die()
    {
        if (isDead || isKnockingBack)
            return;

        isDead = true;
        isGrounded = false;

        Vector3 knockbackDirection = -transform.forward;

        knockbackDirection.y = 0f;

        if (knockbackDirection.sqrMagnitude < 0.01f)
        {
            knockbackDirection = -transform.right;
            knockbackDirection.y = 0f;
        }

        knockbackDirection.Normalize();

        knockbackStart = transform.position;

        knockbackTarget =
            transform.position +
            knockbackDirection * knockbackDistance;

        knockbackTimer = 0f;
        isKnockingBack = true;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Debug.Log("PLAYER HIT - KNOCKBACK!");
    }

    void SetMoveAnim(int state)
    {
        if (animator != null)
        {
            animator.SetInteger(MoveHash, state);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (IsObstacle(collision.gameObject))
        {
            Die();
            return;
        }

        CheckGround(collision);
    }

    void OnCollisionStay(Collision collision)
    {
        if (IsObstacle(collision.gameObject))
        {
            Die();
            return;
        }

        CheckGround(collision);
    }

    void OnCollisionExit(Collision collision)
    {
        if (!IsObstacle(collision.gameObject))
        {
            isGrounded = false;
        }
    }

    void CheckGround(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                isGrounded = true;
                return;
            }
        }
    }

    bool IsObstacle(GameObject obj)
    {
        return (obstacleLayer.value & (1 << obj.layer)) != 0;
    }
}
