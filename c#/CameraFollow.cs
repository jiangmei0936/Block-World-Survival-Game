using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    // 要跟随的目标（通常是玩家角色）
    public Transform target;

    // 相机与目标之间的偏移量
    public Vector3 offset = new Vector3(0, 0, -10);

    // 跟随的平滑度（值越大跟随越快）
    public float smoothSpeed = 5f;

    void LateUpdate()
    {
        // 确保有目标可跟随
        if (target == null)
            return;

        // 计算相机应该在的位置
        Vector3 desiredPosition = target.position + offset;

        // 使用插值平滑移动相机
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // 设置相机位置
        transform.position = smoothedPosition;
    }
}
