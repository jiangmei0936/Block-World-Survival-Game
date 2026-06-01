using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Animator), typeof(Rigidbody2D))]
public class ZombieController : MonoBehaviour
{
    [Header("敌人设置")]
    public int maxHealth = 1;
    public int damageToPlayer = 1;
    public float moveSpeed = 2f;
    public float chaseSpeed = 3.5f;
    public float groundCheckDistance = 0.5f;
    public float detectionRange = 5f;
    public LayerMask groundLayer;

    [Header("巡逻设置")]
    public float patrolRange = 5f;
    private float patrolStartX;
    private float patrolDirection = 1f;
    public float positionTolerance = 0.5f;

    [Header("转向设置")]
    public float flipCooldown = 0.1f;
    public float edgeCheckOffset = 0.7f;
    public float obstacleCheckDistance = 1.0f;

    [Header("攻击设置")]
    public float attackRange = 1.2f;
    public float attackCooldown = 2f;
    public float attackWindupTime = 0.3f;

    [Header("视觉效果")]
    public ParticleSystem deathParticles;  // 死亡粒子系统
    public float deathParticleDuration = 1.5f;
    public GameObject attackEffectPrefab;
    public float effectOffset = 0.5f;
    public Color attackColor = Color.red;
    public float colorFlashDuration = 0.2f;

    [Header("组件")]
    public LayerMask playerLayer;

    // 内部状态
    private int currentHealth;
    private bool isDead = false;
    private bool facingRight = false;
    private Rigidbody2D rb;
    private Transform player;
    private float lastAttackTime;
    private bool isAttacking;
    private bool isChasing;
    private bool isPatrolling = true;
    private float lastFlipTime;
    private List<SpriteRenderer> childRenderers = new List<SpriteRenderer>();
    private Animator animator;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        patrolStartX = transform.position.x;
        animator = GetComponent<Animator>();

        // 获取所有子物体的SpriteRenderer
        childRenderers = new List<SpriteRenderer>(GetComponentsInChildren<SpriteRenderer>());
        // 移除父物体自身的SpriteRenderer（如果有）
        SpriteRenderer selfRenderer = GetComponent<SpriteRenderer>();
        if (selfRenderer != null) childRenderers.Remove(selfRenderer);

