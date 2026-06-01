using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private NPCInteraction currentNPC;

    [Header("状态")]
    public bool isHealing = false;
    public bool isEating = false;
    public bool isDead = false;

    [Header("基本移动设置")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("地面检测点")]
    public Transform groundCheck;

    [Header("陷阱设置")]
    public float sinkSpeed = 0.5f;
    public float trapSlowFactor = 0.5f;
    private bool isInTrap = false;

    [Header("水中设置")]
    public float waterSinkSpeed = 0.4f;
    public float waterSlowFactor = 0.4f;
    public float waterJumpForce = 6f;
    public bool IsInWater { get; private set; }

    private Rigidbody2D rb;
    private Animator animator;
    private bool isFacingRight = true;
    private bool isGrounded;

    [Header("战斗状态")]
    public bool isAttacking = false;

    [Header("手持系统")]
    public HandItemController handItemController;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        handItemController = GetComponentInChildren<HandItemController>();
    }

    void Start()
    {
        EnsureGroundCheckExists();

        // 确保初始缩放正确（面向右）
        Vector3 localScale = transform.localScale;
        if (localScale.x < 0)
        {
            localScale.x = -localScale.x;
            transform.localScale = localScale;
        }
        // 确保先初始化朝向
        isFacingRight = true;

        // 恢复手持物品（不再需要更新位置）
        if (Inv.Instance != null && handItemController != null)
        {
            Item equippedItem = Inv.Instance.GetEquippedItem();
            if (equippedItem != null)
            {
                handItemController.EquipItem(equippedItem);
            }
        }
    }

    void EnsureGroundCheckExists()
    {
        if (groundCheck == null)
        {
            groundCheck = transform.Find("GroundCheck");
            if (groundCheck == null)
            {
                GameObject groundCheckObj = new GameObject("GroundCheck");
                groundCheck = groundCheckObj.transform;
                groundCheck.SetParent(transform);
                groundCheck.localPosition = new Vector3(0, -0.5f, 0);
            }
        }
    }

    public void SetTrapState(bool state)
    {
        isInTrap = state;
    }

    public void SetWaterState(bool state)
    {
        IsInWater = state;
        Debug.Log($"Water state set to: {state}");
    }

    void FixedUpdate()
    {
        // 添加水中下沉效果
        if (IsInWater)
        {
            rb.velocity = new Vector2(rb.velocity.x, -waterSinkSpeed);
        }
        else if (isInTrap)
        {
            rb.velocity = new Vector2(rb.velocity.x, -sinkSpeed);
        }
    }

    void Update()
    {
        if (isDead || isHealing || isEating) return;

        if (groundCheck == null)
        {
            EnsureGroundCheckExists();
            return;
        }

        if (isAttacking || isHealing)
        {
            if (rb != null) rb.velocity = new Vector2(0, rb.velocity.y);
            return;
        }

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (animator != null)
        {
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetFloat("Speed", Mathf.Abs(Input.GetAxisRaw("Horizontal")));
        }

        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float moveMultiplier = 1f;

        if (IsInWater) moveMultiplier = waterSlowFactor;
        else if (isInTrap) moveMultiplier = trapSlowFactor;

        Vector2 movement = new Vector2(horizontalInput * moveMultiplier, 0);

        if (horizontalInput > 0 && !isFacingRight)
        {
            FlipCharacter();
        }
        else if (horizontalInput < 0 && isFacingRight)
        {
            FlipCharacter();
        }

        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            float effectiveJumpForce = jumpForce;
            if (IsInWater) effectiveJumpForce = waterJumpForce;

            rb.velocity = new Vector2(rb.velocity.x, effectiveJumpForce);
            if (animator != null) animator.SetTrigger("Jump");
        }

        if (rb != null)
        {
            rb.velocity = new Vector2(movement.x * moveSpeed, rb.velocity.y);
        }
        if (Input.GetKeyDown(KeyCode.E) && currentNPC != null)
        {
            currentNPC.OpenDialogue();
        }

    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("NPC"))
        {
            Debug.Log($"进入触发区域:");
            currentNPC = other.GetComponent<NPCInteraction>();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("NPC"))
        {
            currentNPC = null;
        }
    }
    private void FlipCharacter()
    {
        isFacingRight = !isFacingRight;
        Vector3 newScale = transform.localScale;
        newScale.x *= -1;
        transform.localScale = newScale;

        // 已移除手持物品位置更新
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        if (isInTrap)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawCube(transform.position + Vector3.up * 1.2f, new Vector3(0.6f, 0.1f, 0));
        }

        if (IsInWater)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawCube(transform.position + Vector3.down * 1.2f, new Vector3(0.6f, 0.1f, 0));
        }
    }
}