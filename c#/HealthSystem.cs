using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine.SceneManagement; // 添加场景管理命名空间

public class HealthSystem : MonoBehaviour
{
    // 添加游戏失败界面
    [Header("游戏失败界面")]
    public GameObject deathScreen; // 游戏失败界面Canvas
    public Button retryButton; // "再试一次"按钮


    public Image damageEffect;

    [Header("血量设置")]
    public int maxHealth = 5;
    public int currentHealth;
    public bool isInvulnerable = false;

    [Header("UI设置")]
    public List<Image> heartImages;

    [Header("伤害效果")]
    public float invulnerabilityDuration = 1.5f;
    public float flashDuration = 0.5f;
    public Color hurtColor = new Color(1, 0.3f, 0.3f, 1);

    [Header("全局变色设置")]
    public bool affectAllChildren = true;
    public bool includeInactive = true;

    [Header("音效设置")]
    public AudioClip hurtSound;
    public AudioClip deathSound;
    [Range(0, 1)] public float volume = 0.8f;
    public float pitchRange = 0.1f;

    private List<SpriteRenderer> allRenderers = new List<SpriteRenderer>();
    private Dictionary<SpriteRenderer, Color> originalColors = new Dictionary<SpriteRenderer, Color>();
    private AudioSource audioSource;

    [Header("敌人碰撞伤害")]
    public bool canTakeCollisionDamage = true;
    public float collisionDamageCooldown = 1f;
    private float lastCollisionDamageTime;

    [Header("回血设置")]
    public float healCooldown = 2f;
    public Color healColor = Color.green;
    public float healFlashDuration = 0.5f;
    public AudioClip healSound;
    public ParticleSystem healParticles;

    [Header("动画设置")]
    public string healAnimationTrigger = "Heal";
    public float healAnimationDuration = 0.5f;
    public string eatAnimationTrigger = "Eat";

    private Animator animator;
    private PlayerMovement playerMovement;
    private float lastHealTime = -10f;

    private HandItemController handItemController;

    void Awake()
    {
        // 初始化按钮事件
        if (retryButton != null)
        {
            retryButton.onClick.AddListener(RestartGame);
        }

        // 确保死亡界面初始隐藏
        if (deathScreen != null)
        {
            deathScreen.SetActive(false);
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = volume;
        }

        animator = GetComponent<Animator>();
        playerMovement = GetComponent<PlayerMovement>();

        CollectAllRenderers();
    }

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHearts();

        // 获取手持物品控制器 - 修改查找方式
        handItemController = GetComponentInChildren<HandItemController>(true); // 包含非激活对象

        if (handItemController == null)
        {
            //Debug.LogError("未找到手持物品控制器，将尝试在场景中查找");
            handItemController = FindObjectOfType<HandItemController>();

            if (handItemController == null)
            {
                Debug.LogError("场景中未找到任何手持物品控制器！");
            }
            else
            {
                Debug.Log("在场景中找到手持物品控制器");
            }
        }

