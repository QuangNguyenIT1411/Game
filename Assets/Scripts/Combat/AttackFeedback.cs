using System.Collections;
using UnityEngine;

namespace DungeonCrawler.Combat
{
    /// <summary>
    /// Runtime-only visual helpers for simple combat feedback.
    /// </summary>
    public static class AttackFeedback
    {
        public static void SpawnSlash(Vector3 origin, Vector2 direction, float duration = 0.12f)
        {
            Vector2 safeDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.down;
            GameObject slash = new GameObject("AttackSlashFeedback");
            slash.transform.position = origin + (Vector3)(safeDirection * 0.75f);
            slash.transform.localScale = new Vector3(0.9f, 0.45f, 1f);

            float angle = Mathf.Atan2(safeDirection.y, safeDirection.x) * Mathf.Rad2Deg;
            slash.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            SpriteRenderer renderer = slash.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateSquareSprite(new Color(1f, 0.95f, 0.45f, 0.85f));
            renderer.sortingOrder = 20;

            Object.Destroy(slash, duration);
        }

        public static void FlashHit(MonoBehaviour owner, SpriteRenderer renderer)
        {
            if (owner == null || renderer == null)
            {
                return;
            }

            owner.StartCoroutine(FlashRoutine(renderer));
        }

        private static IEnumerator FlashRoutine(SpriteRenderer renderer)
        {
            Color originalColor = renderer.color;
            renderer.color = Color.white;
            yield return new WaitForSeconds(0.06f);
            if (renderer != null)
            {
                renderer.color = new Color(1f, 0.2f, 0.15f);
            }

            yield return new WaitForSeconds(0.06f);
            if (renderer != null)
            {
                renderer.color = originalColor;
            }
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
