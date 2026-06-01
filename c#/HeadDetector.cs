using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadDetector : MonoBehaviour
{
    private OxygenSystem oxygenSystem;

    void Start()
    {
        oxygenSystem = GetComponentInParent<OxygenSystem>();
        if (oxygenSystem == null)
        {
            Debug.LogError("OxygenSystem component not found in parent!");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Water"))
        {
            if (oxygenSystem != null)
            {
                // 正确的方法调用
                oxygenSystem.SetHeadUnderwater(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Water"))
        {
            if (oxygenSystem != null)
            {
                // 修正这里：使用 SetHeadUnderwater(false)
                oxygenSystem.SetHeadUnderwater(false);
            }
        }
    }
}