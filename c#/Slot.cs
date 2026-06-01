using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Slot : MonoBehaviour, IPointerClickHandler
{
    public Item item;
    public int amount = 0;
    public Image icon;
    public Text amountText;

    [Header("选中高亮")]
    public Image highlightImage;
    public Color highlightColor = new Color(1, 1, 1, 0.3f);

    private Inv inventory;

    void Start()
    {
        // 获取背包引用
        inventory = Inv.Instance;
        UpdateSlot();
    }

    public void UpdateSlot()
    {
        if (item != null && amount > 0)
        {
            icon.sprite = item.icon;
            icon.enabled = true;

            if (amount > 1)
            {
                amountText.text = amount.ToString();
                amountText.enabled = true;
            }
            else
            {
                amountText.enabled = false;
            }
        }
        else
        {
            ClearSlot();
        }
    }

    public void ClearSlot()
    {
        item = null;
        amount = 0;
        icon.sprite = null;
        icon.enabled = false;
        amountText.enabled = false;
        SetHighlight(false);
    }

    public bool AddItem(Item newItem, int newAmount = 1)
    {
        if (item == null)
        {
            // 空槽位，直接添加
            item = newItem;
            amount = Mathf.Min(newAmount, newItem.maxStackSize);
            UpdateSlot();
            return amount == newAmount;
        }

        if (item == newItem && item.maxStackSize > 1)
        {
            // 相同物品可堆叠
            int availableSpace = item.maxStackSize - amount;

            if (newAmount <= availableSpace)
            {
                amount += newAmount;
                UpdateSlot();
                return true;
            }
            else
            {
                amount = item.maxStackSize;
                UpdateSlot();
                return false;
            }
        }

        return false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (inventory == null)
        {
            inventory = FindObjectOfType<Inv>();
        }

        if (item != null && inventory != null)
        {
            inventory.SelectSlot(this);
        }
    }

    public void SetHighlight(bool active)
    {
        if (highlightImage != null)
        {
            highlightImage.color = active ? highlightColor : Color.clear;
        }
    }

    public void RemoveItem(int amount = 1)
    {
        if (item == null || amount <= 0) return;

        amount = Mathf.Min(amount, this.amount);
        this.amount -= amount;

        if (this.amount <= 0)
        {
            ClearSlot();

            // 关键更新：如果当前槽位被选中，更新手持物品
            if (Inv.Instance != null && Inv.Instance.GetSelectedSlot() == this)
            {
                // 更新手持显示
                Inv.Instance.UpdateHandItem();
                // 同时取消选择状态
                Inv.Instance.DeselectSlot();
            }
        }
        else
        {
            UpdateSlot();

            // 更新手持显示（数量变化）
            if (Inv.Instance != null && Inv.Instance.GetSelectedSlot() == this)
            {
                Inv.Instance.UpdateHandItem();
            }
        }
    }
}