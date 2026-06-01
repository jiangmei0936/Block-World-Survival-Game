using UnityEngine;
using System.Collections.Generic;

public class Inv : MonoBehaviour
{
    public static Inv Instance { get; private set; }

    [Header("UI元素")]
    public GameObject inv;
    public Transform slotsParent;
    public GameObject slotPrefab;
    public int slotCount = 12;

    [Header("初始物品")]
    public List<Item> startingItems = new List<Item>();
    public List<int> startingAmounts = new List<int>();

    private Slot[] allSlots;
    private Slot selectedSlot;

    [Header("手持系统")]
    public HandItemController handItemController;
    private Item currentlyEquippedItem;

    public Slot GetSelectedSlot()
    {
        return selectedSlot;
    }

    private void Awake()
    {
        // 设置单例
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 确保背包在场景切换时不被销毁
        }
        else
        {
            Destroy(gameObject);
        }

        // 添加：确保背包在场景切换时保持选择状态
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    // 在Start方法中调用恢复选择
    void Start()
    {
        if (inv != null) inv.SetActive(false);
        CreateSlots();
        AddStartingItems();
        RestoreSelection(); // 恢复选择状态
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInv();
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            DeselectSlot();
        }
    }

    void CreateSlots()
    {
        // 清空现有槽位
        foreach (Transform child in slotsParent)
        {
            Destroy(child.gameObject);
        }

        // 创建新槽位
        allSlots = new Slot[slotCount];
        for (int i = 0; i < slotCount; i++)
        {
            GameObject newSlot = Instantiate(slotPrefab, slotsParent);
            allSlots[i] = newSlot.GetComponent<Slot>();
            allSlots[i].ClearSlot();
        }
    }

    void AddStartingItems()
    {
        int count = Mathf.Min(startingItems.Count, startingAmounts.Count);
        for (int i = 0; i < count; i++)
        {
            if (startingItems[i] != null && startingAmounts[i] > 0)
            {
                AddItem(startingItems[i], startingAmounts[i]);
            }
        }
    }

    public bool AddItem(Item item, int amount = 1)
    {
        // 1. 尝试堆叠到现有槽位
        foreach (Slot slot in allSlots)
        {
            if (slot.item == item && slot.amount < item.maxStackSize)
            {
                int spaceLeft = item.maxStackSize - slot.amount;
                int toAdd = Mathf.Min(amount, spaceLeft);
                slot.amount += toAdd;
                slot.UpdateSlot();
                amount -= toAdd;
                if (amount <= 0) return true;
            }
        }

        // 2. 添加到空槽位
        foreach (Slot slot in allSlots)
        {
            if (slot.item == null)
            {
                slot.item = item;
                slot.amount = Mathf.Min(amount, item.maxStackSize);
                slot.UpdateSlot();
                amount -= slot.amount;
                if (amount <= 0) return true;
            }
        }

        // 3. 无法添加所有物品
        return amount == 0;
    }

    public bool HasItem(Item item, int requiredAmount = 1)
    {
        int totalAmount = 0;
        foreach (Slot slot in allSlots)
        {
            if (slot.item == item)
            {
                totalAmount += slot.amount;
                if (totalAmount >= requiredAmount) return true;
            }
        }
        return false;
    }

    public void ToggleInv()
    {
        if (inv != null)
        {
            bool newState = !inv.activeSelf;
            inv.SetActive(newState);

            // 仅在关闭背包时取消UI高亮，但保持选择状态
            if (!newState)
            {
                if (selectedSlot != null)
                {
                    // 只取消UI高亮，不取消选择状态
                    selectedSlot.SetHighlight(false);
                }
            }
            else
            {
                // 打开背包时恢复高亮
                if (selectedSlot != null)
                {
                    selectedSlot.SetHighlight(true);
                }
            }
        }
    }

    public void CloseInv()
    {
        if (inv != null)
        {
            inv.SetActive(false);
            // 仅取消选择槽位，不取消装备
            if (selectedSlot != null)
            {
                selectedSlot.SetHighlight(false);
                selectedSlot = null;
            }
        }
    }

    public void SelectSlot(Slot slot)
    {
        // 如果点击的是已选中的槽位，则取消选择
        if (selectedSlot == slot)
        {
            DeselectSlot();
            return;
        }

        // 取消之前的选择（UI高亮）
        if (selectedSlot != null)
        {
            selectedSlot.SetHighlight(false);
        }

        // 选择新槽位
        selectedSlot = slot;
        slot.SetHighlight(true);

        // 装备物品
        if (handItemController != null && slot.item != null)
        {
            handItemController.EquipItem(slot.item);
            currentlyEquippedItem = slot.item;
        }
        else if (handItemController != null)
        {
            handItemController.EquipItem(null);
            currentlyEquippedItem = null;
        }
        // 持久化选择状态
        PlayerPrefs.SetInt("SelectedSlotIndex", System.Array.IndexOf(allSlots, slot));
    }

    public Item GetEquippedItem()
    {
        return currentlyEquippedItem;
    }

    public void UpdateHandItemDisplay()
    {
        // 如果当前选中的槽位为空或物品数量为0，则取消选择
        if (selectedSlot != null && (selectedSlot.item == null || selectedSlot.amount <= 0))
        {
            DeselectSlot();
        }
        // 否则更新手持物品显示
        else if (handItemController != null && selectedSlot != null)
        {
            handItemController.EquipItem(selectedSlot.item);
        }
    }

    public int GetItemAmount(Item item)
    {
        if (item == null) return 0;
        int totalAmount = 0;
        foreach (Slot slot in allSlots)
        {
            if (slot.item == item)
            {
                totalAmount += slot.amount;
            }
        }
        return totalAmount;
    }

    public void DeselectSlot()
    {
        if (selectedSlot != null)
        {
            selectedSlot.SetHighlight(false);
        }

        selectedSlot = null;

        if (handItemController != null)
        {
            handItemController.EquipItem(null);
        }

        currentlyEquippedItem = null;

        // 清除持久化选择状态
        PlayerPrefs.DeleteKey("SelectedSlotIndex");
    }

    // 新增方法：恢复选择状态
    public void RestoreSelection()
    {
        int selectedIndex = PlayerPrefs.GetInt("SelectedSlotIndex", -1);
        if (selectedIndex >= 0 && selectedIndex < allSlots.Length)
        {
            Slot slot = allSlots[selectedIndex];
            if (slot.item != null && slot.amount > 0)
            {
                SelectSlot(slot);
            }
            else
            {
                DeselectSlot();
            }
        }
    }

    public bool RemoveItem(Item item, int amount = 1)
    {
        if (!HasItem(item, amount)) return false;

        // 从后往前移除物品（避免索引问题）
        for (int i = allSlots.Length - 1; i >= 0; i--)
        {
            Slot slot = allSlots[i];
            if (slot.item == item && slot.amount > 0)
            {
                int toRemove = Mathf.Min(amount, slot.amount);
                slot.amount -= toRemove;
                amount -= toRemove;

                // 如果这个槽位是当前选中的槽位，更新手持物品
                if (slot == selectedSlot)
                {
                    UpdateHandItem();
                }
                else
                {
                    slot.UpdateSlot();
                }

                if (slot.amount <= 0)
                {
                    slot.ClearSlot();
                    // 如果是当前选中的槽位，取消选择
                    if (selectedSlot == slot)
                    {
                        DeselectSlot();
                    }
                }

                if (amount <= 0) return true;
            }
        }
        return amount == 0;
    }

    public void UpdateHandItem()
    {
        if (handItemController != null && selectedSlot != null)
        {
            // 检查物品是否仍然存在
            if (selectedSlot.item != null && selectedSlot.amount > 0)
            {
                handItemController.EquipItem(selectedSlot.item);
            }
            else
            {
                handItemController.EquipItem(null);
                DeselectSlot();
            }
        }
    }
}