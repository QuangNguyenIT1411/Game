using DungeonCrawler.Combat;
using UnityEngine;

namespace DungeonCrawler.Items
{
    /// <summary>
    /// Drops simple pickup items when this actor dies.
    /// </summary>
    public class LootDropper : MonoBehaviour
    {
        [SerializeField] private float dropRadius = 0.75f;

        private Health health;

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            if (health == null)
            {
                health = GetComponent<Health>();
            }

            if (health != null)
            {
                health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= OnDied;
            }
        }

        private void OnDied(Health deadHealth)
        {
            DropLoot();
        }

        private void DropLoot()
        {
            ItemType[] guaranteedDrops = { ItemType.Gold, ItemType.Potion, ItemType.Material, ItemType.Sword, ItemType.Armor };
            Debug.Log($"Dropping loot count: {guaranteedDrops.Length}", this);

            for (int i = 0; i < guaranteedDrops.Length; i++)
            {
                float angle = i * Mathf.PI * 2f / guaranteedDrops.Length;
                Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dropRadius;
                SpawnPickup(guaranteedDrops[i], 1, transform.position + (Vector3)offset);
            }
        }

        private static void SpawnPickup(ItemType itemType, int amount, Vector3 position)
        {
            GameObject pickupObject = new GameObject($"Pickup_{itemType}");
            pickupObject.transform.position = position;

            ItemPickup pickup = pickupObject.AddComponent<ItemPickup>();
            pickup.Configure(itemType, amount);
            Debug.Log($"Dropped item: {itemType} at position {position}", pickupObject);
        }
    }
}
