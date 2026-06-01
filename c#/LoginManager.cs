using UnityEngine;
using UnityEngine.SceneManagement;

public class LoginManager : MonoBehaviour
{
    // 开始游戏按钮点击事件
    public void StartGame()
    {
        // 加载游戏场景
        SceneManager.LoadScene("SampleScene");
    }
}