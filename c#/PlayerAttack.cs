using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("攻击设置")]
    public float attackRange = 1.5f; // 攻击范围
    public int attackDamage = 1; // 每次攻击伤害
    public float attackCooldown = 0.5f; // 攻击冷却时间
    public LayerMask enemyLayer; // 敌人图层
    public Transform attackPoint; // 攻击点（放置在玩家前方）

    private Animator animator;
    private float lastAttackTime;
    private PlayerMovement playerMovement;

    void Start()
    {
        animator = GetComponent<Animator>();
        playerMovement = GetComponent<PlayerMovement>();

        // 如果没有指定攻击点，使用默认位置
        if (attackPoint == null)
        {
            attackPoint = new GameObject("AttackPoint").transform;
            attackPoint.SetParent(transform);
            attackPoint.localPosition = new Vector3(0.8f, 0, 0);
        }
    }

    void Update()
    {
        // 检测攻击输入
        if (Input.GetKeyDown(KeyCode.V) && Time.time > lastAttackTime + attackCooldown)
        {
            Attack();
        }
    }

    void Attack()
    {
        lastAttackTime = Time.time;

        // 设置攻击状态
        if (playerMovement != null)
        {
            playerMovement.isAttacking = true;
        }

        // 播放攻击动画
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // 检测攻击范围内的敌人
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);

        foreach (Collider2D enemy in hitEnemies)
        {
            EnemyController enemyController = enemy.GetComponent<EnemyController>();
            ZombieController zombie = enemy.GetComponent<ZombieController>();
            if (enemyController != null)
            {
                enemyController.TakeDamage(attackDamage);
            }
            else if (zombie != null)
            {
                zombie.TakeDamage(attackDamage);
            }
        }

        // 重置攻击状态
        Invoke("ResetAttack", 0.3f);
    }

    void ResetAttack()
    {
        if (playerMovement != null)
        {
            playerMovement.isAttacking = false;
        }
    }

    // 可视化攻击范围
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}