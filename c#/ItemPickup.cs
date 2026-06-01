// ItemPickup.cs
using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public Item item;
    public int amount = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Inv inventory = other.GetComponent<Inv>();
            if (inventory != null)
            {
                inventory.AddItem(item, amount);
                Destroy(gameObject);
            }
        }
    }
}