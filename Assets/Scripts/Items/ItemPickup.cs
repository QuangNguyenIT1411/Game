using UnityEngine;

namespace DungeonCrawler.Items
{
    /// <summary>
    /// Pickup item that adds itself to the Player inventory on contact.
    /// </summary>
    public class ItemPickup : MonoBehaviour
    {
        [SerializeField] private ItemType itemType;
        [SerializeField] private int amount = 1;

        public void Configure(ItemType newItemType, int newAmount)
        {
            itemType = newItemType;
            amount = Mathf.Max(1, newAmount);
            SetupVisual();
            SetupCollider();
        }

        private void Awake()
        {
            SetupVisual();
            SetupCollider();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (itemType == ItemType.Sword || itemType == ItemType.Armor)
            {
                EquipmentInventory equipmentInventory = other.GetComponentInParent<EquipmentInventory>();
                if (equipmentInventory == null)
                {
                    return;
                }

                EquipmentItem equipmentItem = itemType == ItemType.Sword
                    ? EquipmentItem.CreateSword()
                    : EquipmentItem.CreateArmor();
                equipmentInventory.Add(equipmentItem);
                Debug.Log($"Picked up item: {itemType}", this);
                Destroy(gameObject);
                return;
            }

            Inventory inventory = other.GetComponentInParent<Inventory>();
            if (inventory == null)
            {
                return;
            }

            inventory.Add(itemType, amount);
            Debug.Log($"Picked up item: {itemType}", this);
            Destroy(gameObject);
        }

        private void SetupCollider()
        {
            Collider2D pickupCollider = GetComponent<Collider2D>();
            if (pickupCollider == null)
            {
                pickupCollider = gameObject.AddComponent<CircleCollider2D>();
            }

            if (pickupCollider != null)
            {
                pickupCollider.isTrigger = true;
            }

            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
            }

            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0f;
                rb.freezeRotation = true;
            }
        }

        private void SetupVisual()
        {
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = CreateSquareSprite(GetColor(itemType));
            spriteRenderer.sortingOrder = 12;
            transform.localScale = new Vector3(0.35f, 0.35f, 1f);
        }

        private static Color GetColor(ItemType type)
        {
            return type switch
            {
                ItemType.Gold => new Color(1f, 0.8f, 0.05f),
                ItemType.Potion => new Color(0.9f, 0.05f, 0.2f),
                ItemType.Material => new Color(0.2f, 0.8f, 1f),
                ItemType.Sword => new Color(0.95f, 0.95f, 1f),
                ItemType.Armor => new Color(0.55f, 0.65f, 1f),
                _ => Color.white
            };
        }

        private static Sprite CreateSquareSprite(Color color)
        {
            Texture2D texture = new Texture2D(1, 1)
            {
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave
            };

            texture.SetPixel(0, 0, color);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
