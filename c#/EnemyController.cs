using UnityEngine;
using System.Collections;

public class EnemyController : MonoBehaviour
{
    [Header("敌人设置")]
    public int maxHealth = 3; // 改为3点生命值（打三下死亡）
    public int damageToPlayer = 1; // 对玩家造成的伤害
    public float moveSpeed = 2f; // 移动速度
    public float groundCheckDistance = 0.5f; // 地面检测距离
    public LayerMask groundLayer; // 地面图层

    [Header("死亡效果")]
    public ParticleSystem deathParticles; // 死亡粒子特效
    public float deathParticleDuration = 1.5f; // 粒子效果持续时间

    private int currentHealth;
    private bool isDead = false;
    private bool facingRight = false;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;

    void Start()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        // 初始随机方向
        facingRight = Random.value > 0.5f;
        UpdateFacing();
    }

    void Update()
    {
        if (isDead) return;

        // 地面检测
        bool isGrounded = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayer);

        if (isGrounded)
        {
            // 地面行走
            float moveDirection = facingRight ? 1 : -1;
            rb.velocity = new Vector2(moveDirection * moveSpeed, rb.velocity.y);
        }
        else
        {
            // 走到边缘时转向
            Flip();
        }

        // 前方障碍检测
        Vector2 frontCheckPos = new Vector2(
            transform.position.x + (facingRight ? 0.5f : -0.5f),
            transform.position.y
        );

        RaycastHit2D hit = Physics2D.Raycast(
            frontCheckPos,
            facingRight ? Vector2.right : Vector2.left,
            0.2f,
            groundLayer
        );

        if (hit.collider != null)
        {
            Flip();
        }
    }

    // 敌人受到伤害
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"敌人受到 {damage} 点伤害，剩余生命: {currentHealth}");

        // 受击变色效果
        StartCoroutine(FlashRed());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    IEnumerator FlashRed()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = Color.white;
    }

    // 敌人死亡
    void Die()
    {
        isDead = true;
        Debug.Log("敌人死亡!");

        // 播放死亡粒子特效
        PlayDeathParticles();

        // 添加死亡效果
        spriteRenderer.color = Color.gray;
        rb.velocity = Vector2.zero;

        // 禁用碰撞体和渲染器
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;

        spriteRenderer.enabled = false;

        // 延迟销毁
        Destroy(gameObject, deathParticleDuration);
    }

    // 播放死亡粒子特效
    void PlayDeathParticles()
    {
        if (deathParticles != null)
        {
            // 创建粒子实例（确保粒子在敌人销毁后继续存在）
            ParticleSystem particles = Instantiate(deathParticles, transform.position, Quaternion.identity);

            // 设置粒子颜色为敌人当前颜色（可选）
            var mainModule = particles.main;
            mainModule.startColor = spriteRenderer.color;

            // 播放粒子
            particles.Play();

            // 粒子播放完毕后销毁
            Destroy(particles.gameObject, deathParticleDuration);
        }
    }

    // 当玩家碰到敌人时造成伤害
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            HealthSystem playerHealth = collision.gameObject.GetComponent<HealthSystem>();
            if (playerHealth != null && playerHealth.CanTakeDamage())
            {
                playerHealth.TakeDamage(damageToPlayer);
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
        transform.localScale = new Vector3(facingRight ? -1 : 1, 1, 1);
    }

    // 可视化调试
    private void OnDrawGizmos()
    {
        // 地面检测线
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);

        // 前方障碍检测线
        Gizmos.color = Color.blue;
        Vector2 frontCheckPos = new Vector2(
            transform.position.x + (facingRight ? 0.5f : -0.5f),
            transform.position.y
        );
        Vector2 endPoint = frontCheckPos + new Vector2(
            (facingRight ? 0.2f : -0.2f),
            0
        );
        Gizmos.DrawLine(frontCheckPos, endPoint);
    }
}
