using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class TileInteraction : MonoBehaviour
{
    private bool isMining = false;

    public KeyCode destroyKey = KeyCode.F;
    public float interactionRange = 2f;
    public LayerMask tileLayer;
    public Color rayColor = Color.red;
    public float rayDuration = 1f;

    [Header("掉落系统")]
    public GameObject droppedItemPrefab;
    public Item[] possibleDrops;
    public float dropChance = 0.7f;
    public int minDropAmount = 1;
    public int maxDropAmount = 3;

    [Header("破坏系统")]
    public float destructionTime = 5f; // 破坏所需时间（秒）
    public ParticleSystem destructionParticle; // 破坏过程中的粒子效果
    public ParticleSystem destroyEffect; // 破坏完成时的粒子效果
    public Slider destructionProgressBar; // 破坏进度条（可选）
    public GameObject progressIndicatorPrefab; // 破坏进度指示器预制体

    [Header("瓦片地图设置")]
    public Tilemap destructibleTilemap;

    [Header("控制模式")]
    public ControlMode controlMode = ControlMode.MouseDirection;
    public enum ControlMode { CharacterFacing, MouseDirection }

    [Header("玩家动画")]
    public Animator playerAnimator; // 玩家动画控制器
    public string miningAnimParam = "Mining"; // 挖掘动画参数名

    // 私有变量
    private Vector3Int? currentDestructionTarget = null;
    private float currentDestructionProgress = 0f;
    private ParticleSystem activeParticleEffect;
    private GameObject activeProgressIndicator;
    private Camera mainCamera;
    private ProgressIndicator progressIndicatorComponent;
    private TileBase currentTileType; // 当前正在破坏的瓦片类型

    // 添加瓦片到物品的映射
    [System.Serializable]
    public class TileDropMapping
    {
        public TileBase tile; // 瓦片类型
        public Item item;     // 对应掉落的物品
    }

    [Header("瓦片掉落映射")]
    public TileDropMapping[] tileDropMappings; // 瓦片与掉落物品的映射关系

    private void Start()
    {
        mainCamera = Camera.main;

        // 验证瓦片地图引用
        if (destructibleTilemap == null)
        {
            Debug.LogError("[TileInteraction] 未设置可破坏瓦片地图引用！");
            enabled = false;
            return;
        }

        // 确保瓦片地图不是静态的
        if (destructibleTilemap.gameObject.isStatic)
        {
            destructibleTilemap.gameObject.isStatic = false;
        }

        // 添加TilemapCollider2D
        TilemapCollider2D tilemapCollider = destructibleTilemap.GetComponent<TilemapCollider2D>();
        if (tilemapCollider == null)
        {
            tilemapCollider = destructibleTilemap.gameObject.AddComponent<TilemapCollider2D>();
            tilemapCollider.usedByComposite = false;
        }

        // 添加Rigidbody2D确保物理检测
        Rigidbody2D rb = destructibleTilemap.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = destructibleTilemap.gameObject.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
        }

        // 初始化进度条
        if (destructionProgressBar != null)
        {
            destructionProgressBar.gameObject.SetActive(false);
        }

        // 自动获取Animator组件
        if (playerAnimator == null)
        {
            playerAnimator = GetComponent<Animator>();
            if (playerAnimator == null)
            {
                Debug.LogWarning("玩家Animator未找到，请手动分配");
            }
        }
    }

    private void Update()
    {
        // 处理破坏进度
        HandleDestructionProgress();

        // 处理按键按下（持续检测）
        if (Input.GetKey(destroyKey))
        {
            // 只有在没有挖掘时才尝试开始新挖掘
            if (!isMining)
            {
                AttemptDestroyTile();
            }
        }
        else
        {
            // 松开按键时重置破坏进度
            if (isMining)
            {
                ResetDestruction();
            }
        }
    }

    private void HandleDestructionProgress()
    {
        if (currentDestructionTarget == null) return;

        // 更新破坏进度
        currentDestructionProgress += Time.deltaTime;

        // 更新进度条
        if (destructionProgressBar != null)
        {
            destructionProgressBar.value = currentDestructionProgress / destructionTime;
        }

        // 更新粒子效果位置
        if (activeParticleEffect != null && currentDestructionTarget.HasValue)
        {
            Vector3 worldPos = destructibleTilemap.GetCellCenterWorld(currentDestructionTarget.Value);
            activeParticleEffect.transform.position = worldPos;
        }

        // 更新进度指示器
        if (progressIndicatorComponent != null)
        {
            float progress = currentDestructionProgress / destructionTime;
            progressIndicatorComponent.UpdateProgress(progress);
        }

        // 检查是否完成破坏
        if (currentDestructionProgress >= destructionTime)
        {
            DestroyTileAtPosition(currentDestructionTarget.Value);
            ResetDestruction();
        }
    }

    private void AttemptDestroyTile()
    {
        Vector2 direction = GetRayDirection();
        Vector3 startPosition = transform.position;

        // 显示射线
        Debug.DrawRay(startPosition, direction * interactionRange, rayColor, rayDuration);

        // 执行射线检测
        RaycastHit2D hit = Physics2D.Raycast(
            startPosition,
            direction,
            interactionRange,
            tileLayer
        );

        if (hit.collider != null)
        {
            // 确认命中的是瓦片地图
            if (hit.collider.gameObject == destructibleTilemap.gameObject)
            {
                Vector3Int tilePosition = FindTileAtHitPosition(hit.point, direction);

                if (tilePosition != new Vector3Int(int.MinValue, int.MinValue, int.MinValue))
                {
                    // 获取瓦片类型
                    TileBase tileType = destructibleTilemap.GetTile(tilePosition);

                    // 如果目标改变，重置进度
                    if (currentDestructionTarget != tilePosition)
                    {
                        ResetDestruction();
                        currentDestructionTarget = tilePosition;
                        currentTileType = tileType; // 保存当前瓦片类型
                        StartDestructionEffect(tilePosition);
                    }
                }
                else
                {
                    ResetDestruction();
                }
            }
            else
            {
                ResetDestruction();
            }
        }
        else
        {
            ResetDestruction();
        }
    }

    private void StartDestructionEffect(Vector3Int tilePosition)
    {
        // 重置进度
        currentDestructionProgress = 0f;

        // 显示进度条
        if (destructionProgressBar != null)
        {
            destructionProgressBar.gameObject.SetActive(true);
            destructionProgressBar.value = 0f;
        }

        // 创建粒子效果
        if (destructionParticle != null)
        {
            Vector3 effectPosition = destructibleTilemap.GetCellCenterWorld(tilePosition);
            activeParticleEffect = Instantiate(destructionParticle, effectPosition, Quaternion.identity);
            activeParticleEffect.Play();
        }

        // 创建进度指示器
        if (progressIndicatorPrefab != null)
        {
            Vector3 indicatorPos = destructibleTilemap.GetCellCenterWorld(tilePosition) + Vector3.up * 0.5f;
            activeProgressIndicator = Instantiate(progressIndicatorPrefab, indicatorPos, Quaternion.identity);
            progressIndicatorComponent = activeProgressIndicator.GetComponent<ProgressIndicator>();
        }

        isMining = true;

        // 启动挖掘动画
        StartMiningAnimation();
    }

    private void StartMiningAnimation()
    {
        if (playerAnimator != null && !string.IsNullOrEmpty(miningAnimParam))
        {
            playerAnimator.SetBool(miningAnimParam, true);
        }
    }

    private void StopMiningAnimation()
    {
        if (playerAnimator != null && !string.IsNullOrEmpty(miningAnimParam))
        {
            playerAnimator.SetBool(miningAnimParam, false);
        }
    }

    private void ResetDestruction()
    {
        // 停止并销毁粒子效果
        if (activeParticleEffect != null)
        {
            activeParticleEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Destroy(activeParticleEffect.gameObject, activeParticleEffect.main.duration);
            activeParticleEffect = null;
        }

        // 重置挖掘状态
        isMining = false;

        // 隐藏进度条
        if (destructionProgressBar != null)
        {
            destructionProgressBar.gameObject.SetActive(false);
        }

        // 销毁进度指示器
        if (activeProgressIndicator != null)
        {
            Destroy(activeProgressIndicator);
            activeProgressIndicator = null;
            progressIndicatorComponent = null;
        }

        // 停止挖掘动画
        StopMiningAnimation();

        // 重置进度
        currentDestructionTarget = null;
        currentDestructionProgress = 0f;
    }

    // 获取射线方向
    private Vector2 GetRayDirection()
    {
        if (controlMode == ControlMode.MouseDirection)
        {
            return GetMouseDirection();
        }
        else
        {
            return GetCharacterFacingDirection();
        }
    }

    // 获取角色朝向方向
    private Vector2 GetCharacterFacingDirection()
    {
        // 使用SpriteRenderer判断角色朝向
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            return spriteRenderer.flipX ? Vector2.left : Vector2.right;
        }

        // 备用方案：使用transform.localScale判断
        return transform.localScale.x > 0 ? Vector2.right : Vector2.left;
    }

    // 获取鼠标方向
    private Vector2 GetMouseDirection()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return Vector2.right;
            }
        }

        // 获取鼠标在屏幕上的位置
        Vector3 mouseScreenPos = Input.mousePosition;

        // 设置z坐标（屏幕空间到世界空间转换需要）
        mouseScreenPos.z = -mainCamera.transform.position.z;

        // 转换为世界坐标
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0; // 确保z坐标为0

        // 计算从玩家到鼠标的方向
        return (mouseWorldPos - transform.position).normalized;
    }

    // 精确找到命中点的瓦片位置
    private Vector3Int FindTileAtHitPosition(Vector3 hitPoint, Vector2 rayDirection)
    {
        // 方法1：直接使用命中点转换
        Vector3Int tilePos = destructibleTilemap.WorldToCell(hitPoint);
        if (destructibleTilemap.HasTile(tilePos))
        {
            return tilePos;
        }

        // 方法2：向射线方向微调位置
        Vector3 adjustedPos = hitPoint + (Vector3)rayDirection.normalized * 0.1f;
        tilePos = destructibleTilemap.WorldToCell(adjustedPos);
        if (destructibleTilemap.HasTile(tilePos))
        {
            return tilePos;
        }

        // 方法3：检查周围相邻瓦片
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector3Int neighborPos = destructibleTilemap.WorldToCell(hitPoint) + new Vector3Int(x, y, 0);
                if (destructibleTilemap.HasTile(neighborPos))
                {
                    return neighborPos;
                }
            }
        }

        // 方法4：扩展搜索
        for (int x = -2; x <= 2; x++)
        {
            for (int y = -2; y <= 2; y++)
            {
                Vector3Int testPos = destructibleTilemap.WorldToCell(hitPoint) + new Vector3Int(x, y, 0);
                if (destructibleTilemap.HasTile(testPos))
                {
                    return testPos;
                }
            }
        }

        return new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
    }

    private void DestroyTileAtPosition(Vector3Int tilePosition)
    {
        // 播放破坏效果
        PlayDestroyEffect(tilePosition);

        // 生成掉落物（传入当前瓦片类型）
        TrySpawnDrop(tilePosition, currentTileType);

        // 直接移除瓦片
        destructibleTilemap.SetTile(tilePosition, null);

        // 强制刷新瓦片地图
        destructibleTilemap.RefreshTile(tilePosition);
        destructibleTilemap.RefreshAllTiles();
    }

    // 修改后的TrySpawnDrop方法，添加了tileType参数
    private void TrySpawnDrop(Vector3Int tilePosition, TileBase tileType)
    {
        // 如果没有设置掉落物品预制体，直接返回
        if (droppedItemPrefab == null) return;

        // 检查是否有掉落机会
        if (Random.value > dropChance) return;

        // 查找对应的掉落物品
        Item dropItem = FindDropItemForTile(tileType);

        // 如果没有找到对应的掉落物品，直接返回
        if (dropItem == null) return;

        // 确定掉落数量
        int amount = Random.Range(minDropAmount, maxDropAmount + 1);

        // 生成掉落位置
        Vector3 dropPosition = destructibleTilemap.GetCellCenterWorld(tilePosition);

        // 实例化掉落物
        GameObject drop = Instantiate(droppedItemPrefab, dropPosition, Quaternion.identity);

        // 设置掉落物属性
        DroppedItem droppedItem = drop.GetComponent<DroppedItem>();
        if (droppedItem != null)
        {
            droppedItem.item = dropItem;
            droppedItem.amount = amount;
        }
    }

    // 根据瓦片类型查找对应的掉落物品
    private Item FindDropItemForTile(TileBase tileType)
    {
        // 如果没有设置映射关系，返回null
        if (tileDropMappings == null || tileDropMappings.Length == 0) return null;

        // 遍历映射关系，查找匹配的瓦片类型
        foreach (TileDropMapping mapping in tileDropMappings)
        {
            if (mapping.tile == tileType)
            {
                return mapping.item;
            }
        }

        // 没有找到匹配的映射，返回null
        return null;
    }

    private void PlayDestroyEffect(Vector3Int tilePosition)
    {
        if (destroyEffect != null)
        {
            Vector3 effectPosition = destructibleTilemap.GetCellCenterWorld(tilePosition);
            ParticleSystem effectInstance = Instantiate(destroyEffect, effectPosition, Quaternion.identity);
            effectInstance.Play();
            Destroy(effectInstance.gameObject, effectInstance.main.duration);
        }
    }

    // 在场景视图中绘制Gizmos
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.blue;
        Vector2 direction = GetRayDirection();
        Gizmos.DrawRay(transform.position, direction * interactionRange);
    }
}