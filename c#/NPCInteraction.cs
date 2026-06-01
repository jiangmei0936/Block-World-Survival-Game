// NPCInteraction.cs
using UnityEngine;
using UnityEngine.UI;

public class NPCInteraction : MonoBehaviour
{
    [Header("交互设置")]
    public float interactRange = 2f;
    public LayerMask playerLayer;
    
    [Header("任务设置")]
    public Item requiredItem; // 需要的物品
    public int requiredAmount = 3; // 需要的数量
    public string questCompleteMessage = "感谢你！任务完成！";
    
    [Header("UI设置")]
    public GameObject dialoguePanel;
    public Text dialogueText;
    public Text requirementText;
    public Button submitButton;
    
    private bool questCompleted = false;

    void Start()
    {
        if (dialoguePanel != null) 
            dialoguePanel.SetActive(false);
        
        if (submitButton != null)
            submitButton.onClick.AddListener(SubmitItems);
    }

    public void OpenDialogue()
    {
        if (questCompleted) return;
        
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
            dialogueText.text = "你能帮我找到这些材料吗？";
            UpdateRequirementText();
        }
    }

    void UpdateRequirementText()
    {
        if (requirementText == null || requiredItem == null) return;
        
        int currentAmount = Inv.Instance.GetItemAmount(requiredItem);
        requirementText.text = $"{requiredItem.name}: {currentAmount}/{requiredAmount}";
        
        if (submitButton != null)
            submitButton.interactable = (currentAmount >= requiredAmount);
    }

    public void SubmitItems()
    {
        if (questCompleted) return;
        
        if (Inv.Instance.HasItem(requiredItem, requiredAmount))
        {
            // 移除物品
            Inv.Instance.RemoveItem(requiredItem, requiredAmount);
            
            // 完成任务
            questCompleted = true;
            dialogueText.text = questCompleteMessage;
            
            if (requirementText != null)
                requirementText.text = "任务完成！";
            
            if (submitButton != null)
                submitButton.interactable = false;
            
            // 通关游戏
            GameManager.Instance.CompleteGame();
        }
        else
        {
            dialogueText.text = "材料还不够哦，再找找看！";
        }
    }

    public void CloseDialogue()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }
}