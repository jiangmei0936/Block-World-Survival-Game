using UnityEngine;
using System.Collections;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(HealthSystem))]
public class TrapDetector : MonoBehaviour
{
    [Header("陷阱设置")]
    public float damageInterval = 2f; // 伤害间隔(秒)
    public int trapDamage = 1;       // 每次伤害值
    public float trapEnterDamage = 1; // 进入陷阱时的初始伤害

    [Header("精确检测设置")]
    public float detectionOffsetY = -0.4f; // 从-0.3调整为-0.4，使检测点更低
    public float detectionRadius = 0.08f;   // 从0.1调整为0.08，减小检测范围
    public LayerMask trapLayer;

    private PlayerMovement playerMovement;
    private HealthSystem healthSystem;
    private Coroutine trapCoroutine;
    private bool isInTrap = false;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        healthSystem = GetComponent<HealthSystem>();
        // 确保组件存在
        if (playerMovement == null)
            Debug.LogError("PlayerMovement组件缺失!");
        if (healthSystem == null)
            Debug.LogError("HealthSystem组件缺失!");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 确保进入的是陷阱图层
        if (other.gameObject.layer == LayerMask.NameToLayer("Trap"))
        {
            Debug.Log("玩家进入陷阱区域");
            EnterTrap();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Trap"))
        {
            ExitTrap();
            Debug.Log("玩家离开陷阱区域");
        }
    }

    void EnterTrap()
    {
        if (isInTrap) return;

        isInTrap = true;

        // 通知玩家移动脚本进入陷阱状态
        if (playerMovement != null)
        {
            playerMovement.SetTrapState(true);
        }

        // 立即造成一次伤害
        if (healthSystem != null)
        {
            healthSystem.TakeDamage(trapDamage);
            Debug.Log($"陷阱初始伤害: {trapEnterDamage}");
        }

        // 开始持续伤害协程
        if (trapCoroutine != null)
            StopCoroutine(trapCoroutine);

        trapCoroutine = StartCoroutine(TrapDamageRoutine());
    }

    void ExitTrap()
    {
        if (!isInTrap) return;

        isInTrap = false;

        // 通知玩家移动脚本离开陷阱
        if (playerMovement != null)
        {
            playerMovement.SetTrapState(false);
        }

        // 停止伤害协程
        if (trapCoroutine != null)
        {
            StopCoroutine(trapCoroutine);
            trapCoroutine = null;
        }
    }

    IEnumerator TrapDamageRoutine()
    {
        while (isInTrap)
        {
            yield return new WaitForSeconds(damageInterval);

            if (healthSystem != null && isInTrap)
            {
                healthSystem.TakeDamage(trapDamage);
                Debug.Log($"陷阱持续伤害: {trapDamage}");
            }
        }
    }
}