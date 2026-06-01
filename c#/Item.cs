using UnityEngine;
public enum ItemType
{
    Food,
    Weapon,
    Material,
    Other
}

// 在Item.cs文件中添加瓦片类型
public enum TileType
{
    None,
    Grass,
    Stone,
    Wood,
    Dirt
}

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public new string name = "新物品";
    public ItemType itemType = ItemType.Other;
    public Sprite icon = null;
    public Sprite handSprite = null; // 手持精灵
    public bool isDefaultItem = false;
    public int maxStackSize = 1;
    public int healAmount = 0; // 食物回血量
    public int damage = 0; // 武器伤害
}