        // 初始随机方向
        facingRight = Random.value > 0.5f;
        patrolDirection = facingRight ? 1f : -1f;
        UpdateFacing();
        lastFlipTime = Time.time;
    }

    void Update()
    {
        if (isDead) return;

        // 玩家检测
        Collider2D playerCollider = Physics2D.OverlapCircle(
            transform.position,
            detectionRange,
            playerLayer
        );

        isChasing = (playerCollider != null);
        isPatrolling = !isChasing && !isAttacking;

        if (isChasing && !isAttacking)
        {
            float distance = Vector2.Distance(transform.position, player.position);
            ChasePlayer(distance);
        }
        else if (isPatrolling)
        {
            Patrol();
        }

        // 更新动画状态
        UpdateAnimations();
    }

    void FixedUpdate()
    {
        if (isDead || isAttacking) return;

        // 地面和边缘检测
        Vector2 rayOrigin = transform.position;
        if (childRenderers.Count > 0)
        {
            // 使用第一个子物体的边界作为参考
            rayOrigin.y -= childRenderers[0].bounds.extents.y;
        }

        // 1. 地面存在检测
        RaycastHit2D groundHit = Physics2D.Raycast(
            rayOrigin,
            Vector2.down,
            groundCheckDistance * 1.5f,
            groundLayer
        );

        bool isGrounded = groundHit.collider != null;

        bool shouldFlip = false;

        if (isGrounded && isPatrolling)
        {
            // 2. 前方边缘检测
            Vector2 edgeCheckPos = rayOrigin;
            edgeCheckPos.x += (facingRight ? edgeCheckOffset : -edgeCheckOffset);

            RaycastHit2D edgeHit = Physics2D.Raycast(
                edgeCheckPos,
                Vector2.down,
                groundCheckDistance * 3f,
                groundLayer
            );

            // 3. 障碍物检测
            Vector2 obstacleCheckPos = transform.position;
            obstacleCheckPos.y -= 0.5f;

            RaycastHit2D obstacleHit = Physics2D.Raycast(
                obstacleCheckPos,
                facingRight ? Vector2.right : Vector2.left,
                obstacleCheckDistance,
                groundLayer
            );

            // 转向条件：前方无地面或有障碍物
            shouldFlip = (edgeHit.collider == null || obstacleHit.collider != null);
        }

        // 转向条件 - 添加冷却时间检查
        if (shouldFlip && Time.time > lastFlipTime + flipCooldown)
        {
            Flip();
            patrolDirection *= -1;
            lastFlipTime = Time.time;
        }
        else if (shouldFlip)
        {
            // 新增：即使需要转向但冷却中，也保持50%速度移动
            rb.velocity = new Vector2(patrolDirection * moveSpeed * 0.5f, rb.velocity.y);
        }
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        // 只设置必要的动画参数
        bool isMoving = (isPatrolling || isChasing) && !isAttacking;
        animator.SetBool("isMoving", isMoving);

        if (isAttacking)
        {
            animator.SetTrigger("Attack");
        }
    }

    void Patrol()
    {
        // 检查是否超出巡逻范围（添加容差）
        float currentOffset = transform.position.x - patrolStartX;
        if (Mathf.Abs(currentOffset) >= patrolRange - positionTolerance)
        {
            patrolDirection = (currentOffset > 0) ? -1f : 1f;
            facingRight = (patrolDirection > 0);
            UpdateFacing();
        }

        // 应用移动速度
        rb.velocity = new Vector2(patrolDirection * moveSpeed, rb.velocity.y);
    }

    void ChasePlayer(float distance)
    {
        // 计算朝向玩家的方向
        Vector2 direction = (player.position - transform.position).normalized;
        rb.velocity = new Vector2(direction.x * chaseSpeed, rb.velocity.y);

        // 更新朝向
        if (direction.x > 0 && !facingRight)
        {
            facingRight = true;
            UpdateFacing();
            patrolDirection = 1f;
        }
        else if (direction.x < 0 && facingRight)
        {
            facingRight = false;
            UpdateFacing();
            patrolDirection = -1f;
        }

        // 检查是否在攻击范围内
        if (distance <= attackRange && CanAttack())
        {
            StartCoroutine(AttackPlayer());
        }
    }

    bool CanAttack()
    {
        return Time.time >= lastAttackTime + attackCooldown;
    }

    IEnumerator AttackPlayer()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        // 停止移动
        Vector2 originalVelocity = rb.velocity;
        rb.velocity = Vector2.zero;

        // 播放攻击动画
        animator.SetTrigger("Attack");

        // 等待攻击前摇
        yield return new WaitForSeconds(attackWindupTime);

        // 实际攻击判定（现在由动画事件调用OnAttackHit）

        // 等待攻击动画完成
        yield return new WaitForSeconds(0.3f);

        // 恢复移动
        rb.velocity = originalVelocity;
        isAttacking = false;
    }

    // 动画事件调用的攻击判定
    public void OnAttackHit()
    {
        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(
            transform.position + (facingRight ? Vector3.right * attackRange / 2 : Vector3.left * attackRange / 2),
            attackRange / 2,
            playerLayer
        );

        foreach (Collider2D playerCollider in hitPlayers)
        {
            HealthSystem playerHealth = playerCollider.GetComponent<HealthSystem>();
            if (playerHealth != null && playerHealth.CanTakeDamage())
            {
                playerHealth.TakeDamage(damageToPlayer);

                // 显示伤害文本
                FloatingTextManager.Instance?.ShowText(
                    damageToPlayer.ToString(),
                    playerCollider.transform.position + Vector3.up * 0.5f,
                    Color.red
                );
            }
        }

        // 显示攻击效果
        ShowAttackEffect();

        // 颜色闪烁效果
        StartCoroutine(FlashColor(attackColor));
    }

    IEnumerator FlashColor(Color flashColor)
    {
        if (childRenderers.Count == 0) yield break;

        List<Color> originalColors = new List<Color>();
        foreach (SpriteRenderer renderer in childRenderers)
        {
            originalColors.Add(renderer.color);
            renderer.color = flashColor;
        }

        yield return new WaitForSeconds(colorFlashDuration);

        for (int i = 0; i < childRenderers.Count; i++)
        {
            if (childRenderers[i] != null)
                childRenderers[i].color = originalColors[i];
        }
    }

    void ShowAttackEffect()
    {
        if (attackEffectPrefab != null && childRenderers.Count > 0)
        {
            Vector3 spawnPos = transform.position;
            spawnPos.x += facingRight ? effectOffset : -effectOffset;
            spawnPos.y += 0.5f;

            GameObject effect = Instantiate(attackEffectPrefab, spawnPos, Quaternion.identity);
            Destroy(effect, 0.5f);
        }
    }

    // 敌人受到伤害
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"僵尸受到 {damage} 点伤害，剩余生命: {currentHealth}");

        // 受击动画
        animator.SetTrigger("Hit");

        // 受击变色效果
        StartCoroutine(FlashRed());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    IEnumerator FlashRed()
    {
        if (childRenderers.Count == 0) yield break;

        List<Color> originalColors = new List<Color>();
        foreach (SpriteRenderer renderer in childRenderers)
        {
            originalColors.Add(renderer.color);
            renderer.color = Color.red;
        }

        yield return new WaitForSeconds(0.1f);

        for (int i = 0; i < childRenderers.Count; i++)
        {
            if (childRenderers[i] != null)
                childRenderers[i].color = originalColors[i];
        }
    }

    // 敌人死亡
    void Die()
    {
        isDead = true;
        Debug.Log("僵尸死亡!");

        // 播放死亡动画
        //animator.SetTrigger("Die");

        // 播放死亡粒子特效
        PlayDeathParticles();

        // 添加死亡效果
        foreach (SpriteRenderer renderer in childRenderers)
        {
            if (renderer != null)
                renderer.color = Color.gray;
        }

        rb.velocity = Vector2.zero;

        // 禁用碰撞体
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;

        // 延迟销毁
        StartCoroutine(DelayedDestroy());
    }

    // 播放死亡粒子特效 - 修正后版本
    void PlayDeathParticles()
    {
        if (deathParticles != null)
        {
            ParticleSystem particles = Instantiate(deathParticles, transform.position, Quaternion.identity);

            var mainModule = particles.main;
            if (childRenderers.Count > 0 && childRenderers[0] != null)
                mainModule.startColor = childRenderers[0].color;

            particles.Play();
            Destroy(particles.gameObject, deathParticleDuration);
        }
        else
        {
            Debug.LogWarning("未设置死亡粒子效果!", gameObject);
        }
    }

    IEnumerator DelayedDestroy()
    {
        // 等待死亡动画播放完毕
        yield return new WaitForSeconds(1.0f);

        // 禁用所有精灵渲染器
        foreach (SpriteRenderer renderer in childRenderers)
        {
            if (renderer != null)
                renderer.enabled = false;
        }

        // 等待粒子效果完成
        yield return new WaitForSeconds(deathParticleDuration - 1.0f);
        Destroy(gameObject);
    }

    // 当玩家碰到敌人时造成伤害
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead || isAttacking) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            HealthSystem playerHealth = collision.gameObject.GetComponent<HealthSystem>();
            if (playerHealth != null && playerHealth.CanTakeDamage())
            {
                playerHealth.TakeDamage(damageToPlayer);

                // 显示伤害文本
                FloatingTextManager.Instance?.ShowText(
                    damageToPlayer.ToString(),
                    collision.transform.position + Vector3.up * 0.5f,
                    Color.red
                );
            }
        }
    }

    private void Flip()
    {
        facingRight = !facingRight;
        UpdateFacing();
    }

    private void UpdateFacing()
    {
        // 仅通过翻转Sprite来处理朝向
        foreach (SpriteRenderer renderer in childRenderers)
        {
            renderer.flipX = !facingRight;
        }
    }

    // 可视化调试
    private void OnDrawGizmos()
    {
        // 地面检测线
        Gizmos.color = Color.green;
        Vector2 rayOrigin = transform.position;
        if (childRenderers.Count > 0)
        {
            rayOrigin.y -= childRenderers[0].bounds.extents.y;
        }
        Gizmos.DrawLine(rayOrigin, rayOrigin + Vector2.down * groundCheckDistance);

        // 前方障碍检测线
        Gizmos.color = Color.blue;
        Vector2 frontCheckPos = new Vector2(
            transform.position.x + (facingRight ? edgeCheckOffset : -edgeCheckOffset),
            transform.position.y - 0.5f
        );
        Vector2 endPoint = frontCheckPos + new Vector2(
            (facingRight ? obstacleCheckDistance : -obstacleCheckDistance),
            0
        );
        Gizmos.DrawLine(frontCheckPos, endPoint);

        // 检测范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // 攻击范围
        Gizmos.color = Color.red;
        Vector3 attackCenter = transform.position +
            (facingRight ? Vector3.right * attackRange / 2 : Vector3.left * attackRange / 2);
        Gizmos.DrawWireSphere(attackCenter, attackRange / 2);

        // 巡逻范围
        if (Application.isPlaying)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(
                new Vector3(patrolStartX - patrolRange, transform.position.y - 1f, 0),
                new Vector3(patrolStartX + patrolRange, transform.position.y - 1f, 0)
            );
            Gizmos.DrawWireSphere(
                new Vector3(patrolStartX - patrolRange, transform.position.y - 1f, 0),
                0.2f
            );
            Gizmos.DrawWireSphere(
                new Vector3(patrolStartX + patrolRange, transform.position.y - 1f, 0),
                0.2f
            );
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 边缘检测可视化
        if (!Application.isPlaying) return;

        Vector2 rayOrigin = transform.position;
        if (childRenderers.Count > 0)
        {
            rayOrigin.y -= childRenderers[0].bounds.extents.y;
        }

        Gizmos.color = Color.cyan;
        Vector2 edgeCheckPos = rayOrigin;
        edgeCheckPos.x += (facingRight ? edgeCheckOffset : -edgeCheckOffset);
        Gizmos.DrawLine(edgeCheckPos, edgeCheckPos + Vector2.down * groundCheckDistance * 3f);
    }
}