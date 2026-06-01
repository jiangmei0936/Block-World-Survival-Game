using UnityEngine;

public class HandItemController : MonoBehaviour
{
    public SpriteRenderer handItemRenderer;

    [Header("固定位置偏移")]
    public Vector3 fixedOffset = new Vector3(1f, 0.1f, 0); // 合并为单一固定偏移

    [Header("固定旋转设置")]
    public float fixedRotationZ = -374.9f; // 单一固定旋转值

    private Item currentItem;
    private Vector3 originalScale;

    public Item CurrentItem => currentItem;

    void Awake()
    {
        // 保存原始缩放值
        originalScale = transform.localScale;
        // 确保初始旋转正确
        transform.localEulerAngles = new Vector3(0, 0, fixedRotationZ);
    }

    public void EquipItem(Item item)
    {
        // 如果物品为空或数量为0，强制清空
        if (item == null || (Inv.Instance != null && Inv.Instance.GetItemAmount(item) <= 0))
        {
            item = null;
        }

        // 如果装备的是同一个物品且数量>0，不需要更新
        if (currentItem == item && item != null && Inv.Instance.GetItemAmount(item) > 0)
            return;

        currentItem = item;

        if (item == null)
        {
            handItemRenderer.sprite = null;
            handItemRenderer.enabled = false;
            return;
        }

        if (item.handSprite != null)
        {
            handItemRenderer.sprite = item.handSprite;
            handItemRenderer.enabled = true;
            ApplyFixedPosition(); // 应用固定位置和旋转
        }
        else
        {
            Debug.LogWarning($"物品 {item.name} 没有设置手持精灵");
            handItemRenderer.enabled = false;
        }
    }

    // 应用固定位置和旋转
    private void ApplyFixedPosition()
    {
        if (handItemRenderer == null || !handItemRenderer.enabled)
            return;

        // 应用固定位置偏移
        transform.localPosition = fixedOffset;

        // 应用固定旋转
        transform.localEulerAngles = new Vector3(0, 0, fixedRotationZ);

        // 重置缩放
        transform.localScale = originalScale;

        // 取消精灵翻转
        handItemRenderer.flipX = false;
    }

    public void UpdateItemDisplay()
    {
        // 确保当物品数量为0时清空手持
        if (currentItem != null && Inv.Instance != null &&
            Inv.Instance.GetItemAmount(currentItem) <= 0)
        {
            EquipItem(null);
        }
        else if (currentItem != null)
        {
            // 更新为固定位置
            ApplyFixedPosition();
        }
    }
}