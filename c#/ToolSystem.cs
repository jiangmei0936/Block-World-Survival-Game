// ToolSystem.cs
using UnityEngine;

public class ToolSystem : MonoBehaviour
{
    public enum ToolType { Hand, Axe, Pickaxe, Shovel }
    public ToolType currentTool = ToolType.Hand;

    public void SwitchTool(ToolType newTool)
    {
        currentTool = newTool;
        // 更新工具贴图等
    }

    public bool CanBreak(BlockData block)
    {
        // 如果没有要求工具，或者当前工具匹配，则返回true
        if (block.requiredTool == BlockData.ToolType.Hand ||
            block.requiredTool == (BlockData.ToolType)currentTool)
            return true;

        return false;
    }
}