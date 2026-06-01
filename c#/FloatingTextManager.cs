using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;

public class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager Instance;

    public GameObject textPrefab;
    public Canvas canvas;
    public float displayTime = 1.5f;
    public float floatSpeed = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ShowText(string message, Vector3 worldPosition, Color color)
    {
        GameObject textObj = Instantiate(textPrefab, canvas.transform);
        Text text = textObj.GetComponent<Text>();

        if (text != null)
        {
            text.text = message;
            text.color = color;

            // 转换世界坐标到屏幕坐标
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
            textObj.transform.position = screenPosition;

            // 启动浮动动画
            StartCoroutine(FloatText(textObj));
        }
    }

    IEnumerator FloatText(GameObject textObj)
    {
        float timer = 0f;
        Vector3 startPosition = textObj.transform.position;

        while (timer < displayTime)
        {
            timer += Time.deltaTime;
            textObj.transform.position = startPosition + new Vector3(0, floatSpeed * timer * 50, 0);

            // 淡出效果
            Text text = textObj.GetComponent<Text>();
            if (text != null)
            {
                Color c = text.color;
                c.a = 1 - (timer / displayTime);
                text.color = c;
            }

            yield return null;
        }

        Destroy(textObj);
    }
}