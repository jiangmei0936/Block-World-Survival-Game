using UnityEngine;
using System.Collections;

public class WaterTrap : MonoBehaviour
{
    [Header("水陷阱设置")]
    public float damageInterval = 4f; // 伤害间隔4秒
    public int trapDamage = 1;        // 每次伤害值

    private PlayerMovement playerMovement;
    private HealthSystem healthSystem;
    private Coroutine damageCoroutine;
    private bool isInWater = false;   // 是否在水中
    private float timeInWater = 0f;   // 在水中停留的时间

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        healthSystem = GetComponent<HealthSystem>();

        if (playerMovement == null)
            Debug.LogError("PlayerMovement组件缺失!");
        if (healthSystem == null)
            Debug.LogError("HealthSystem组件缺失!");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 确保进入的是水图层
        if (other.gameObject.layer == LayerMask.NameToLayer("Water"))
        {
            EnterWater();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Water"))
        {
            ExitWater();
        }
    }

    void Update()
    {
        // 如果玩家在水中，更新计时器
        if (isInWater)
        {
            timeInWater += Time.deltaTime;
        }
    }

    void EnterWater()
    {
        if (isInWater) return;

        isInWater = true;
        timeInWater = 0f; // 重置计时器

        // 通知玩家移动脚本进入水中状态
        if (playerMovement != null)
        {
            playerMovement.SetWaterState(true);
        }

        // 开始伤害协程
        if (damageCoroutine != null)
            StopCoroutine(damageCoroutine);

        //damageCoroutine = StartCoroutine(WaterDamageRoutine());
    }

    void ExitWater()
    {
        if (!isInWater) return;

        isInWater = false;
        timeInWater = 0f; // 重置计时器

        // 通知玩家移动脚本离开水中状态
        if (playerMovement != null)
        {
            playerMovement.SetWaterState(false);
        }

        // 停止伤害协程
        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
            damageCoroutine = null;
        }
    }

    //IEnumerator WaterDamageRoutine()
    //{
    //    while (isInWater)
    //    {
    //        // 等待4秒
    //        yield return new WaitForSeconds(damageInterval);

    //        // 再次检查是否仍在水中
    //        if (isInWater && healthSystem != null)
    //        {
    //            // 应用伤害
    //            healthSystem.TakeDamage(trapDamage);
    //            Debug.Log($"水域伤害: {trapDamage} (在水中停留了 {timeInWater:F1} 秒)");

    //            // 重置计时器，用于下一次伤害计算
    //            timeInWater = 0f;
    //        }
    //    }
    //}
}