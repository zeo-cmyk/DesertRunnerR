using System.Collections;
using UnityEngine;

public class PlayerRunner : MonoBehaviour
{
    const int AnimIdle = 0;
    const int AnimRunning = 1;
    const int AnimDeath = 2;
    const int AnimJump = 3;

    static readonly int MoveHash = Animator.StringToHash("move");

    [Header("Run")]
    public float runSpeed = 10f;
    public float speedGainPerSecond = 0.25f;
    public float maxRunSpeed = 18f;

    [Header("Lanes")]
    [Tooltip("0 = left, 1 = center, 2 = right")]
    public int startLane = 1;
    public float laneWidth = 2.8f;
    public float laneChangeSpeed = 14f;

    [Header("Audio Settings")]
    public AudioClip laneChangeSound;
    private AudioSource audioSource;

    [Header("Jump")]
    public float jumpForce = 7.5f;

    [Header("Hit")]
    public LayerMask obstacleLayers;
    public string obstacleTag = "Obstacle";

    [Tooltip("How far the player moves backward when hitting an obstacle.")]
    public float hitBackwardDistance = 1.0f;

    [Tooltip("How fast the player moves backward when hit.")]
    public float hitBackwardSpeed = 8f;

    Rigidbody rb;
    Animator animator;

    int currentLane;
    bool isGrounded = true;
    bool isDead;
    bool isRunning;

    float groundCheckUnlockTime;
    float swipeStartX;
    float swipeStartY;
    bool swipeArmed;

    public bool IsDead => isDead;
    public bool IsRunning => isRunning;
    public int CurrentLane => currentLane;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("PlayerRunner requires a Rigidbody component.");
            return;
        }

        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        animator = GetComponent<Animator>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;

        if (obstacleLayers.value == 0)
        {
            int obstacleLayer = LayerMask.NameToLayer("Obstacle");

            if (obstacleLayer >= 0)
                obstacleLayers = 1 << obstacleLayer;
        }

        currentLane = Mathf.Clamp(startLane, 0, 2);

        // Adjust run speeds based on the difficulty multiplier selected in MainMenu
        float speedMultiplier = PlayerPrefs.GetFloat("GameSpeedMultiplier", 1.0f);
        runSpeed *= speedMultiplier;
        maxRunSpeed *= speedMultiplier;

        SetMoveAnim(AnimIdle);
    }

    public void BeginRun()
    {
        if (isDead)
            return;

        isRunning = true;
        SetMoveAnim(AnimRunning);
    }

    public void RestartGame()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RestartGame();
    }

    void Update()
    {
        if (isDead || !isRunning)
            return;

        runSpeed = Mathf.Min(
            maxRunSpeed,
            runSpeed + speedGainPerSecond * Time.deltaTime
        );

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            ChangeLane(-1);
        }

        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            ChangeLane(1);
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            TryJump();
        }

        ReadSwipe();
    }

    void ReadSwipe()
    {
        if (Input.GetMouseButtonDown(0))
        {
            swipeArmed = true;

            swipeStartX = Input.mousePosition.x;
            swipeStartY = Input.mousePosition.y;
        }

        if (!swipeArmed || !Input.GetMouseButtonUp(0))
            return;

        swipeArmed = false;

        Vector2 delta = new Vector2(
            Input.mousePosition.x - swipeStartX,
            Input.mousePosition.y - swipeStartY
        );

        if (delta.magnitude < 80f)
            return;

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            ChangeLane(delta.x > 0f ? 1 : -1);
        }
        else if (delta.y > 0f)
        {
            TryJump();
        }
    }

    void ChangeLane(int dir)
    {
        int nextLane = Mathf.Clamp(currentLane + dir, 0, 2);

        if (nextLane != currentLane)
        {
            currentLane = nextLane;

            if (audioSource != null && laneChangeSound != null)
            {
                audioSource.PlayOneShot(laneChangeSound);
            }
        }
    }

    void TryJump()
    {
        if (!isGrounded || isDead)
            return;

        rb.linearVelocity = new Vector3(
            rb.linearVelocity.x,
            0f,
            rb.linearVelocity.z
        );

        rb.AddForce(
            Vector3.up * jumpForce,
            ForceMode.Impulse
        );

        isGrounded = false;

        groundCheckUnlockTime = Time.time + 0.12f;

        SetMoveAnim(AnimJump);
    }

    void FixedUpdate()
    {
        if (rb == null)
            return;

        if (isDead)
        {
            rb.linearVelocity = new Vector3(
                0f,
                rb.linearVelocity.y,
                0f
            );

            return;
        }

        float targetX = (currentLane - 1) * laneWidth;

        float newX = Mathf.MoveTowards(
            rb.position.x,
            targetX,
            laneChangeSpeed * Time.fixedDeltaTime
        );

        float zSpeed = isRunning ? runSpeed : 0f;

        rb.linearVelocity = new Vector3(
            0f,
            rb.linearVelocity.y,
            zSpeed
        );

        rb.MovePosition(
            new Vector3(
                newX,
                rb.position.y,
                rb.position.z
            )
        );

        rb.MoveRotation(Quaternion.identity);

        if (!isRunning)
        {
            SetMoveAnim(AnimIdle);
            return;
        }

        if (!isGrounded)
        {
            SetMoveAnim(AnimJump);
        }
        else
        {
            SetMoveAnim(AnimRunning);
        }
    }

    public void Die()
    {
        if (isDead)
            return;

        isDead = true;
        isRunning = false;
        isGrounded = true;

        rb.linearVelocity = Vector3.zero;

        SetMoveAnim(AnimDeath);

        StartCoroutine(MoveBackwardOnDeath());
    }

    IEnumerator MoveBackwardOnDeath()
    {
        Vector3 startPosition = rb.position;

        Vector3 targetPosition =
            startPosition - Vector3.forward * hitBackwardDistance;

        float duration =
            hitBackwardDistance / Mathf.Max(hitBackwardSpeed, 0.01f);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.fixedDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            t = Mathf.SmoothStep(0f, 1f, t);

            rb.MovePosition(
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    t
                )
            );

            yield return new WaitForFixedUpdate();
        }

        rb.MovePosition(targetPosition);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
    }

    bool IsObstacle(GameObject other)
    {
        if (other == null)
            return false;

        if (obstacleLayers.value != 0 && ((1 << other.layer) & obstacleLayers) != 0)
        {
            return true;
        }

        return !string.IsNullOrEmpty(obstacleTag) && other.CompareTag(obstacleTag);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (IsObstacle(collision.gameObject))
        {
            Die();
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (isDead || Time.time < groundCheckUnlockTime)
        {
            return;
        }

        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                isGrounded = true;
                break;
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (isDead)
            return;

        isGrounded = false;
    }

    void SetMoveAnim(int state)
    {
        if (animator != null)
        {
            animator.SetInteger(MoveHash, state);
        }
    }
}