        if (allRenderers.Count == 0)
        {
            CollectAllRenderers();
        }
    }

    void CollectAllRenderers()
    {
        allRenderers.Clear();
        originalColors.Clear();
        allRenderers = GetComponentsInChildren<SpriteRenderer>(includeInactive).ToList();

        foreach (var renderer in allRenderers)
        {
            if (renderer != null)
            {
                originalColors[renderer] = renderer.color;
            }
        }
    }

    void Update()
    {
        // 修改为尝试吃食物
        if (Input.GetKeyDown(KeyCode.T))
        {
            TryEatFood();
        }
    }

    // 尝试食用手中的食物
    public void TryEatFood()
    {
        // 检查冷却时间
        if (Time.time < lastHealTime + healCooldown)
        {
            Debug.Log("回血冷却中");
            return;
        }

        if (handItemController == null || handItemController.CurrentItem == null)
        {
            Debug.Log("手中没有物品");
            return;
        }

        // 获取当前手持物品
        Item currentItem = handItemController.CurrentItem;

        // 检查是否为可食用物品
        if (currentItem.itemType != ItemType.Food)
        {
            Debug.Log("手中物品不可食用");
            return;
        }

        // 播放吃食物的动画
        PlayEatAnimation();
    }

    // 播放吃食物的动画
    void PlayEatAnimation()
    {
        // 设置吃食物状态
        if (playerMovement != null)
        {
            playerMovement.isEating = true;
        }

        // 播放动画
        if (animator != null && !string.IsNullOrEmpty(eatAnimationTrigger))
        {
            animator.SetTrigger(eatAnimationTrigger);
        }

        // 在动画结束后执行实际吃食物的逻辑
        Invoke("ConsumeFood", 0.5f);
    }

    // 实际消耗食物
    void ConsumeFood()
    {
        if (handItemController == null || handItemController.CurrentItem == null)
        {
            Debug.Log("食物已消失");
            return;
        }

        Item foodItem = handItemController.CurrentItem;

        // 1. 消耗背包中的物品
        if (Inv.Instance != null)
        {
            Inv.Instance.RemoveItem(foodItem, 1);
        }

        // 2. 关键修复：更新手持物品显示
        if (Inv.Instance != null)
        {
            // 直接更新手持物品显示
            Inv.Instance.UpdateHandItemDisplay();
        }

        // 3. 执行回血效果
        PerformHealing(foodItem.healAmount);

        // 4. 重置状态
        if (playerMovement != null)
        {
            playerMovement.isEating = false;
        }
    }

    void PerformHealing(int healAmount)
    {
        lastHealTime = Time.time;
        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
        UpdateHearts();
        PlayHealSound();

        if (healParticles != null)
        {
            healParticles.transform.position = transform.position;
            healParticles.Play();
        }

        StartCoroutine(HealEffect());

        // 直接触发治疗动画，而不是调用已移除的方法
        if (animator != null && !string.IsNullOrEmpty(healAnimationTrigger))
        {
            animator.SetTrigger(healAnimationTrigger);
        }

        Debug.Log($"食用食物，恢复{healAmount}点生命!");
    }

    // 在HealthSystem中添加
    public void EndEating()
    {
        if (playerMovement != null)
        {
            playerMovement.isEating = false;
        }
    }

    // 回血特效协程
    IEnumerator HealEffect()
    {
        Dictionary<SpriteRenderer, Color> tempColors = new Dictionary<SpriteRenderer, Color>();

        foreach (var renderer in allRenderers)
        {
            if (renderer != null)
            {
                tempColors[renderer] = renderer.color;
            }
        }

        SetAllRenderersColor(healColor);

        yield return new WaitForSeconds(healFlashDuration);

        foreach (var renderer in allRenderers)
        {
            if (renderer != null && tempColors.ContainsKey(renderer))
            {
                renderer.color = tempColors[renderer];
            }
        }
    }

    // 播放回血音效
    void PlayHealSound()
    {
        if (healSound != null && audioSource != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(healSound, volume);
        }
    }

    public bool CanTakeDamage()
    {
        return !isInvulnerable && currentHealth > 0;
    }

    public void UpdateHearts()
    {
        if (heartImages == null) return;

        for (int i = 0; i < heartImages.Count; i++)
        {
            if (i < maxHealth)
            {
                heartImages[i].enabled = i < currentHealth;
            }
        }
    }

    public void TakeDamage(int damage = 1)
    {
        if (!CanTakeDamage()) return;

        if (Time.time < lastCollisionDamageTime + collisionDamageCooldown) return;

        lastCollisionDamageTime = Time.time;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        UpdateHearts();

        PlayHurtSound();

        if (allRenderers.Count > 0)
        {
            StartCoroutine(FlashEffect());
        }

        StartCoroutine(InvulnerabilityCoroutine());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // 播放受伤音效
    void PlayHurtSound()
    {
        if (hurtSound != null && audioSource != null)
        {
            audioSource.pitch = 1f + Random.Range(-pitchRange, pitchRange);
            audioSource.PlayOneShot(hurtSound, volume);
        }
    }

    IEnumerator InvulnerabilityCoroutine()
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(invulnerabilityDuration);
        isInvulnerable = false;
    }

    IEnumerator FlashEffect()
    {
        SetAllRenderersColor(hurtColor);
        yield return new WaitForSeconds(flashDuration);
        RestoreOriginalColors();
    }

    void SetAllRenderersColor(Color color)
    {
        foreach (var renderer in allRenderers)
        {
            if (renderer != null)
            {
                renderer.color = color;
            }
        }
    }

    void RestoreOriginalColors()
    {
        foreach (var renderer in allRenderers)
        {
            if (renderer != null && originalColors.ContainsKey(renderer))
            {
                renderer.color = originalColors[renderer];
            }
        }
    }

    void Die()
    {
        Debug.Log("角色死亡!");

        // 显示死亡界面
        ShowDeathScreen();

        PlayDeathSound();

        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.enabled = false;
            movement.isDead = true;
        }

        TrapDetector trapDetector = GetComponent<TrapDetector>();
        if (trapDetector != null) trapDetector.enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
        }

        StartCoroutine(DeathAnimation());
    }

    // 显示死亡界面
    void ShowDeathScreen()
    {
        if (deathScreen != null)
        {
            deathScreen.SetActive(true);

            // 暂停游戏
            Time.timeScale = 0f;

            // 解锁鼠标
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // 重新开始游戏
    public void RestartGame()
    {
        // 恢复时间
        Time.timeScale = 1f;

        // 重新加载当前场景
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void PlayDeathSound()
    {
        if (deathSound != null && audioSource != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(deathSound, volume);
        }
    }

    IEnumerator DeathAnimation()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D col in colliders)
        {
            col.enabled = false;
        }

        float duration = 1.5f;
        float elapsed = 0f;
        float blinkInterval = 0.1f;
        float nextBlinkTime = 0f;
        bool visible = true;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (elapsed >= nextBlinkTime)
            {
                visible = !visible;
                SetAllRenderersVisible(visible);
                nextBlinkTime = elapsed + blinkInterval;
            }

            yield return null;
        }

        SetAllRenderersVisible(false);
    }

    void SetAllRenderersVisible(bool visible)
    {
        foreach (var renderer in allRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = visible;
            }
        }
    }
}