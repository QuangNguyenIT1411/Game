using DungeonCrawler.Enemy;
using DungeonCrawler.Player;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DungeonCrawler.Dungeon
{
    public class RuntimeVisibilityFixer : MonoBehaviour
    {
        private const int DefaultLayer = 0;

        private void Start()
        {
            ApplyFixes();
            LogVisibilityState();
        }

        private void LateUpdate()
        {
            ApplyFixes();
        }

        private void ApplyFixes()
        {
            EnemyVisualController.CleanOrphanSlimeVisuals();

            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.orthographic = true;
                camera.orthographicSize = 8f;
                camera.cullingMask |= 1 << DefaultLayer;
                camera.transform.rotation = Quaternion.identity;

                Vector3 cameraPosition = camera.transform.position;
                camera.transform.position = new Vector3(cameraPosition.x, cameraPosition.y, -10f);
            }

            ConfigureTilemap(FindTilemap("FloorTilemap"), 0);
            ConfigureTilemap(FindTilemap("WallTilemap"), 5);

            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                player.layer = DefaultLayer;
                ConfigurePlayerRenderers(player);
            }

            foreach (EnemyController enemy in FindObjectsByType<EnemyController>(FindObjectsInactive.Include))
            {
                if (enemy != null)
                {
                    enemy.gameObject.layer = DefaultLayer;
                    ConfigureEnemyRenderers(enemy.gameObject);
                }
            }
        }

        private static void ConfigureTilemap(Tilemap tilemap, int sortingOrder)
        {
            if (tilemap == null)
            {
                return;
            }

            tilemap.gameObject.layer = DefaultLayer;
            tilemap.color = Color.white;

            TilemapRenderer renderer = tilemap.GetComponent<TilemapRenderer>();
            if (renderer != null)
            {
                renderer.enabled = true;
                renderer.sortingOrder = sortingOrder;
            }
        }

        private static void ConfigureSpriteRenderers(GameObject root, int sortingOrder)
        {
            foreach (SpriteRenderer renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderer.enabled = true;
                renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, sortingOrder);
                renderer.color = new Color(renderer.color.r, renderer.color.g, renderer.color.b, 1f);
            }
        }

        private static void ConfigurePlayerRenderers(GameObject player)
        {
            Transform visual = player.transform.Find("PlayerVisual");
            PlayerVisualController visualController = visual != null ? visual.GetComponent<PlayerVisualController>() : null;
            bool hasSpriteSheet = visualController != null && visualController.HasSpriteSheet;

            SpriteRenderer rootRenderer = player.GetComponent<SpriteRenderer>();
            if (rootRenderer != null)
            {
                rootRenderer.enabled = !hasSpriteSheet;
                rootRenderer.sortingOrder = 20;
            }

            if (visual != null)
            {
                SpriteRenderer visualRenderer = visual.GetComponent<SpriteRenderer>();
                if (visualRenderer != null)
                {
                    visualRenderer.enabled = true;
                    visualRenderer.sortingOrder = 20;
                    visualRenderer.color = Color.white;
                }
            }

            if (!hasSpriteSheet)
            {
                ConfigureSpriteRenderers(player, 20);
            }
        }

        private static void ConfigureEnemyRenderers(GameObject enemy)
        {
            EnemyVisualController visualController = enemy.GetComponent<EnemyVisualController>();
            Transform visual = enemy.transform.Find("EnemyVisual");
            bool hasVisualChild = (visualController != null && visualController.HasLoadedVisual)
                || (visual != null && visual.GetComponentsInChildren<SpriteRenderer>(true).Length > 0);

            SpriteRenderer rootRenderer = enemy.GetComponent<SpriteRenderer>();
            if (rootRenderer != null)
            {
                rootRenderer.enabled = !hasVisualChild;
                if (hasVisualChild)
                {
                    rootRenderer.sprite = null;
                    rootRenderer.color = new Color(rootRenderer.color.r, rootRenderer.color.g, rootRenderer.color.b, 0f);
                    rootRenderer.sortingOrder = 0;
                }
                else
                {
                    rootRenderer.sortingOrder = Mathf.Max(rootRenderer.sortingOrder, 21);
                }
            }

            if (visual == null)
            {
                if (rootRenderer != null)
                {
                    rootRenderer.enabled = true;
                }

                return;
            }

            foreach (SpriteRenderer renderer in visual.GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderer.enabled = true;
                renderer.sortingOrder = 30;
                renderer.color = new Color(renderer.color.r, renderer.color.g, renderer.color.b, renderer.color.a);
            }
        }

        private static Tilemap FindTilemap(string objectName)
        {
            GameObject tilemapObject = GameObject.Find(objectName);
            return tilemapObject != null ? tilemapObject.GetComponent<Tilemap>() : null;
        }

        private static void LogVisibilityState()
        {
            Camera camera = Camera.main;
            if (camera != null)
            {
                Debug.Log($"[RuntimeVisibilityFixer] Camera position={camera.transform.position} orthographicSize={camera.orthographicSize} cullingMask={camera.cullingMask}", camera);
            }
            else
            {
                Debug.LogWarning("[RuntimeVisibilityFixer] Main Camera missing.");
            }

            LogTilemapState("FloorTilemap");
            LogTilemapState("WallTilemap");
        }

        private static void LogTilemapState(string objectName)
        {
            Tilemap tilemap = FindTilemap(objectName);
            if (tilemap == null)
            {
                Debug.LogWarning($"[RuntimeVisibilityFixer] {objectName} missing.");
                return;
            }

            TilemapRenderer renderer = tilemap.GetComponent<TilemapRenderer>();
            bool rendererEnabled = renderer != null && renderer.enabled;
            int sortingOrder = renderer != null ? renderer.sortingOrder : int.MinValue;
            Debug.Log($"[RuntimeVisibilityFixer] {objectName} active={tilemap.gameObject.activeInHierarchy} rendererEnabled={rendererEnabled} sortingOrder={sortingOrder} tileCount={tilemap.GetUsedTilesCount()}", tilemap);
        }
    }
}
