using UnityEngine;
using System.Collections; // 添加这个命名空间引用

[RequireComponent(typeof(Collider2D))]
public class DroppedItem : MonoBehaviour
{
    public Item item;
    public int amount = 1;
    public float pickupRadius = 1.5f;
    public float attractSpeed = 5f;
    public float rotationSpeed = 90f;
    public float floatHeight = 0.5f;
    public float floatSpeed = 2f;

    private Transform player;
    private Vector3 startPosition;
    private bool isAttracted = false;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (item != null && item.icon != null)
        {
            spriteRenderer.sprite = item.icon;
        }

        startPosition = transform.position;
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // 添加初始动画效果
        StartCoroutine(FloatAnimation());
    }

    void Update()
    {
        if (player != null && !isAttracted)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance <= pickupRadius)
            {
                isAttracted = true;
            }
        }

        if (isAttracted && player != null)
        {
            // 向玩家移动
            transform.position = Vector3.MoveTowards(
                transform.position,
                player.position,
                attractSpeed * Time.deltaTime
            );

            // 旋转效果
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

            // 检查是否到达玩家位置
            if (Vector3.Distance(transform.position, player.position) < 0.2f)
            {
                Pickup();
            }
        }
    }

    IEnumerator FloatAnimation()
    {
        float timer = 0f;
        Vector3 basePosition = startPosition;

        while (!isAttracted)
        {
            timer += Time.deltaTime * floatSpeed;
            float yOffset = Mathf.Sin(timer) * floatHeight;
            transform.position = basePosition + new Vector3(0, yOffset, 0);
            yield return null;
        }
    }

    void Pickup()
    {
        if (Inv.Instance != null)
        {
            bool added = Inv.Instance.AddItem(item, amount);
            if (added)
            {
                // 播放拾取音效
                //AudioManager.Instance?.PlaySound("Pickup");

                // 显示拾取提示
                FloatingTextManager.Instance?.ShowText(
                    $"+{amount} {item.name}",
                    transform.position,
                    Color.green
                );

                Destroy(gameObject);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}