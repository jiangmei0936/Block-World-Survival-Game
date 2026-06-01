using UnityEngine;
using UnityEngine.UI;

public class ProgressIndicator : MonoBehaviour
{
    public Image progressCircle; // 进度圆环图像

    public void UpdateProgress(float progress)
    {
        // 确保进度值在0-1之间
        progress = Mathf.Clamp01(progress);

        // 更新进度圆环的填充量
        if (progressCircle != null)
        {
            progressCircle.fillAmount = progress;

            // 根据进度改变颜色（可选）
            progressCircle.color = Color.Lerp(Color.yellow, Color.red, progress);
        }
    }
}