using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonCrawler.Items
{
    public class Chest : MonoBehaviour
    {
        [SerializeField] private float openRange = 1.2f;
        [SerializeField] private bool opened;

        private SpriteRenderer spriteRenderer;
        private TextMesh promptText;
        private Transform player;

        private void Awake()
        {
            SetupVisual();
            SetupCollider();
            SetupPrompt();
        }

        private void Update()
        {
            if (opened)
            {
                SetPromptVisible(false);
                return;
            }

            if (player == null)
            {
                GameObject playerObject = GameObject.Find("Player");
                player = playerObject != null ? playerObject.transform : null;
            }

            bool playerNear = player != null && Vector2.Distance(player.position, transform.position) <= openRange;
            SetPromptVisible(playerNear);

            Keyboard keyboard = Keyboard.current;
            if (playerNear && keyboard != null && keyboard.fKey.wasPressedThisFrame)
            {
                Open();
            }
        }

        public void Open()
        {
            if (opened)
            {
                return;
            }

            opened = true;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(0.28f, 0.25f, 0.20f, 1f);
            }

            SetPromptVisible(false);
            Debug.Log("Chest opened", this);
            DropLoot();
        }

        private void DropLoot()
        {
            ItemType[] lootTable =
            {
                ItemType.Gold,
                ItemType.Potion,
                ItemType.Material,
                ItemType.Sword,
                ItemType.Armor
            };

            int count = Random.Range(2, 5);
            for (int i = 0; i < count; i++)
            {
                ItemType itemType = lootTable[Random.Range(0, lootTable.Length)];
                float angle = i * Mathf.PI * 2f / count;
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 0.75f;
                SpawnPickup(itemType, itemType == ItemType.Gold ? Random.Range(2, 6) : 1, transform.position + offset);
            }
        }

        private static void SpawnPickup(ItemType itemType, int amount, Vector3 position)
        {
            GameObject pickupObject = new GameObject($"Pickup_{itemType}");
            pickupObject.transform.position = position;
            ItemPickup pickup = pickupObject.AddComponent<ItemPickup>();
            pickup.Configure(itemType, amount);
        }

        private void SetupVisual()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = CreateChestSprite();
            spriteRenderer.color = opened ? new Color(0.28f, 0.25f, 0.20f, 1f) : Color.white;
            spriteRenderer.sortingOrder = 8;
            transform.localScale = new Vector3(0.85f, 0.65f, 1f);
        }

        private void SetupCollider()
        {
            Collider2D chestCollider = GetComponent<Collider2D>();
            if (chestCollider == null)
            {
                chestCollider = gameObject.AddComponent<BoxCollider2D>();
            }

            chestCollider.isTrigger = true;
        }

        private void SetupPrompt()
        {
            Transform existingPrompt = transform.Find("Prompt");
            GameObject promptObject = existingPrompt != null ? existingPrompt.gameObject : new GameObject("Prompt");
            promptObject.transform.SetParent(transform, false);
            promptObject.transform.localPosition = new Vector3(0f, 0.95f, 0f);

            promptText = promptObject.GetComponent<TextMesh>();
            if (promptText == null)
            {
                promptText = promptObject.AddComponent<TextMesh>();
            }

            promptText.text = "Press F to open chest";
            promptText.fontSize = 24;
            promptText.characterSize = 0.08f;
            promptText.anchor = TextAnchor.MiddleCenter;
            promptText.alignment = TextAlignment.Center;
            promptText.color = Color.white;

            MeshRenderer meshRenderer = promptObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.sortingOrder = 30;
            }

            SetPromptVisible(false);
        }

        private void SetPromptVisible(bool visible)
        {
            if (promptText != null)
            {
                promptText.gameObject.SetActive(visible);
            }
        }

        private static Sprite CreateChestSprite()
        {
            const int width = 16;
            const int height = 12;
            Texture2D texture = new Texture2D(width, height)
            {
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color dark = new Color(0.28f, 0.13f, 0.04f, 1f);
            Color wood = new Color(0.58f, 0.30f, 0.08f, 1f);
            Color gold = new Color(1f, 0.74f, 0.12f, 1f);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    bool border = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                    bool band = y == 5 || x == width / 2;
                    texture.SetPixel(x, y, border ? dark : band ? gold : wood);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), width);
        }
    }
}
