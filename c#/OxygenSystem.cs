using UnityEngine;
using UnityEngine.UI;

public class OxygenSystem : MonoBehaviour
{
    [Header("氧气设置")]
    public float maxOxygen = 100f;
    public float currentOxygen;
    public float oxygenDepletionRate = 10f; // 氧气消耗速率
    public float oxygenRecoveryRate = 20f; // 氧气恢复速率
    public bool isHeadUnderwater = false;

    [Header("氧气条UI")]
    public Slider oxygenSlider;
    public Image oxygenFill;
    public Gradient oxygenGradient; // 根据氧气量改变颜色

    [Header("警告设置")]
    public AudioClip lowOxygenSound;
    public float lowOxygenThreshold = 30f;
    public GameObject lowOxygenWarning;
    private bool lowOxygenWarningActive = false;

    private AudioSource audioSource;
    private HealthSystem healthSystem;

    void Start()
    {
        currentOxygen = maxOxygen;
        healthSystem = GetComponent<HealthSystem>();
        audioSource = GetComponent<AudioSource>();

        if (oxygenSlider != null)
        {
            oxygenSlider.maxValue = maxOxygen;
            oxygenSlider.value = maxOxygen;
            oxygenSlider.gameObject.SetActive(false);
        }

        if (lowOxygenWarning != null)
        {
            lowOxygenWarning.SetActive(false);
        }
    }

    void Update()
    {
        if (isHeadUnderwater)
        {
            // 消耗氧气
            currentOxygen -= oxygenDepletionRate * Time.deltaTime;
            if (currentOxygen <= 0)
            {
                currentOxygen = 0;
                // 氧气耗尽，开始造成伤害
                if (healthSystem != null && healthSystem.CanTakeDamage())
                {
                    healthSystem.TakeDamage(1);
                }
            }
        }
        else if (currentOxygen < maxOxygen)
        {
            // 恢复氧气
            currentOxygen += oxygenRecoveryRate * Time.deltaTime;
            if (currentOxygen > maxOxygen)
            {
                currentOxygen = maxOxygen;
            }
        }

        // 更新UI
        UpdateOxygenUI();

        // 检查低氧气警告
        CheckLowOxygenWarning();
    }

    void UpdateOxygenUI()
    {
        // 确保所有UI组件都存在
        if (oxygenSlider == null || oxygenFill == null)
        {
            // 尝试自动查找氧气条
            if (oxygenSlider == null)
            {
                oxygenSlider = GameObject.Find("OxygenBar")?.GetComponent<Slider>();
                if (oxygenSlider == null)
                {
                    Debug.LogWarning("Oxygen Slider not found!");
                    return;
                }
            }

            // 尝试获取填充图像
            if (oxygenFill == null && oxygenSlider != null)
            {
                oxygenFill = oxygenSlider.fillRect?.GetComponent<Image>();
                if (oxygenFill == null)
                {
                    Debug.LogWarning("Oxygen fill image not found!");
                }
            }
        }

        // 更新UI前再次检查
        if (oxygenSlider != null)
        {
            oxygenSlider.value = currentOxygen;

            // 更新填充颜色（如果存在）
            if (oxygenFill != null)
            {
                oxygenFill.color = oxygenGradient.Evaluate(oxygenSlider.normalizedValue);
            }

            // 控制氧气条显示/隐藏
            bool shouldShow = isHeadUnderwater || currentOxygen < maxOxygen;
            if (shouldShow != oxygenSlider.gameObject.activeSelf)
            {
                oxygenSlider.gameObject.SetActive(shouldShow);
            }
        }
    }

    void CheckLowOxygenWarning()
    {
        if (lowOxygenWarning == null) return;

        bool shouldWarn = isHeadUnderwater && currentOxygen <= lowOxygenThreshold;

        if (shouldWarn && !lowOxygenWarningActive)
        {
            lowOxygenWarning.SetActive(true);
            lowOxygenWarningActive = true;

            // 播放警告音效
            if (lowOxygenSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(lowOxygenSound);
            }
        }
        else if (!shouldWarn && lowOxygenWarningActive)
        {
            lowOxygenWarning.SetActive(false);
            lowOxygenWarningActive = false;
        }
    }

    public void SetHeadUnderwater(bool underwater)
    {
        isHeadUnderwater = underwater;
    }
}