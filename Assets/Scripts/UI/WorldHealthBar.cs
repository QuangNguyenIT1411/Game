using DungeonCrawler.Combat;
using UnityEngine;

namespace DungeonCrawler.UI
{
    /// <summary>
    /// Small world-space health bar for enemies and other actors.
    /// </summary>
    public class WorldHealthBar : MonoBehaviour
    {
        [SerializeField] private Health targetHealth;
        [SerializeField] private Vector3 localOffset = new Vector3(0f, 0.85f, 0f);

        private Transform barRoot;
        private Transform fillTransform;
        private const float FillWidth = 0.9f;

        private void Awake()
        {
            if (targetHealth == null)
            {
                targetHealth = GetComponent<Health>();
            }

            SetupBar();
        }

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Bind(Health health)
        {
            Unsubscribe();
            targetHealth = health;
            SetupBar();
            Subscribe();
            Refresh();
        }

        public void SetLocalOffset(Vector3 offset)
        {
            localOffset = offset;
            if (barRoot != null)
            {
                barRoot.localPosition = localOffset;
            }
        }

        private void LateUpdate()
        {
            if (barRoot != null)
            {
                barRoot.localPosition = localOffset;
            }
        }

        private void Subscribe()
        {
            if (targetHealth == null)
            {
                return;
            }

            targetHealth.OnHealthChanged += OnHealthChanged;
            targetHealth.OnDied += OnDied;
        }

        private void Unsubscribe()
        {
            if (targetHealth == null)
            {
                return;
            }

            targetHealth.OnHealthChanged -= OnHealthChanged;
            targetHealth.OnDied -= OnDied;
        }

        private void OnHealthChanged(Health health)
        {
            Refresh();
        }

        private void OnDied(Health health)
        {
            if (barRoot != null)
            {
                barRoot.gameObject.SetActive(false);
            }
        }

        private void SetupBar()
        {
            if (barRoot == null)
            {
                Transform existingRoot = transform.Find("EnemyWorldHealthBar");
                barRoot = existingRoot != null ? existingRoot : new GameObject("EnemyWorldHealthBar").transform;
                barRoot.SetParent(transform);
            }

            barRoot.localPosition = localOffset;
            barRoot.localRotation = Quaternion.identity;
            barRoot.localScale = Vector3.one;

            CreateBarPart("Background", new Color(0.05f, 0.02f, 0.02f), FillWidth, 0.12f, 13);
            fillTransform = CreateBarPart("Fill", new Color(0.1f, 1f, 0.25f), FillWidth, 0.08f, 14);
        }

        private Transform CreateBarPart(string partName, Color color, float width, float height, int sortingOrder)
        {
            Transform part = barRoot.Find(partName);
            if (part == null)
            {
                part = new GameObject(partName).transform;
                part.SetParent(barRoot);
            }

            part.localPosition = Vector3.zero;
            part.localRotation = Quaternion.identity;
            part.localScale = new Vector3(width, height, 1f);

            SpriteRenderer renderer = part.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = part.gameObject.AddComponent<SpriteRenderer>();
            }

            renderer.sprite = CreateSquareSprite(color);
            renderer.sortingOrder = sortingOrder;
            return part;
        }

        private void Refresh()
        {
            if (targetHealth == null || fillTransform == null)
            {
                return;
            }

            float healthPercent = Mathf.Clamp01(targetHealth.Normalized);
            fillTransform.localScale = new Vector3(FillWidth * healthPercent, 0.08f, 1f);
            fillTransform.localPosition = new Vector3((FillWidth * (healthPercent - 1f)) * 0.5f, 0f, 0f);
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
