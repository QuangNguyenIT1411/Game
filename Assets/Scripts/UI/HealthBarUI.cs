using DungeonCrawler.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonCrawler.UI
{
    /// <summary>
    /// Simple screen-space health bar bound to a Health component.
    /// </summary>
    public class HealthBarUI : MonoBehaviour
    {
        [SerializeField] private Health targetHealth;
        [SerializeField] private Image fillImage;
        [SerializeField] private Text hpText;

        private float lastLoggedFillWidth = -1f;
        private bool loggedBind;

        private void Awake()
        {
            if (targetHealth == null)
            {
                GameObject player = GameObject.Find("Player");
                if (player != null)
                {
                    targetHealth = player.GetComponent<Health>();
                }
            }

            if (fillImage == null)
            {
                Transform fillTransform = transform.Find("PlayerHealthBar_Fill") ?? transform.Find("Fill");
                if (fillTransform != null)
                {
                    fillImage = fillTransform.GetComponent<Image>();
                }
            }

            if (hpText == null)
            {
                Transform textTransform = transform.Find("HPText");
                if (textTransform != null)
                {
                    hpText = textTransform.GetComponent<Text>();
                }
            }
        }

        private void OnEnable()
        {
            Subscribe();
            LogBindOnce();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (targetHealth == null)
            {
                GameObject player = GameObject.Find("Player");
                if (player != null)
                {
                    Bind(player.GetComponent<Health>(), fillImage);
                }
            }

            Refresh();
        }

        public void Bind(Health health, Image fill)
        {
            Unsubscribe();
            targetHealth = health;
            fillImage = fill;
            Transform textTransform = transform.Find("HPText");
            if (textTransform != null)
            {
                hpText = textTransform.GetComponent<Text>();
            }

            LogBindOnce();

            Subscribe();
            Refresh();
        }

        private void LogBindOnce()
        {
            if (loggedBind || targetHealth == null)
            {
                return;
            }

            loggedBind = true;
        }

        private void Subscribe()
        {
            if (targetHealth != null)
            {
                targetHealth.Changed += OnHealthChanged;
            }
        }

        private void Unsubscribe()
        {
            if (targetHealth != null)
            {
                targetHealth.Changed -= OnHealthChanged;
            }
        }

        private void OnHealthChanged(Health health)
        {
            Refresh();
        }

        public void Refresh()
        {
            if (fillImage == null || targetHealth == null)
            {
                return;
            }

            float healthPercent = targetHealth.Normalized;
            fillImage.fillAmount = healthPercent;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            UpdateFillWidth(healthPercent);

            if (hpText != null)
            {
                hpText.text = $"HP: {targetHealth.CurrentHealth} / {targetHealth.MaxHealth}";
            }

        }

        private void UpdateFillWidth(float healthPercent)
        {
            RectTransform fillRect = fillImage != null ? fillImage.rectTransform : null;
            RectTransform backgroundRect = transform as RectTransform;
            if (fillRect == null || backgroundRect == null)
            {
                return;
            }

            // Keep vertical offsets consistent with generator (2px padding)
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(fillRect.offsetMax.x, -2f);

            float maxWidth = backgroundRect.rect.width - 4f; // 2px left + 2px right
            float fillWidth = Mathf.Max(0f, maxWidth * Mathf.Clamp01(healthPercent));
            fillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fillWidth);

            if (!Mathf.Approximately(lastLoggedFillWidth, fillWidth))
            {
                lastLoggedFillWidth = fillWidth;
            }
        }
    }
}
