// BlockData.cs
using UnityEngine;

public enum BlockType { Dirt, Stone, Wood, Leaf }

[CreateAssetMenu(fileName = "New Block", menuName = "2D Minecraft/Block")]
public class BlockData : ScriptableObject
{
    public string blockName;
    public BlockType blockType;
    public Sprite sprite;
    public float hardness = 1f; // 破坏所需时间
    public ToolType requiredTool = ToolType.Hand; // 所需工具类型
    public Item dropItem; // 掉落物品
    public int dropAmount = 1; // 掉落数量

    public enum ToolType { Hand, Axe, Pickaxe, Shovel }
}